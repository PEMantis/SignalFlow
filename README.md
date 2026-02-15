# SignalFlow

SignalFlow is a multi-tenant AI governance platform that combines:

- LLM evaluation
- Deterministic policy enforcement
- Schema validation
- Full audit logging
- Replay evaluation
- Human override tracking

## Architecture

- .NET 9 Web API
- EF Core + SQLite
- Clean Architecture (Domain / Application / Infrastructure)
- Tenant-scoped configuration
- Structured AI JSON enforcement

## Key Features

- Versioned prompt templates
- Schema-validated AI outputs
- Deterministic decision resolution
- Immutable audit records with hash
- Replay without model re-execution
- Human override with traceability
