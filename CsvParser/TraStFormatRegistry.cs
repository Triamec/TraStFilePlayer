// Copyright © 2026 Triamec Motion AG
#nullable disable

namespace TraStFilePlayer.CsvParser {
	internal sealed class TraStFormatRegistry {
		readonly Dictionary<int, ITraStFormatHandler> _handlersByVersion;

		public TraStFormatRegistry()
			: this(new ITraStFormatHandler[] { new TraStFormatV1Handler() }) {
		}

		internal TraStFormatRegistry(IEnumerable<ITraStFormatHandler> handlers) {
			_handlersByVersion = new Dictionary<int, ITraStFormatHandler>();
			foreach (var handler in handlers) {
				if (_handlersByVersion.ContainsKey(handler.Version)) {
					throw new InvalidOperationException($"Duplicate TraSt format handler for version {handler.Version}.");
				}
				_handlersByVersion.Add(handler.Version, handler);
			}
		}

		public ITraStFormatHandler Resolve(int version) {
			if (_handlersByVersion.TryGetValue(version, out ITraStFormatHandler handler)) {
				return handler;
			}

			throw new InvalidOperationException(
				$"Unsupported file format version {version}. No parser handler is registered for this version.");
		}
	}
}
