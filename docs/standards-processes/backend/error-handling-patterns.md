# Error Handling — pointer to the standard

> **This file has been superseded by `error-handling-standard.md` in the same folder.**
> It is preserved as a breadcrumb so links from older docs / session-work / commit messages still resolve.

**For the current, authoritative rules** on how errors flow from service → endpoint → HTTP client, see:

- **[/docs/standards-processes/backend/error-handling-standard.md](./error-handling-standard.md)** — the full RFC 7807 ProblemDetails standard: `Result<T>`, `ResultErrorKind`, `ToProblem(title)`, `GlobalExceptionHandler`, `EndpointErrorShapeTests`.
- **[CLAUDE.md → "Error Responses"](../../../CLAUDE.md)** — quick-reference table mapping `ResultErrorKind` to HTTP status codes.

## Why this file exists as a pointer (not content)

Per the repo's single-source-of-truth rule (CLAUDE.md), every piece of information lives in exactly one place. The prior version of this file predated the standardized cross-repo error handling pattern and contained earlier Result-pattern usage examples that have since been subsumed by the full standard. Keeping the old body here alongside the new standard would create drift (two sources for the same topic). Keeping the filename with a forward-pointer preserves backward compatibility for existing links while the standard file becomes the single source.

## See also

- **[/docs/standards-processes/backend/serilog-logging-guide.md](./serilog-logging-guide.md)** — still the source for Serilog-specific patterns (message templates, enrichers, PostgreSQL sink).
- **[/docs/standards-processes/backend/service-layer-patterns.md](./service-layer-patterns.md)** — service-layer conventions (interface design, dependency injection) that are orthogonal to the error-handling wire format.
