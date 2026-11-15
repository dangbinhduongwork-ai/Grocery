---
trigger: glob
globs: "src/**/*.cs, tests/**/*.cs, **/*.csproj, Directory.Build.props, Directory.Packages.props"
description: "Backend (C#/.NET 10/ASP.NET Core) coding rules for GroceryHub modular monolith."
---

# Backend rules

## Structure
- One class-library project per module: `src/GroceryHub.Modules.<Name>`. Folders: `Domain/`, `Application/`, `Infrastructure/`, `Api/` (endpoint classes), `Contracts/` (public DTOs/events other modules may reference).
- A module may reference only `SharedKernel` and OTHER modules' `Contracts`. Never another module's Domain/Infrastructure. Enforce with architecture tests (NetArchTest or equivalent license-checked library).
- `GroceryHub.Api` is the composition root (Program.cs, middleware, DI wiring, OpenAPI). No business logic there.
- Make module internals `internal`; expose only Contracts and a `Add<Module>Module(IServiceCollection)` + `Map<Module>Endpoints(...)` pair.

## Code
- Nullable reference types on, `TreatWarningsAsErrors` on, central package versions (`Directory.Packages.props`), `.editorconfig` enforced.
- No mediator/mapper libraries with commercial licenses. Use plain application services/handlers resolved via DI; map manually or with a source-generator mapper after license check.
- Endpoints: thin. Parse request -> validate (FluentValidation) -> call application service -> map to response. Return RFC 7807 `ProblemDetails` for errors. Use `TypedResults`.
- All I/O is async with `CancellationToken` propagated. No `.Result`/`.Wait()`.
- Money: `long` VND via a `Money` value object; no `decimal`/`double` for prices. Time: `DateTimeOffset` UTC via injected `TimeProvider`. Never `DateTime.Now/UtcNow` directly.
- Ids: `Guid` (v7 when available) for entities exposed to clients; human-facing codes (order number, payment code) are separate unique columns.
- Domain rules live in the domain model (aggregates enforce invariants: order state machine, stock reservation rules). Application services orchestrate; they do not duplicate invariants.
- Cross-module communication: synchronous calls through contract interfaces for queries; domain events + transactional outbox for side effects that must not fail the main transaction.
- Idempotency: write endpoints that create orders/payments accept `Idempotency-Key` and persist it with a unique index.
- Logging: Serilog structured logs, never log secrets, tokens, full webhook bodies in Information level (store raw payloads in the DB table only), no PII beyond ids.
- Configuration via `IOptions<T>` with `ValidateOnStart`. No secrets in appsettings.

## Common mistakes to avoid
- Returning EF entities from endpoints. Always return DTOs.
- Lazy loading, `Include` chains across module boundaries, N+1 queries in list endpoints (use projection `Select`).
- Catch-all `catch (Exception)` that swallows errors. Let the global exception handler produce ProblemDetails.
- Business decisions in controllers/endpoints or in EF configurations.
