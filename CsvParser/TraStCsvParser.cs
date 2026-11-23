// Copyright © 2026 Triamec Motion AG
#nullable disable
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional. Reason: TraSt CSV data is naturally represented as a 2D array.

namespace TraStFilePlayer.CsvParser {
	/// <summary>
	/// Parses trajectory streaming CSV files and exposes header and data access helpers.
	/// </summary>
	internal sealed partial class TraStCsvParser : IDisposable {
		readonly StreamReader _reader;
		readonly string _filePath;
		readonly TraStFormatRegistry _formatRegistry;
		readonly object _firstDataRowSync = new object();
		int _dataRowsRead;
		string[] _firstDataRowParts;
		int? _firstDataRowColumnCount;

		/// <summary>
		/// Gets the parsed TraSt header.
		/// </summary>
		public TraStHeader Header { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TraStCsvParser"/> class for the specified CSV file.
		/// </summary>
		/// <param name="filePath">The path to the TraSt CSV file.</param> 
		public TraStCsvParser(string filePath)
			: this(filePath, new TraStFormatRegistry()) {
		}

		internal TraStCsvParser(string filePath, TraStFormatRegistry formatRegistry) {
			_filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
			_formatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
			_reader = new StreamReader(filePath);
			try {
				Header = ParseHeader();
				ValidateColumnIndices();
			} catch {
				_reader.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Determines whether unread trajectory data rows are still available.
		/// </summary>
		/// <returns><see langword="true"/> if more data rows can be read; otherwise, <see langword="false"/>.</returns>
		public bool HasMoreData() => _dataRowsRead < Header.NumberOfDataRows && !_reader.EndOfStream;

		/// <summary>
		/// Reads the first trajectory data row without advancing the current parser position.
		/// </summary>
		/// <param name="positionColumnIndices">The zero-based position column indices to extract.</param>
		/// <returns>The parsed position values from the first trajectory data row.</returns>
		public double[] PeekFirstDataRow(IReadOnlyList<int> positionColumnIndices) {
			if (positionColumnIndices == null) throw new ArgumentNullException(nameof(positionColumnIndices));
			var parts = ReadFirstDataRowParts();
			var values = new double[positionColumnIndices.Count];
			for (int i = 0; i < positionColumnIndices.Count; i++) {
				values[i] = TraStCsvCommon.ParseDouble(parts, positionColumnIndices[i], "first data row");
			}
			return values;
		}

		/// <summary>
		/// Reads the first trajectory data row for the specified axis identifiers without advancing the current parser position.
		/// </summary>
		/// <param name="axisIdentifiers">The axis identifiers whose position values should be extracted.</param>
		/// <returns>The parsed position values from the first trajectory data row.</returns>
		public double[] PeekFirstDataRow(IReadOnlyList<string> axisIdentifiers) {
			var positionColumnIndices = ResolvePositionColumnIndices(axisIdentifiers);
			return PeekFirstDataRow(positionColumnIndices);
		}

		/// <summary>
		/// Gets the number of columns present in the first trajectory data row.
		/// </summary>
		/// <returns>The number of columns in the first trajectory data row.</returns>
		public int GetFirstDataRowColumnCount() {
			if (_firstDataRowColumnCount.HasValue) {
				return _firstDataRowColumnCount.Value;
			}

			string[] parts = ReadFirstDataRowParts();
			return _firstDataRowColumnCount ?? parts.Length;
		}

		/// <summary>
		/// Reads up to the specified number of trajectory points from the current parser position.
		/// </summary>
		/// <param name="maxPoints">The maximum number of data rows to read.</param>
		/// <param name="positionColumns">The zero-based position column indices to extract.</param>
		/// <param name="extensionColumns">The zero-based extension column indices to extract.</param>
		/// <returns>The position and extension buffers trimmed to the number of rows that were actually read.</returns>
		public (double[,] positions, float[,] extensions) ReadSegment(
			int maxPoints,
			IReadOnlyList<int> positionColumns,
			IReadOnlyList<int> extensionColumns) {
			if (positionColumns == null) throw new ArgumentNullException(nameof(positionColumns));
			if (extensionColumns == null) throw new ArgumentNullException(nameof(extensionColumns));
			if (maxPoints < 0) throw new ArgumentOutOfRangeException(nameof(maxPoints), maxPoints, $"{nameof(maxPoints)} must not be negative.");
			var posBuf = new double[maxPoints, positionColumns.Count];
			float[,] extBuf = extensionColumns.Count > 0
				? new float[maxPoints, extensionColumns.Count]
				: null;

			int row = 0;
			while (row < maxPoints && _dataRowsRead < Header.NumberOfDataRows) {
				string line = _reader.ReadLine();
				if (line == null) {
					break;
				}
				if (string.IsNullOrWhiteSpace(line)) {
					continue;
				}

				var parts = line.Split(',');

				for (int c = 0; c < positionColumns.Count; c++) {
					posBuf[row, c] = TraStCsvCommon.ParseDouble(parts, positionColumns[c], $"row {row}");
				}

				if (extBuf != null) {
					for (int c = 0; c < extensionColumns.Count; c++) {
						extBuf[row, c] = TraStCsvCommon.ParseFloat(parts, extensionColumns[c], $"row {row}");
					}
				}

				row++;
				_dataRowsRead++;
			}

			return (
				TraStCsvCommon.TrimDouble(posBuf, row, positionColumns.Count),
				extBuf == null ? null : TraStCsvCommon.TrimFloat(extBuf, row, extensionColumns.Count));
		}

		/// <summary>
		/// Reads up to the specified number of trajectory points for the specified axis identifiers from the current parser position.
		/// </summary>
		/// <param name="maxPoints">The maximum number of data rows to read.</param>
		/// <param name="axisIdentifiers">The axis identifiers whose position columns should be extracted.</param>
		/// <param name="extensionColumns">The zero-based extension column indices to extract.</param>
		/// <returns>The position and extension buffers trimmed to the number of rows that were actually read.</returns>
		public (double[,] positions, float[,] extensions) ReadSegment(
			int maxPoints,
			IReadOnlyList<string> axisIdentifiers,
			IReadOnlyList<int> extensionColumns) {

			var positionColumns = ResolvePositionColumnIndices(axisIdentifiers);
			return ReadSegment(maxPoints, positionColumns, extensionColumns);
		}

		/// <summary>
		/// Releases the underlying file reader.
		/// </summary>
		public void Dispose() => _reader.Dispose();

		/// <summary>
		/// Validates that all header-defined column indices are within the available data range.
		/// </summary>
		private void ValidateColumnIndices() {
			int columnCount = GetFirstDataRowColumnCount();
			foreach (var axis in Header.Axes) {
				EnsureColumnInRange(axis.PositionColumnIndex, columnCount,
					$"ax[{axis.AxisIndex}].PositionColumnIndex");

				if (axis.CurrentColumnIndex.HasValue) {
					EnsureColumnInRange(axis.CurrentColumnIndex.Value, columnCount,
						$"ax[{axis.AxisIndex}].CurrentColumnIndex");
				}

				if (axis.VelocityColumnIndex.HasValue) {
					EnsureColumnInRange(axis.VelocityColumnIndex.Value, columnCount,
						$"ax[{axis.AxisIndex}].VelocityColumnIndex");
				}

				for (int i = 0; i < axis.CustomExtensionColumnIndices.Count; i++) {
					EnsureColumnInRange(axis.CustomExtensionColumnIndices[i], columnCount,
						$"ax[{axis.AxisIndex}].CustomExtensionColumnIndex[{i}]");
				}
			}
		}

		TraStHeader ParseHeader() {
			var tokens = new List<TraStHeaderToken>();
			bool endFound = ReadHeaderTokens(tokens);
			if (!endFound) {
				throw new InvalidOperationException(
					$"TraSt header does not contain the required end marker '{TraStCsvCommon.EndOfHeaderMarker}'.");
			}

			ValidateDuplicateGlobalParameters(tokens);
			int version = ResolveFileFormatVersion(tokens);
			var handler = _formatRegistry.Resolve(version);
			var header = new TraStHeaderBuilder();
			var axesByIndex = new SortedDictionary<int, TraStAxisDefinitionBuilder>();
			foreach (var token in tokens) {
				handler.ApplyParameter(header, token.Name, token.Value, axesByIndex, token.LineNumber);
			}

			header.NextFilePath = ResolveAndValidateNextFilePath(header.NextFilePath);
			return handler.CreateHeader(header, axesByIndex);
		}

		string ResolveAndValidateNextFilePath(string nextFilePath) {
			if (string.IsNullOrWhiteSpace(nextFilePath)) {
				return nextFilePath;
			}

			string currentDirectory = Path.GetDirectoryName(_filePath);
			if (string.IsNullOrWhiteSpace(currentDirectory)) {
				throw new InvalidOperationException("Cannot resolve the directory of the current TraSt CSV file.");
			}

			string resolvedPath = Path.GetFullPath(nextFilePath, currentDirectory);
			if (!File.Exists(resolvedPath)) {
				throw new FileNotFoundException(
					$"The next trajectory file '{nextFilePath}' (resolved to '{resolvedPath}') does not exist.",
					resolvedPath);
			}

			return resolvedPath;
		}

		bool ReadHeaderTokens(List<TraStHeaderToken> tokens) {
			int lineNumber = 0;
			string line;
			while ((line = _reader.ReadLine()) != null) {
				lineNumber++;
				string content = TraStCsvCommon.StripComment(line).Trim();
				if (string.IsNullOrWhiteSpace(content)) {
					continue;
				}

				if (string.Equals(content, TraStCsvCommon.EndOfHeaderMarker, StringComparison.OrdinalIgnoreCase)) {
					return true;
				}

				var (name, value) = TraStCsvCommon.SplitParameter(content, lineNumber);
				tokens.Add(new TraStHeaderToken(lineNumber, name, value));
			}

			return false;
		}

		static int ResolveFileFormatVersion(List<TraStHeaderToken> tokens) {
			foreach (var token in tokens) {
				if (string.Equals(token.Name, "gp.FileFormatVersion", StringComparison.OrdinalIgnoreCase)) {
					return TraStCsvCommon.ParseInt(token.Value, token.Name, token.LineNumber);
				}
			}

			throw new InvalidOperationException("Missing required header parameter 'gp.FileFormatVersion'.");
		}

		static void ValidateDuplicateGlobalParameters(List<TraStHeaderToken> tokens) {
			var linesByParameterName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var token in tokens) {
				if (!token.Name.StartsWith("gp.", StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (linesByParameterName.TryGetValue(token.Name, out int firstLineNumber)) {
					throw new InvalidOperationException(
						$"Duplicate parameter '{token.Name}' in line {token.LineNumber}. First declared in line {firstLineNumber}.");
				}

				linesByParameterName.Add(token.Name, token.LineNumber);
			}
		}

		string[] ReadFirstDataRowParts() {
			if (_firstDataRowParts != null) {
				return _firstDataRowParts;
			}

			lock (_firstDataRowSync) {
				if (_firstDataRowParts != null) {
					return _firstDataRowParts;
				}

				using var reader = new StreamReader(_filePath);
				bool inData = false;
				string line;
				while ((line = reader.ReadLine()) != null) {
					if (!inData) {
						if (string.Equals(TraStCsvCommon.StripComment(line).Trim(), TraStCsvCommon.EndOfHeaderMarker, StringComparison.OrdinalIgnoreCase)) {
							inData = true;
						}
						continue;
					}

					if (string.IsNullOrWhiteSpace(line)) {
						continue;
					}

					_firstDataRowParts = line.Split(',');
					_firstDataRowColumnCount = _firstDataRowParts.Length;
					return _firstDataRowParts;
				}
			}

			throw new InvalidOperationException("CSV file contains no trajectory data rows.");
		}

		int[] ResolvePositionColumnIndices(IReadOnlyList<string> axisIdentifiers) {
			if (axisIdentifiers == null) {
				throw new ArgumentNullException(nameof(axisIdentifiers));
			}

			var result = new int[axisIdentifiers.Count];
			for (int i = 0; i < axisIdentifiers.Count; i++) {
				string axisIdentifier = axisIdentifiers[i];
				if (string.IsNullOrWhiteSpace(axisIdentifier)) {
					throw new InvalidOperationException($"Axis identifier at index {i} must not be empty.");
				}

				TraStAxisDefinition axis = null;
				foreach (var candidate in Header.Axes) {
					if (string.Equals(candidate.AxisIdentifier, axisIdentifier, StringComparison.OrdinalIgnoreCase)) {
						axis = candidate;
						break;
					}
				}

				if (axis == null) {
					throw new InvalidOperationException(
						$"Axis '{axisIdentifier}' was not found in TraSt header.");
				}

				result[i] = axis.PositionColumnIndex;
			}

			return result;
		}

		static void EnsureColumnInRange(int index, int columnCount, string paramName) {
			if (index < 0) {
				throw new ArgumentOutOfRangeException(paramName, index, $"{paramName} must not be negative.");
			}
			if (index >= columnCount) {
				throw new ArgumentOutOfRangeException(
					paramName,
					index,
					$"{paramName} references column {index} but the data has only {columnCount} columns.");
			}
		}
	}
}
