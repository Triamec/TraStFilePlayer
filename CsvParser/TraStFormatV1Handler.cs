// Copyright © 2026 Triamec Motion AG
#nullable disable

using System.Globalization;
using System.Text.RegularExpressions;

namespace TraStFilePlayer.CsvParser {
	internal sealed partial class TraStFormatV1Handler : ITraStFormatHandler {
		static readonly Regex AxisParamRegex = AxisRegex();

		public int Version => 1;

		public void ApplyParameter(
			TraStHeaderBuilder header,
			string name,
			string value,
			SortedDictionary<int, TraStAxisDefinitionBuilder> axesByIndex,
			int lineNumber) {

			if (string.Equals(name, "gp.FileFormatVersion", StringComparison.OrdinalIgnoreCase)) {
				header.FileFormatVersion = TraStCsvCommon.ParseInt(value, name, lineNumber);
				return;
			}
			if (string.Equals(name, "gp.NumberOfDataRows", StringComparison.OrdinalIgnoreCase)) {
				header.NumberOfDataRows = TraStCsvCommon.ParseInt(value, name, lineNumber);
				return;
			}
			if (string.Equals(name, "gp.SamplingFrequencyHz", StringComparison.OrdinalIgnoreCase)) {
				header.SamplingFrequencyHz = TraStCsvCommon.ParseDouble(value, name, lineNumber);
				return;
			}
			if (string.Equals(name, "gp.NextFile", StringComparison.OrdinalIgnoreCase)) {
				header.NextFilePath = TraStCsvCommon.UnquoteString(value);
				return;
			}

			var m = AxisParamRegex.Match(name);
			if (!m.Success) {
				throw new InvalidOperationException(
					$"Unsupported header parameter '{name}' in line {lineNumber} for file format version {Version}.");
			}

			int axisIndex = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
			string paramName = m.Groups[2].Value;
			TraStAxisDefinitionBuilder axis = GetOrCreateAxis(axesByIndex, axisIndex);

			if (string.Equals(paramName, "AxisIdentifier", StringComparison.OrdinalIgnoreCase)) {
				axis.AxisIdentifier = TraStCsvCommon.UnquoteString(value);
				return;
			}
			if (string.Equals(paramName, "PositionColumnIndex", StringComparison.OrdinalIgnoreCase)) {
				axis.PositionColumnIndex = TraStCsvCommon.ParseInt(value, name, lineNumber);
				return;
			}
			if (string.Equals(paramName, "CurrentColumnIndex", StringComparison.OrdinalIgnoreCase)) {
				axis.CurrentColumnIndex = TraStCsvCommon.ParseInt(value, name, lineNumber);
				return;
			}
			if (string.Equals(paramName, "VelocityColumnIndex", StringComparison.OrdinalIgnoreCase)) {
				axis.VelocityColumnIndex = TraStCsvCommon.ParseInt(value, name, lineNumber);
				return;
			}
			if (paramName.StartsWith("CustomExtensionColumnIndex", StringComparison.OrdinalIgnoreCase)) {
				if (!m.Groups[3].Success) {
					throw new InvalidOperationException(
						$"'{name}' in line {lineNumber} requires a sub-index, e.g. CustomExtensionColumnIndex[0].");
				}
				int subIndex = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
				TraStCsvCommon.SetIndexedColumn(
					axis.CustomExtensionColumnIndices,
					subIndex,
					TraStCsvCommon.ParseInt(value, name, lineNumber),
					name,
					lineNumber);
				return;
			}
		}

		public TraStHeader CreateHeader(TraStHeaderBuilder header, SortedDictionary<int, TraStAxisDefinitionBuilder> axesByIndex) {
			if (header.FileFormatVersion != Version) {
				throw new InvalidOperationException(
					$"Unsupported file format version {header.FileFormatVersion}. Only version {Version} is supported by this handler.");
			}
			if (header.NumberOfDataRows <= 0) {
				throw new ArgumentOutOfRangeException(
					nameof(header),
					header.NumberOfDataRows,
					"gp.NumberOfDataRows must be greater than zero.");
			}
			if (axesByIndex.Count <= 0) {
				throw new InvalidOperationException("At least one axis parameter group ax[n] is required.");
			}
			if (header.SamplingFrequencyHz <= 0) {
				throw new ArgumentOutOfRangeException(
					nameof(header),
					header.SamplingFrequencyHz,
					"gp.SamplingFrequencyHz must be greater than zero.");
			}

			var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var axes = new List<TraStAxisDefinition>(axesByIndex.Count);

			foreach (var pair in axesByIndex) {
				TraStAxisDefinitionBuilder axis = pair.Value;
				int axisIndex = axis.AxisIndex;
				if (string.IsNullOrWhiteSpace(axis.AxisIdentifier)) {
					throw new InvalidOperationException($"ax[{axisIndex}].AxisIdentifier is missing or empty.");
				}
				if (axis.PositionColumnIndex < 0) {
					throw new ArgumentOutOfRangeException(
						nameof(axesByIndex),
						axis.PositionColumnIndex,
						$"ax[{axisIndex}].PositionColumnIndex is missing or invalid.");
				}
				if (!identifiers.Add(axis.AxisIdentifier)) {
					throw new InvalidOperationException(
						$"ax[{axisIndex}].AxisIdentifier '{axis.AxisIdentifier}' is not unique.");
				}

				axes.Add(new TraStAxisDefinition(
					axis.AxisIndex,
					axis.AxisIdentifier,
					axis.PositionColumnIndex,
					axis.CurrentColumnIndex,
					axis.VelocityColumnIndex,
					axis.CustomExtensionColumnIndices));
			}

			return new TraStHeader(
				header.FileFormatVersion,
				header.NumberOfDataRows,
				header.SamplingFrequencyHz,
				header.NextFilePath,
				axes);
		}

		static TraStAxisDefinitionBuilder GetOrCreateAxis(SortedDictionary<int, TraStAxisDefinitionBuilder> dict, int index) {
			if (!dict.TryGetValue(index, out TraStAxisDefinitionBuilder axis)) {
				axis = new TraStAxisDefinitionBuilder { AxisIndex = index };
				dict[index] = axis;
			}
			return axis;
		}

		[GeneratedRegex(@"^ax\[(\d+)\]\.(AxisIdentifier|PositionColumnIndex|CurrentColumnIndex|VelocityColumnIndex|CustomExtensionColumnIndex(?:\[(\d+)\])?)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		private static partial Regex AxisRegex();
	}
}
