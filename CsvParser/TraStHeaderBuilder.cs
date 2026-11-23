// Copyright © 2026 Triamec Motion AG
#nullable disable

namespace TraStFilePlayer.CsvParser {
	internal sealed class TraStHeaderBuilder {
		public int FileFormatVersion { get; set; }
		public int NumberOfDataRows { get; set; }
		public double SamplingFrequencyHz { get; set; }
		public string NextFilePath { get; set; }
	}

	internal sealed class TraStAxisDefinitionBuilder {
		public int AxisIndex { get; set; }
		public string AxisIdentifier { get; set; } = string.Empty;
		public int PositionColumnIndex { get; set; } = -1;
		public int? CurrentColumnIndex { get; set; }
		public int? VelocityColumnIndex { get; set; }
		public List<int> CustomExtensionColumnIndices { get; } = [];
	}
}
