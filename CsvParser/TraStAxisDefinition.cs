// Copyright © 2026 Triamec Motion AG

using System.Collections.ObjectModel;

namespace TraStFilePlayer.CsvParser {
	/// <summary>
	/// Describes the CSV column mapping for one trajectory streaming axis.
	/// </summary>
	internal sealed class TraStAxisDefinition {
		readonly ReadOnlyCollection<int> _customExtensionColumnIndices;

		/// <summary>
		/// Initializes a new instance of the <see cref="TraStAxisDefinition"/> class.
		/// </summary>
		/// <param name="axisIndex">The zero-based axis index defined in the TraSt header.</param>
		/// <param name="axisIdentifier">The axis identifier used in the TraSt header.</param>
		/// <param name="positionColumnIndex">The zero-based CSV column index that contains the position values.</param>
		/// <param name="currentColumnIndex">The optional zero-based CSV column index that contains the current values.</param>
		/// <param name="velocityColumnIndex">The optional zero-based CSV column index that contains the velocity values.</param>
		/// <param name="customExtensionColumnIndices">The zero-based CSV column indices that contain custom extension values.</param>
		public TraStAxisDefinition(
			int axisIndex,
			string axisIdentifier,
			int positionColumnIndex,
			int? currentColumnIndex,
			int? velocityColumnIndex,
			IEnumerable<int> customExtensionColumnIndices) {

			if (axisIdentifier == null) throw new ArgumentNullException(nameof(axisIdentifier));
			if (customExtensionColumnIndices == null) throw new ArgumentNullException(nameof(customExtensionColumnIndices));

			AxisIndex = axisIndex;
			AxisIdentifier = axisIdentifier;
			PositionColumnIndex = positionColumnIndex;
			CurrentColumnIndex = currentColumnIndex;
			VelocityColumnIndex = velocityColumnIndex;
			_customExtensionColumnIndices = new ReadOnlyCollection<int>(customExtensionColumnIndices.ToArray());
		}

		/// <summary>
		/// Gets the zero-based axis index defined in the TraSt header.
		/// </summary>
		public int AxisIndex { get; }

		/// <summary>
		/// Gets the axis identifier used in the TraSt header.
		/// </summary>
		public string AxisIdentifier { get; }

		/// <summary>
		/// Gets the zero-based CSV column index that contains the position values.
		/// </summary>
		public int PositionColumnIndex { get; }

		/// <summary>
		/// Gets the optional zero-based CSV column index that contains the current values.
		/// </summary>
		public int? CurrentColumnIndex { get; }

		/// <summary>
		/// Gets the optional zero-based CSV column index that contains the velocity values.
		/// </summary>
		public int? VelocityColumnIndex { get; }

		/// <summary>
		/// Gets the zero-based CSV column indices that contain custom extension values.
		/// </summary>
		public IReadOnlyList<int> CustomExtensionColumnIndices => _customExtensionColumnIndices;
	}
}
