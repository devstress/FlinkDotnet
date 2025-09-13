# IR Versioning

Policy:
- IR schema uses SemVer-like `major.minor` in `JobMetadata.version`.
- Backward-compatible changes increment minor (additive fields with defaults).
- Breaking changes increment major and require dual-support window in Gateway/Runner.

Compatibility:
- Gateway/Runner accept N and N-1 majors during transition windows.
- JSON Schema files are frozen per version (see `docs/ir-schema-v1.json`).

