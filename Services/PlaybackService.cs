// Copyright © 2026 Triamec Motion AG

using Triamec.Tam.Motion;
using Triamec.Tam.Motion.TrajectoryStreaming;
using SharedTraStHeader = Triamec.Tam.Motion.TrajectoryStreaming.Parser.TraStHeader;

namespace TraStFilePlayer.Services {
	/// <summary>
	/// Manages trajectory playback: maps TraSt CSV columns to streamer buffers, drives the
	/// streaming loop, applies feed override, handles pause (override = 0), stop, loop and error.
	/// </summary>
	internal sealed class PlaybackService : IAsyncDisposable {
		const int MaxPointsPerSegment = 500_000;

		// Injected
		readonly MotionSystemService _motionSystem;

		// Active session state
		ITrajectoryStreamer? _streamer;
		CancellationTokenSource? _cts;
		Task? _streamingTask;

		/// <summary>Raised on the thread pool whenever state changes (progress, state, message).</summary>
		public event EventHandler? StateChanged;

		// ── Observable properties ───────────────────────────────────────────────

		public bool IsStreaming => _streamer?.IsStreaming ?? false;
		public bool IsPlaybackSessionActive { get; private set; }
		public bool IsPaused { get; private set; }
		public long StreamedIndex => _streamer?.StreamedIndex ?? 0;
		public long ActualIndex => _streamer?.ActualIndex ?? 0;
		public long TotalPoints { get; private set; }
		public string? CurrentFilePath { get; private set; }
		public string? NotificationMessage { get; private set; }
		public bool HasError { get; private set; }

		public string? ConsumeNotificationMessage() {
			var message = NotificationMessage;
			NotificationMessage = null;
			return message;
		}

		public double FeedOverride {
			get => _feedOverride;
			set {
				_feedOverride = Math.Clamp(value, 0.0, 100.0);
				// Apply immediately if streaming and not paused
				if (_streamer != null && !IsPaused) {
					_streamer.Override = _feedOverride;
				}
			}
		}
		double _feedOverride = 100.0;

		public bool LoopEnabled { get; set; }

		public PlaybackService(MotionSystemService motionSystem) {
			_motionSystem = motionSystem ?? throw new ArgumentNullException(nameof(motionSystem));
		}

		// ── Play ────────────────────────────────────────────────────────────────

		/// <summary>
		/// Starts or resumes playback of <paramref name="filePath"/> using metadata from <paramref name="header"/>.
		/// If paused, unpauses by restoring the current feed override.
		/// </summary>
		public async Task PlayAsync(string filePath, SharedTraStHeader header) {
			// Resume from pause
			if (IsPaused && _streamer != null) {
				IsPaused = false;
				_streamer.Override = _feedOverride;
				Notify("- Trajectory stream is active ...");
				return;
			}

			// Fresh start
			await StopInternalAsync().ConfigureAwait(false);

			CurrentFilePath = filePath;
			TotalPoints = header.NumberOfDataRows;
			HasError = false;
			IsPaused = false;
			IsPlaybackSessionActive = true;

			// Build extension column index lists across all axes
			var positionColumns = header.Axes.Select(a => a.PositionColumnIndex).ToList();
			var extensionColumns = BuildExtensionColumns(header);

			_streamer = await CreateStreamerAsync(header).ConfigureAwait(false);
			_streamer.Override = _feedOverride;

			_cts = new CancellationTokenSource();
			_streamingTask = Task.Run(() => RunStreamingLoopAsync(
				filePath, header, positionColumns, extensionColumns, _cts.Token));

			Notify("- Trajectory stream is active ...");
		}

		// ── Pause ───────────────────────────────────────────────────────────────

		/// <summary>Pauses playback by setting override to 0. The stream remains active.</summary>
		public void Pause() {
			if (_streamer == null || !IsStreaming || IsPaused) return;
			IsPaused = true;
			_streamer.Override = 0.0;
			Notify("- Trajectory stream paused");
		}

		// ── Stop ────────────────────────────────────────────────────────────────

		/// <summary>Stops streaming and disposes the streamer.</summary>
		public async Task StopAsync() {
			await StopInternalAsync().ConfigureAwait(false);
			IsPaused = false;
			IsPlaybackSessionActive = false;
			Notify("- Trajectory stream stopped");
			NotifyStateChanged();
		}

		// ── Internals ───────────────────────────────────────────────────────────

