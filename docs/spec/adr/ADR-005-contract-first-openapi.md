# ADR-005: Contract-First OpenAPI (`docs/contracts/openapi.json`) & Generated TypeScript Client

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** `backend-developer`, `frontend-developer`, `project-manager`
- **References:** `DEC-001`, `AGENTS.md` §3, `docs/spec/07-API-SPECIFICATION.md`

---

## Context

In a multi-agent workflow (`backend-developer`, `payment-specialist`, `frontend-developer`, `qa-engineer`), a frequent source of integration defects is drift between backend C# DTOs and frontend TypeScript interfaces (mismatched field names, nullable vs non-nullable fields, enum string casing, or error payload shapes).

`AGENTS.md` §3 mandates:
> *"The OpenAPI document `docs/contracts/openapi.json` is the contract between backend and frontend. Backend regenerates it; frontend generates TS types from it. No hand-written duplicate DTOs on the frontend."*

---

## Decision

We enforce a strict **OpenAPI 3.1 Contract-Driven Workflow**:

1. **Specification Baseline (`docs/spec/07-API-SPECIFICATION.md`):**
   - `docs/spec/07-API-SPECIFICATION.md` (`v1.0`) defines every REST endpoint under `/api/v1`, its owning module, permission policy, request/response schemas, and domain error codes.
   - Any breaking change to an endpoint path, method, or schema after Phase 03 requires a written Change Request (`docs/agent-notes/CR-<nnn>-<slug>.md`).

2. **ASP.NET Core 10 Minimal API Metadata (`TypedResults` + `Microsoft.AspNetCore.OpenApi`):**
   - Every Minimal API endpoint uses `TypedResults` (e.g., `Results<Ok<OrderDetailDto>, ProblemHttpResult>`) and explicit `.WithName(...)`, `.WithTags(...)`, `.Produces<T>(...)`, and `.ProducesProblem(...)` metadata so ASP.NET Core's built-in OpenAPI 3.1 generator produces complete schemas for all parameters, request bodies, responses, enums, and security schemes.
   - Security schemes (`BearerAuth` JWT and `RefreshCookie`) and endpoint permission policies are emitted into the OpenAPI operation metadata.

3. **Committed Contract Artifact (`docs/contracts/openapi.json`):**
   - `backend-developer`owns `docs/contracts/openapi.json`.
   - Whenever backend endpoints or DTOs are added or updated, `backend-developer` regenerates `docs/contracts/openapi.json` from the build/CLI and commits it alongside the backend code.
   - CI verifies that running the OpenAPI export produces zero git diff against the committed `docs/contracts/openapi.json`.

4. **Frontend Type Generation (`openapi-typescript`):**
   - `frontend-developer` runs `openapi-typescript` against `../docs/contracts/openapi.json` to generate `web/src/shared/api/schema.d.ts`.
   - All TanStack Query hooks and API client calls in `web/src/**` import their request/response types from the generated schema; hand-written duplicate backend DTO interfaces in `web/src/` are forbidden.

---

## Alternatives Considered

1. **Hand-written TypeScript interfaces in `web/src/`:**
   - *Rejected:* Guaranteed to drift from C# records as fields evolve, hiding bugs until E2E runtime and violating `AGENTS.md` §3.
2. **Writing raw YAML OpenAPI manually by hand without code generation:**
   - *Rejected:* Hand-maintained YAML files of several thousand lines quickly diverge from the actual C# Minimal API implementation unless verified by code generation.

---

## Consequences

- **Positive:**
  - Compile-time type safety across the HTTP boundary from C# records (`GroceryHub.Modules.*.Contracts`) through `docs/contracts/openapi.json` to TypeScript (`strict: true`).
  - Any backend contract change immediately surfaces as a TypeScript compiler error in `npm run build` if the frontend is not updated.
- **Negative / Trade-offs:**
  - Minimal API endpoints must always declare accurate `TypedResults` return types rather than untyped `IResult`.
