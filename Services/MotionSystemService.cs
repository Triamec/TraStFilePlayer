// Copyright © 2026 Triamec Motion AG

using System.Diagnostics;
using TraStFilePlayer.CsvParser;
using Triamec.Tam.Motion;

namespace TraStFilePlayer.Services {
	/// <summary>
	/// Manages the connection to the Triamec motion system, axis-group lifecycle,
	/// and pre-streaming axis preparation (enable / home / init-move / reset-error).
	/// </summary>
	internal sealed class MotionSystemService : IAsyncDisposable, IDisposable {
		readonly object _stateLock = new();
		bool _isDisposed;
		IMotionSystem? _system;
		IAxisGroup<IAxisWithRegisterLayoutId>? _axisGroup;

		// ── Connection ──────────────────────────────────────────────────────────

		public bool IsConnected => _system != null;

		public bool IsConnecting { get; private set; } = true;

		public IAxisGroup<IAxisWithRegisterLayoutId>? AxisGroup => _axisGroup;

		/// <summary>
		/// Connects to all reachable drives via Ethernet and takes control.
		/// </summary>
		public async Task ConnectAsync() {
			lock (_stateLock) {
				if (_system != null || _isDisposed) return;
			}
			IsConnecting = true;
			try {
				IMotionSystem system = await MotionSystem.Connect(control: true).ConfigureAwait(false);
				bool disposed;
				lock (_stateLock) {
					disposed = _isDisposed;
					if (!disposed) {
						_system = system;
					}
				}
				if (disposed) {
					await system.DisposeAsync().ConfigureAwait(false);
				}
			} finally {
				IsConnecting = false;
			}
		}

		/// <summary>Disposes the axis group and the motion system.</summary>
		public async ValueTask DisconnectAsync() {
			lock (_stateLock) {
				_isDisposed = true;
			}
			await DisposeGroupAsync().ConfigureAwait(false);
			await DisposeSystemAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Restarts the application to reset the full Trast File Player lifecycle.
		/// </summary>
		public async Task RestartApplicationAsync() {
			string? executablePath = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(executablePath)) {
				throw new InvalidOperationException("Could not determine application executable path for restart.");
			}
			await DisconnectAsync().ConfigureAwait(false);
			Process.Start(new ProcessStartInfo {
				FileName = executablePath,
				UseShellExecute = true
			});

			await MainThread.InvokeOnMainThreadAsync(() => {
				Application.Current?.Quit();
			}).ConfigureAwait(false);

			Environment.Exit(0);
		}

		// ── Axis resolution and group creation ──────────────────────────────────

		/// <summary>
		/// Resolves the axes listed in <paramref name="header"/>, validates uniqueness, and
		/// creates the axis group. Throws <see cref="InvalidOperationException"/> on any check failure.
		/// </summary>
		public async Task<string[]> PrepareAxisGroupAsync(TraStHeader header) {
			EnsureConnected();
			await DisposeGroupAsync().ConfigureAwait(false);
			var resolvedNames = new List<string>(header.NumberOfInvolvedAxes);
			var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var axisDef in header.Axes) {
				IAxis axis = ResolveAxis(axisDef.AxisIdentifier);

				if (!uniqueNames.Add(axis.Name)) {
					throw new InvalidOperationException(
						$"AxisIdentifier '{axisDef.AxisIdentifier}' resolves to '{axis.Name}' which is already used by another axis definition.");
				}

				resolvedNames.Add(axis.Name);
			}

			_axisGroup = await _system!.Axes.CreateGroup(resolvedNames.ToArray()).ToTask().ConfigureAwait(false);
			return [.. resolvedNames];
		}

		// ── Axis operations ─────────────────────────────────────────────────────

		/// <summary>Enables all axes in the group simultaneously.</summary>
		public async Task EnableAsync() {
			EnsureGroup();
			await _axisGroup!.Enable().ConfigureAwait(false);
		}

		/// <summary>Disables all axes in the group simultaneously.</summary>
		public async Task DisableAsync() {
			EnsureGroup();
			await _axisGroup!.Disable().ConfigureAwait(false);
		}

		/// <summary>Homes all axes in the group simultaneously.</summary>
		public async Task HomeAsync() {
			EnsureGroup();
			await _axisGroup!.Home().ConfigureAwait(false);
		}

		/// <summary>
		/// Moves each axis to its corresponding <paramref name="initialPositions"/> entry simultaneously.
		/// </summary>
		public async Task InitMoveAsync(double[] initialPositions) {
			EnsureGroup();
			if (initialPositions.Length != _axisGroup!.Count) {
				throw new ArgumentException(
					$"Expected {_axisGroup.Count} positions but got {initialPositions.Length}.", nameof(initialPositions));
			}
			//// HOTFIX. This was added 11.09.2026 and left here for reference. Can be removed once Modulo issue is fixed.
			//foreach (IAxis axis in _axisGroup) {
			//	if (axis.Name == "C") {
			//		await axis.Home().ConfigureAwait(false);
			//	}
			//}
			//// HOTFIX
			var moveTasks = new Task[_axisGroup.Count];
			for (int i = 0; i < _axisGroup.Count; i++) {
				double target = initialPositions[i];
				moveTasks[i] = _axisGroup[i].MoveAbsolute(target);
			}
			await Task.WhenAll(moveTasks).ConfigureAwait(false);
		}

