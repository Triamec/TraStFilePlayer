// Copyright © 2026 Triamec Motion AG

namespace TraStFilePlayer.Models {
	/// <summary>
	/// Represents the current playback state of the Trast File Player.
	/// </summary>
	internal enum PlaybackState {
		/// <summary>Motion system is currently connecting (startup or reconnect).</summary>
		MotionSystemConnecting,

		/// <summary>Motion system connection is not available.</summary>
		NoMotionSystem,

		/// <summary>No trajectory file is selected.</summary>
		NoFile,

		/// <summary>Selected file is invalid or incompatible with available axes.</summary>
		InvalidFile,

		/// <summary>At least one involved axis has an error.</summary>
		AxesError,

		/// <summary>Axes are valid but not enabled.</summary>
		AxesNotEnabled,

		/// <summary>Axes are enabled but not at initial position.</summary>
		AxesNotAtInitialPosition,

		/// <summary>Axes are enabled, homed and ready for streaming.</summary>
		AxesReadyForStreaming,

		/// <summary>Axes are currently moving while not streaming.</summary>
		NonStreamingMove,

		/// <summary>Trajectory streaming is currently paused or pausing.</summary>
		StreamingPaused,

		/// <summary>Trajectory streaming is active.</summary>
		Streaming
	}
}
