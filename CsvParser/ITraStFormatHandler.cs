// Copyright © 2026 Triamec Motion AG

namespace TraStFilePlayer.CsvParser {
	internal interface ITraStFormatHandler {
		int Version { get; }

		void ApplyParameter(
			TraStHeaderBuilder header,
			string name,
			string value,
			SortedDictionary<int, TraStAxisDefinitionBuilder> axesByIndex,
			int lineNumber);

		TraStHeader CreateHeader(TraStHeaderBuilder header, SortedDictionary<int, TraStAxisDefinitionBuilder> axesByIndex);
	}
}
