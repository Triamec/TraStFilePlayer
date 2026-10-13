// Copyright © 2026 Triamec Motion AG

namespace TraStFilePlayer.Services {
	/// <summary>
	/// Thin singleton wrapper around <see cref="TraStFileService"/> that keeps the
	/// in-memory recent-files list in sync and is injectable via DI.
	/// </summary>
	internal static class RecentFilesService {

		/// <summary>Gets a live snapshot of recent files, most-recent first.</summary>
		public static IReadOnlyList<string> RecentFiles => TraStFileService.GetRecentFiles();

		/// <inheritdoc cref="TraStFileService.ShowOpenFileDialogAsync"/>
		public static Task<string?> ShowOpenFileDialogAsync() => TraStFileService.ShowOpenFileDialogAsync();

		/// <inheritdoc cref="TraStFileService.ParseFile"/>
		public static Triamec.Tam.Motion.TrajectoryStreaming.Parser.TraStHeader ParseFile(string filePath) {
			var header = TraStFileService.ParseFile(filePath);
			TraStFileService.AddToRecentFiles(filePath);
			return header;
		}
	}
}
