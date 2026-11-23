// Copyright © 2026 Triamec Motion AG

using System.Collections.ObjectModel;

namespace TraStFilePlayer.CsvParser {
	/// <summary>
	/// Represents the parsed TraSt CSV header metadata.
	/// </summary>
	internal sealed class TraStHeader {
		readonly ReadOnlyCollection<TraStAxisDefinition> _axes;

		/// <summary>
		/// Initializes a new instance of the <see cref="TraStHeader"/> class.
		/// </summary>
		/// <param name="fileFormatVersion">The TraSt file format version.</param>
		/// <param name="numberOfDataRows">The number of trajectory data rows declared in the header.</param>
		/// <param name="samplingFrequencyHz">The sampling frequency in Hertz.</param>
		/// <param name="nextFilePath">The optional next trajectory file path.</param>
		/// <param name="axes">The axis definitions declared in the header.</param>
		public TraStHeader(
			int fileFormatVersion,
			int numberOfDataRows,
			double samplingFrequencyHz,
			string nextFilePath,
			IEnumerable<TraStAxisDefinition> axes) {

			if (axes == null) throw new ArgumentNullException(nameof(axes));

			FileFormatVersion = fileFormatVersion;
			NumberOfDataRows = numberOfDataRows;
			SamplingFrequencyHz = samplingFrequencyHz;
			NextFilePath = nextFilePath;
			_axes = new ReadOnlyCollection<TraStAxisDefinition>(axes.ToArray());
			NumberOfInvolvedAxes = _axes.Count;
		}

		/// <summary>
		/// Gets the TraSt file format version.
		/// </summary>
		public int FileFormatVersion { get; }

		/// <summary>
		/// Gets the number of trajectory data rows declared in the header.
		/// </summary>
		public int NumberOfDataRows { get; }

		/// <summary>
		/// Gets the number of axes declared in the header.
		/// </summary>
		public int NumberOfInvolvedAxes { get; }

		/// <summary>
		/// Gets the sampling frequency in Hertz.
		/// </summary>
		public double SamplingFrequencyHz { get; }

		/// <summary>
		/// Gets the optional next trajectory file path.
		/// </summary>
		public string NextFilePath { get; }

		/// <summary>
		/// Gets the axis definitions declared in the header.
		/// </summary>
		public IReadOnlyList<TraStAxisDefinition> Axes => _axes;
	}
}