		async Task RunStreamingLoopAsync(
			string filePath,
			SharedTraStHeader header,
			List<int> positionColumns,
			List<int> extensionColumns,
			CancellationToken ct) {
#pragma warning disable CA1031
			try {
				do {
					using var parser = new Triamec.Tam.Motion.TrajectoryStreaming.Parser.TraStCsvParser(filePath);

					while (parser.HasMoreData() && !ct.IsCancellationRequested) {
						var (positions, extensions) = parser.ReadSegment(
							MaxPointsPerSegment, positionColumns, extensionColumns);

						if (positions.GetLength(0) == 0) break;

						bool isLast = !parser.HasMoreData();
						// Retry until buffer accepts the segment
						while (!ct.IsCancellationRequested) {
							if (_streamer!.Move(positions, extensions, isLast)) break;
							await Task.Delay(10, ct).ConfigureAwait(false);
						}

						NotifyStateChanged();
					}

					// Wait for drives to finish the last buffered samples
					while (!ct.IsCancellationRequested && _streamer!.IsStreaming) {
						await Task.Delay(100, ct).ConfigureAwait(false);
						NotifyStateChanged();
					}

					if (ct.IsCancellationRequested) break;

					ThrowIfAxisError();

					// Check loop condition
					if (LoopEnabled && !IsPaused) {
						// If end differs from start: move back to init position then restart
						var initPositions = parser.PeekFirstDataRow(positionColumns);
						if (!_motionSystem.AreAxesAtInitPosition(initPositions)) {
							await _motionSystem.InitMoveAsync(initPositions).ConfigureAwait(false);
						}

						await DisposeStreamerAsync().ConfigureAwait(false);
						_streamer = await CreateStreamerAsync(header).ConfigureAwait(false);
						_streamer.Override = _feedOverride;
					} else {
						break;
					}
				} while (LoopEnabled && !ct.IsCancellationRequested);

				if (!ct.IsCancellationRequested) {
					Notify("- ... trajectory stream done!");
				}
			} catch (OperationCanceledException) {
				// Expected on stop
			} catch (Exception ex) {
				HasError = true;
				Notify($"Error during streaming: {ex.Message}");
				_streamer?.StopStreaming();
			} finally {
				IsPaused = false;
				if (_cts == null || _cts.Token == ct) {
					IsPlaybackSessionActive = false;
				}
				NotifyStateChanged();
			}
#pragma warning restore CA1031
		}

		void ThrowIfAxisError() {
			var status = _motionSystem.GetAxisGroup().Status.Any;
			if (status.ErrorStop || status.Alert.IsError) {
				throw new InvalidOperationException($"Axis error: {status.Alert.Text}");
			}
		}

		async Task<ITrajectoryStreamer> CreateStreamerAsync(SharedTraStHeader header) {
			IAxisGroup<IAxis> group = _motionSystem.GetAxisGroup();
			var builder = group
				.PrepareTrajectoryStreaming()
				.SetStreamRate(header.SamplingFrequencyHz);

			for (int i = 0; i < header.Axes.Count; i++) {
				var axisDef = header.Axes[i];
				IAxis axis = group[i];

				if (axisDef.CurrentColumnIndex.HasValue) {
					builder = builder.UseCurrentExtension(axis);
				}
				if (axisDef.VelocityColumnIndex.HasValue) {
					builder = builder.UseVelocityExtension(axis);
				}
				if (axisDef.CustomExtensionColumnIndices.Count > 0) {
					builder = builder.SetAdditionalExtension((uint)axisDef.CustomExtensionColumnIndices.Count, axis);
				}
			}

			return await builder.CreateStreamer().ConfigureAwait(false);
		}

		static List<int> BuildExtensionColumns(SharedTraStHeader header) {
			var list = new List<int>();
			foreach (var axisDef in header.Axes) {
				if (axisDef.CurrentColumnIndex.HasValue) list.Add(axisDef.CurrentColumnIndex.Value);
				if (axisDef.VelocityColumnIndex.HasValue) list.Add(axisDef.VelocityColumnIndex.Value);
				list.AddRange(axisDef.CustomExtensionColumnIndices);
			}
			return list;
		}

		async Task StopInternalAsync() {
			if (_cts != null && !_cts.IsCancellationRequested) {
				_streamer?.StopStreaming();
				await _cts.CancelAsync().ConfigureAwait(false);
			}

			if (_streamingTask != null) {
				try {
					await _streamingTask.ConfigureAwait(false);
				} catch (OperationCanceledException) {
				} catch (InvalidOperationException) {
					// Swallow on stop
				}
			}

			await DisposeStreamerAsync().ConfigureAwait(false);
			_cts?.Dispose();
			_cts = null;
			_streamingTask = null;
		}

		async Task DisposeStreamerAsync() {
			if (_streamer != null) {
				await _streamer.DisposeAsync().ConfigureAwait(false);
				_streamer = null;
			}
		}

		void Notify(string message) {
			NotificationMessage = message;
			NotifyStateChanged();
		}

		void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

		public async ValueTask DisposeAsync() {
			await StopInternalAsync().ConfigureAwait(false);
			GC.SuppressFinalize(this);
		}
	}
}
