# ADR-004: Ban Commercial-License Mediator/Mapper/Assertion Libraries in Favor of Explicit Services & Extensions

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** `backend-developer`, `qa-engineer`, `project-manager`
- **References:** `DEC-003`, `AGENTS.md` §1, `.agents/rules/backend.md`, `docs/progress/DEPENDENCIES.md`

---

## Context

In 2025, several historically ubiquitous .NET open-source libraries transitioned to commercial or restrictive dual licenses:
- **MediatR** (Lucky Penny Software commercial license starting v12.5+/v13)
- **AutoMapper** (Lucky Penny Software commercial license starting v14+/v15)
- **FluentAssertions** (Xceed commercial license starting v8.0+)
- **MassTransit** (commercial license starting v9.0+)

`AGENTS.md` §1 and `DEC-003` strictly ban commercial-license dependencies and require every NuGet/npm package to be license-verified and recorded in `docs/progress/DEPENDENCIES.md`. Furthermore, runtime reflection mappers and indirect in-process mediator pipelines obscure call stacks, complicate debugging, and encourage anemic pass-through boilerplate.

---

## Decision

We **strictly ban** `MediatR`, `AutoMapper`, `FluentAssertions` (v8+), and `MassTransit` (v9+) across the entire solution and replace them with explicit, zero-cost, compile-time-safe patterns:

1. **Application Services & Cross-Module Contracts instead of `MediatR`:**
   - Within a module, Minimal API endpoints inject plain application service classes (e.g., `OrderCommandService`, `OrderQueryService`, `CatalogService`) registered in DI (`Scoped`).
   - Across modules, callers inject explicit contract interfaces from `GroceryHub.Modules.<Target>.Contracts` (e.g., `IInventoryReservationContract`, `IPaymentService`, `IOrderPaymentPort`).
   - Navigation from an endpoint to its handler is a single `F12` (Go To Definition) in any IDE.

2. **Transactional Outbox & In-Process Event Dispatcher instead of `MediatR.INotification` / `MassTransit`:**
   - Aggregates record domain events extending `DomainEvent` (`GroceryHub.SharedKernel`).
   - When `GroceryHubDbContext.SaveChangesAsync` runs, an EF Core interceptor serializes pending domain/integration events to `shared.OutboxMessages` in the **same SQL transaction**.
   - The hosted `OutboxDispatcher` background service polls unpublished rows from `shared.OutboxMessages` and dispatches them to registered `IIntegrationEventHandler<TEvent>` implementations resolved from DI.

3. **Explicit C# Mapping Extension Methods & Projection Queries instead of `AutoMapper`:**
   - Read endpoints use strongly-typed EF Core LINQ `.Select(x => new ProductSummaryDto(...))` projections (`AsNoTracking()`), which SQL Server translates directly to optimal `SELECT` clauses without loading full entities.
   - Domain-to-DTO mappings for command responses are written as pure static extension methods (`order.ToOrderDetailDto()`), ensuring 100% compile-time type checking when properties change.

4. **xUnit Native `Assert` instead of `FluentAssertions`:**
   - All unit, integration, and architecture tests use `Xunit.Assert` (`Apache-2.0` license) plus small domain assertion helpers where helpful.

5. **Automated Enforcement:**
   - `scripts/verify-reality.ps1` / `scripts/verify-reality.sh` and Architecture Test #5 (`GroceryHub.ArchitectureTests`) scan `Directory.Packages.props`, all `*.csproj` files, and loaded assemblies, failing the build if any banned package is referenced.

---

## Alternatives Considered

1. **Pinning older MIT/Apache versions of `MediatR` (v12.4), `AutoMapper` (v13), or `FluentAssertions` (v7):**
   - *Rejected:* Locks the codebase to unmaintained legacy versions that will never receive security patches or .NET 10 updates, and violates `AGENTS.md` §1 (`DEC-003`).
2. **Third-party source-generator mappers (`Mapperly`):**
   - *Rejected as unnecessary default:* Explicit static `.ToDto()` extension methods and EF `.Select(...)` projections are simpler, require zero external dependencies, and prevent accidental hidden mapping bugs on `Money` (`long` VND) and snapshots.

---

## Consequences

- **Positive:**
  - Zero license compliance risk or commercial subscription cost (`DEC-003`).
  - Faster startup, lower memory allocation, and crystal-clear stack traces without reflection or pipeline proxies.
  - Compiler immediately flags any missing DTO property mapping during refactoring.
- **Negative / Trade-offs:**
  - Developers write explicit mapping methods (typically 5–15 lines per DTO), which is straightforward and less error-prone than debugging runtime mapping profiles.
