// Copyright © 2026 Triamec Motion AG

using Triamec.Tam.Motion.TrajectoryStreaming.Parser;

namespace TraStFilePlayer.Services {
	/// <summary>
	/// Provides file-open dialog support, recent-file list management (persisted via
	/// <see cref="Preferences"/>) and TraSt CSV parsing.
	/// </summary>
	internal sealed class TraStFileService {
		const string PrefKeyRecentFiles = "RecentFiles";
		const int MaxRecentFiles = 10;
		const char ListSeparator = '|';

		// ── Recent-file list ────────────────────────────────────────────────────

		/// <summary>Returns the persisted list of recently opened files, most-recent first.</summary>
		public static IReadOnlyList<string> GetRecentFiles() {
			try {
				string raw = Preferences.Get(PrefKeyRecentFiles, string.Empty);
				if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
				return raw.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);
			} catch (InvalidOperationException) {
				return Array.Empty<string>();
			}
		}

		/// <summary>Inserts <paramref name="filePath"/> at the top of the recent-files list.</summary>
		public static void AddToRecentFiles(string filePath) {
			try {
				var list = new List<string>(GetRecentFiles());
				list.RemoveAll(f => string.Equals(f, filePath, StringComparison.OrdinalIgnoreCase));
				list.Insert(0, filePath);
				if (list.Count > MaxRecentFiles) list.RemoveRange(MaxRecentFiles, list.Count - MaxRecentFiles);
				Preferences.Set(PrefKeyRecentFiles, string.Join(ListSeparator, list));
			} catch (InvalidOperationException) {
				// Persistence failures are non-critical.
			}
		}

		// ── File-open dialog ────────────────────────────────────────────────────

		/// <summary>
		/// Opens a MAUI file picker and returns the selected path, or
		/// <see langword="null"/> if the user cancelled.
		/// </summary>
		public static async Task<string?> ShowOpenFileDialogAsync() {
			FileResult? file = await FilePicker.PickAsync(new PickOptions {
				PickerTitle = "Select TraSt CSV file"
			}).ConfigureAwait(false);

			return file?.FullPath;
		}

		// ── CSV parsing ─────────────────────────────────────────────────────────

		/// <summary>
		/// Parses the TraSt header from <paramref name="filePath"/> and validates column indices.
		/// Returns the parsed <see cref="TraStHeader"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">File is missing, malformed or fails validation.</exception>
		public static TraStHeader ParseFile(string filePath) {
			if (!File.Exists(filePath)) {
				throw new InvalidOperationException($"File not found: {filePath}");
			}

			using var parser = new TraStCsvParser(filePath);
			return parser.Header;
		}
	}
}
