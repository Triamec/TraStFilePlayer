// Copyright © 2026 Triamec Motion AG

namespace TraStFilePlayer.Models {
	/// <summary>
	/// Snapshot of which UI controls should be active for a given <see cref="PlaybackState"/>.
	/// Used by the Blazor page to enable/disable buttons declaratively.
	/// </summary>
	internal sealed class UiCommandState {
		public bool CanRestart { get; init; }
		public bool CanOpen { get; init; }
		public bool CanEnable { get; init; }
		public bool CanDisable { get; init; }
		public bool CanHome { get; init; }
		public bool CanInitMove { get; init; }
		public bool CanPlay { get; init; }
		public bool CanPause { get; init; }
		public bool CanStop { get; init; }
		public bool CanResetError { get; init; }

		/// <summary>Returns the <see cref="UiCommandState"/> matching the given <paramref name="state"/>.</summary>
		public static UiCommandState From(PlaybackState state) => state switch {
			PlaybackState.MotionSystemConnecting => new UiCommandState {
			},
			PlaybackState.NoMotionSystem => new UiCommandState {
				CanRestart = true
			},
			PlaybackState.NoFile => new UiCommandState {
				CanOpen = true,
				CanRestart = true
			},
			PlaybackState.InvalidFile => new UiCommandState {
				CanOpen = true,
				CanRestart = true
			},
			PlaybackState.AxesError => new UiCommandState {
				CanOpen = true,
				CanResetError = true,
				CanRestart = true
			},
			PlaybackState.AxesNotEnabled => new UiCommandState {
				CanOpen = true,
				CanEnable = true,
				CanRestart = true
			},
			PlaybackState.AxesNotAtInitialPosition => new UiCommandState {
				CanOpen = true,
				CanDisable = true,
				CanHome = true,
				CanInitMove = true,
				CanRestart = true
			},
			PlaybackState.AxesReadyForStreaming => new UiCommandState {
				CanOpen = true,
				CanDisable = true,
				CanHome = true,
				CanInitMove = true,
				CanPlay = true,
				CanRestart = true
			},
			PlaybackState.NonStreamingMove => new UiCommandState {
				CanRestart = true
			},
			PlaybackState.StreamingPaused => new UiCommandState {
				CanPlay = true,
				CanStop = true
			},
			PlaybackState.Streaming => new UiCommandState {
				CanPause = true,
				CanStop = true
			},
			_ => new UiCommandState { CanOpen = true }
		};
	}
}
