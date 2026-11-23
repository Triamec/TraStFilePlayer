# TraSt CSV Versioning Policy

- Header contract changes are strict and versioned.
- Any addition, removal, or semantic change of supported header parameters requires incrementing `gp.FileFormatVersion`.
- Each file format version must have its own explicit parser handler (`ITraStFormatHandler`).
- Unknown parameters for a given version must fail parsing.
- Concurrent support for multiple released versions is provided by registering multiple handlers in `TraStFormatRegistry`.
