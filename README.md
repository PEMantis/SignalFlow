![CI](../../actions/workflows/ci.yml/badge.svg)

# SignalFlow (MVP)

SignalFlow is a multi-tenant AI governance platform that combines **structured LLM evaluation** with **deterministic policy enforcement**, **auditability**, and **replay**.

This MVP demonstrates:
- Versioned prompt templates
- Schema-validated AI outputs (structured JSON)
- Deterministic policy checks and final decision resolution
- Full audit records including model, latency, token counts, and prompt version
- Human override support
- Replay evaluation (re-run policies without re-calling the model)

## Architecture

- **API:** .NET 9 Web API
- **Data:** EF Core + SQLite (initially)
- **UI:** Angular (light admin UI; optional for MVP demo)
- **Tenancy:** Tenant-scoped config (per-tenant model selection, thresholds, toggles)

### Request Flow

1. Client submits an input payload (JSON)
2. Prompt is rendered from a **versioned template**
3. Model produces **structured JSON output**
4. Output is **schema validated**
5. Policies run deterministically, producing policy checks + final decision
6. Audit record stored (includes model + latency + token counts + prompt version + hash)
7. Optional: replay evaluation and/or human override

## Quickstart

### Run the API
```bash
cd api
dotnet restore
dotnet run --project ./SignalFlow.Api/SignalFlow.Api.csproj
