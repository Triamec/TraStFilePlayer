// Copyright © 2026 Triamec Motion AG
#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional. Justification: TraSt CSV data is naturally represented as a 2D array.

using System.Globalization;

namespace TraStFilePlayer.CsvParser {
	internal static class TraStCsvCommon {
		public const string EndOfHeaderMarker = "--EndOfHeader--";

		public static string StripComment(string line) {
			bool inQuotes = false;
			for (int i = 0; i < line.Length; i++) {
				if (line[i] == '"') {
					inQuotes = !inQuotes;
					continue;
				}
				if (inQuotes) {
					continue;
				}
				if (line[i] == '#') {
					return line[..i];
				}
				if (line[i] == '/' && i + 1 < line.Length && line[i + 1] == '/') {
					return line[..i];
				}
			}
			return line;
		}

		public static (string name, string value) SplitParameter(string line, int lineNumber) {
			int ws = -1;
			for (int i = 0; i < line.Length; i++) {
				if (char.IsWhiteSpace(line[i])) {
					ws = i;
					break;
				}
			}

			if (ws < 0) {
				throw new InvalidOperationException(
					$"Header line {lineNumber} is not a valid 'name value' parameter: '{line}'.");
			}

			string parameterName = line[..ws].Trim();
			string parameterValue = line[ws..].Trim();

			if (parameterName.EndsWith(":", StringComparison.Ordinal) || parameterValue.StartsWith(":", StringComparison.Ordinal)) {
				throw new InvalidOperationException(
					$"Header line {lineNumber} is not a valid 'name value' parameter: '{line}'.");
			}

			EnsureValidParts(parameterName, parameterValue, lineNumber);
			return (parameterName, parameterValue);
		}

		public static string UnquoteString(string value) {
			string t = value.Trim();
			if (t.Length >= 2 && t[0] == '"' && t[^1] == '"') {
				return t[1..^1];
			}
			return t;
		}

		public static int ParseInt(string value, string paramName, int lineNumber) {
			if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)) {
				throw new InvalidOperationException(
					$"Cannot parse integer for '{paramName}' in line {lineNumber}: '{value}'.");
			}
			return result;
		}

		public static double ParseDouble(string value, string paramName, int lineNumber) {
			if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)) {
				throw new InvalidOperationException(
					$"Cannot parse number for '{paramName}' in line {lineNumber}: '{value}'.");
			}
			return result;
		}

		public static double ParseDouble(string[] parts, int column, string context) {
			if (column < 0 || column >= parts.Length) {
				throw new InvalidOperationException(
					$"Column {column} out of range ({parts.Length} columns) in {context}.");
			}
			if (!double.TryParse(parts[column].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) {
				throw new InvalidOperationException(
					$"Cannot parse double at column {column} in {context}: '{parts[column]}'.");
			}
			return v;
		}

		public static float ParseFloat(string[] parts, int column, string context) {
			if (column < 0 || column >= parts.Length) {
				throw new InvalidOperationException(
					$"Column {column} out of range ({parts.Length} columns) in {context}.");
			}
			if (!float.TryParse(parts[column].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) {
				throw new InvalidOperationException(
					$"Cannot parse float at column {column} in {context}: '{parts[column]}'.");
			}
			return v;
		}

		public static void SetIndexedColumn(IList<int> list, int subIndex, int columnIndex, string paramName, int lineNumber) {
			if (subIndex < 0) {
				throw new InvalidOperationException(
					$"Sub-index in '{paramName}' line {lineNumber} must not be negative.");
			}
			while (list.Count <= subIndex) {
				list.Add(-1);
			}
			if (list[subIndex] >= 0) {
				throw new InvalidOperationException($"Duplicate parameter '{paramName}' in line {lineNumber}.");
			}
			list[subIndex] = columnIndex;
		}

		public static double[,] TrimDouble(double[,] src, int rows, int cols) {
			if (rows == src.GetLength(0)) {
				return src;
			}
			var dst = new double[rows, cols];
			for (int r = 0; r < rows; r++) {
				for (int c = 0; c < cols; c++) {
					dst[r, c] = src[r, c];
				}
			}
			return dst;
		}

		public static float[,] TrimFloat(float[,] src, int rows, int cols) {
			if (rows == src.GetLength(0)) {
				return src;
			}
			var dst = new float[rows, cols];
			for (int r = 0; r < rows; r++) {
				for (int c = 0; c < cols; c++) {
					dst[r, c] = src[r, c];
				}
			}
			return dst;
		}

		static void EnsureValidParts(string name, string value, int lineNumber) {
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) {
				throw new InvalidOperationException(
					$"Header line {lineNumber} is not a valid parameter definition.");
			}
		}
	}
}
