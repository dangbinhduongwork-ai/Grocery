# Phase 04 — Backend Foundation

**Goal:** A running, tested skeleton: solution, SharedKernel, API host, persistence project, migrations baseline, outbox, CI, Docker dev stack, architecture tests, integration-test harness.
**Agents:** devops-engineer ‖ backend-developer → database-engineer → qa-engineer → security-reviewer; PM integrates.
**Inputs:** requirements NFR-004 (health checks/graceful shutdown), NFR-005 (observability), NFR-009 (module boundaries, CI, licences); specs 05, 06, ADRs from Phase 03, `.agents/rules/backend.md`, `database.md`.
**Outputs:** `GroceryHub.sln`, `src/GroceryHub.{Api,SharedKernel,Persistence}`, empty module projects (12) with Contracts, `tests/*` projects, `docker-compose*.yml`, `.env.example`, `.github/workflows/ci.yml`, `docs/progress/COMMANDS.md`, `docs/contracts/openapi.json`, evidence.
**Dependencies:** Phase 03 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 04 — BACKEND FOUNDATION. Follow docs/spec/05-SYSTEM-ARCHITECTURE.md exactly; do not change the stack. Before adding any package check its licence (record in docs/progress/DEPENDENCIES.md); do NOT use MediatR, AutoMapper, FluentAssertions, MassTransit.

Plan branches and delegate:
1. devops-engineer (branch agent/devops-engineer/phase-04-skeleton): docker-compose.yml + docker-compose.override.yml with sqlserver (pinned verified tag, healthcheck, volume, MSSQL_PID per decision), mailpit (dev); .env.example (placeholders only); .gitignore check; ci.yml (restore, build -warnaserror, tests, dotnet format --verify-no-changes, vulnerable packages, gitleaks, verify-reality). Verify with `docker compose config` and `docker compose up -d sqlserver` + health.
2. backend-developer (branch agent/backend-developer/phase-04-solution): create solution + projects per doc 05 §3 (12 module class libraries with Domain/Application/Api/Contracts folders), Directory.Build.props (nullable, TreatWarningsAsErrors, analyzers, LangVersion latest), Directory.Packages.props (central versions), .editorconfig; SharedKernel (Money, Result/Error, DomainEvent, pagination, IClock via TimeProvider, Guard); Api host: Program.cs wiring, Serilog JSON + correlation id, ProblemDetails + global exception handler, FluentValidation integration, rate limiter policies, health endpoints (/health/live, /health/ready), OpenAPI document generation + script writing docs/contracts/openapi.json, module registration pattern (Add<Module>Module / Map<Module>Endpoints) with a real example endpoint GET /api/v1/system/info (version, environment) so the pipeline is proven; options validation on start; JWT/authz NOT yet (Phase 05).
3. database-engineer (after 2 compiles; branch agent/database-engineer/phase-04-persistence): GroceryHub.Persistence: GroceryHubDbContext (schema per module, configuration assembly scan), interceptors (CreatedAt/UpdatedAt via TimeProvider, soft-delete filter), outbox tables + OutboxDispatcher hosted service skeleton with a real handler registration mechanism, migration `20xxxxxx_Baseline` creating schemas + shared tables + sequences + READ_COMMITTED_SNAPSHOT script, migration bundle command documented in COMMANDS.md, DesignTime factory.
4. qa-engineer: architecture tests (rules in doc 05 §15), integration harness (WebApplicationFactory + Testcontainers SQL Server, respawn/reset), first integration tests: health ready, system/info, outbox dispatch idempotency, ProblemDetails shape for 404/422/500; unit tests for SharedKernel (Money arithmetic/rounding rules).
5. security-reviewer: review headers/CSP plan, logging redaction, rate limiting, config validation, dependency list.
6. You: integrate in order devops -> backend -> database -> qa; run from a CLEAN checkout: dotnet restore/build -warnaserror/test; docker compose up -d --build sqlserver; apply migration bundle; run the API container or `dotnet run`, call /health/ready and /api/v1/system/info with curl; run scripts/verify-reality; /wf-progress-review; write phase-04-report.md and update COMMANDS.md with exact commands that worked.
```

## Acceptance criteria
- `dotnet build -warnaserror` exits 0; `dotnet test` all pass, none skipped (show summary lines).
- Architecture tests exist and fail when you deliberately add a forbidden reference (QA proves with a temporary change; evidence).
- `docker compose up -d sqlserver` healthy; migration bundle applies to an empty DB and re-applies idempotently.
- `/health/ready` returns 200 only when the DB is reachable (stop SQL → 503; evidence).
- `docs/contracts/openapi.json` generated from running code.
- No banned packages (verify-reality clean); DEPENDENCIES.md complete.
- CI workflow file validates (`actionlint` or equivalent if available; otherwise note).

## How to verify
Run the commands in `docs/progress/COMMANDS.md` yourself on a fresh clone; stop the DB container and call `/health/ready`; add a forbidden project reference and see the architecture test fail.

## Gate to Phase 05
All acceptance items PASS with evidence; no open Critical/High security findings; PM GO; tag `phase-04-done`.