		/// <summary>
		/// Checks whether the position of all axes matches <paramref name="initialPositions"/>
		/// within <paramref name="tolerance"/>.
		/// </summary>
		public bool AreAxesAtInitPosition(double[] initialPositions, double tolerance = 1e-3) {
			if (_axisGroup == null) return false;
			for (int i = 0; i < _axisGroup.Count; i++) {
				double pos = TryGetReferencePosition(_axisGroup[i]);
				if (Math.Abs(pos - initialPositions[i]) > tolerance) return false;
			}
			return true;
		}

		static double TryGetReferencePosition(IAxisWithRegisterLayoutId axis) {
			if (axis == null) throw new ArgumentNullException(nameof(axis));
			if (axis.RegisterLayoutId == 19) {
				return axis.AsAxis19().Signals.PathPlanner.Position.Read();
			} else if (axis.RegisterLayoutId == 20) {
				return axis.AsAxis20().Signals.PathPlanner.Position.Read();
			} else {
				throw new InvalidOperationException(
					$"Axis '{axis.Name}' has unsupported RegisterLayoutId {axis.RegisterLayoutId}. Expected 19 or 20.");
			}
		}

		/// <summary>Returns whether all axes are homed.</summary>
		public bool AreAxesHomed() =>
			_axisGroup != null && _axisGroup.Status.All.Homed;

		/// <summary>Returns whether all axes are enabled.</summary>
		public bool AreAxesEnabled() =>
			_axisGroup != null && _axisGroup.Status.All.Enabled;

		/// <summary>Returns whether all axes are in standstill.</summary>
		public bool AreAxesInStandstill() =>
			_axisGroup != null && _axisGroup.Status.All.Standstill;

		/// <summary>Returns one message per axis in the group that currently reports an error.</summary>
		public IReadOnlyList<string> GetAxisErrors() {
			if (_axisGroup == null) return [];
			return [.. _axisGroup
				.Where(axis => axis.Status.Alert.IsError)
				.Select(axis => $"- Error on Axis {axis.Name}: {axis.Status.Alert.Text}")];
		}

		/// <summary>Acknowledges all alerts on all axes.</summary>
		public async Task ResetErrorAsync() {
			EnsureGroup();
			await _axisGroup!.Status.All.Alert.Acknowledge().ConfigureAwait(false);
		}

		/// <summary>Acknowledges all alerts when no group is set (individual axes).</summary>
		public async Task ResetErrorOnSystemAsync() {
			if (_system == null) return;
			var tasks = _system.Axes.Select(a => a.Status.Alert.Acknowledge());
			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		// ── Group access (used by PlaybackService) ──────────────────────────────

		/// <summary>Returns the active axis group or throws if not ready.</summary>
		public IAxisGroup<IAxis> GetAxisGroup() {
			EnsureGroup();
			return _axisGroup!;
		}

		// ── IAsyncDisposable & IDisposable ──────────────────────────────────────

		/// <summary>Disposes the axis group and the motion system asynchronously.</summary>
		public async ValueTask DisposeAsync() {
			await DisconnectAsync().ConfigureAwait(false);
			GC.SuppressFinalize(this);
		}

		/// <summary>Disposes the axis group and the motion system synchronously.</summary>
		public void Dispose() {
			lock (_stateLock) {
				_isDisposed = true;
			}
			DisposeGroup();
			DisposeSystem();
			GC.SuppressFinalize(this);
		}

		// ── Private helpers ─────────────────────────────────────────────────────

		IAxis ResolveAxis(string identifier) {
			string trimmed = identifier.Trim();
			try {
				return _system!.Axes[trimmed];
			} catch (KeyNotFoundException) { }

			// Try "DeviceName.AxisName" → fall back to just the axis name part
			int dot = trimmed.LastIndexOf('.');
			if (dot > 0 && dot + 1 < trimmed.Length) {
				string axisNameOnly = trimmed[(dot + 1)..];
				try {
					return _system!.Axes[axisNameOnly];
				} catch (KeyNotFoundException) { }
			}

			throw new InvalidOperationException(
				$"AxisIdentifier '{identifier}' could not be resolved to an axis in the connected motion system.");
		}

		void EnsureConnected() {
			if (_system == null) {
				throw new InvalidOperationException("Not connected to the motion system.");
			}
		}

		void EnsureGroup() {
			if (_axisGroup == null) {
				throw new InvalidOperationException("Axis group is not prepared.");
			}
		}

		async Task DisposeGroupAsync() {
			IAxisGroup<IAxisWithRegisterLayoutId>? axisGroup;
			lock (_stateLock) {
				axisGroup = _axisGroup;
				_axisGroup = null;
			}
			if (axisGroup != null) {
				await axisGroup.DisposeAsync().ConfigureAwait(false);
			}
		}

		async Task DisposeSystemAsync() {
			IMotionSystem? system;
			lock (_stateLock) {
				system = _system;
				_system = null;
			}
			if (system != null) {
				await system.DisposeAsync().ConfigureAwait(false);
			}
		}

		void DisposeGroup() {
			IAxisGroup<IAxisWithRegisterLayoutId>? axisGroup;
			lock (_stateLock) {
				axisGroup = _axisGroup;
				_axisGroup = null;
			}
			axisGroup?.Dispose();
		}

		void DisposeSystem() {
			IMotionSystem? system;
			lock (_stateLock) {
				system = _system;
				_system = null;
			}
			system?.Dispose();
		}

	}
}
