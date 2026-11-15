# ADR-001: Modular Monolith Architecture & Project Boundary Enforcement

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** `backend-developer`, `project-manager`
- **References:** `DEC-001`, `docs/spec/05-SYSTEM-ARCHITECTURE.md` §2–§4, `.agents/rules/backend.md`

---

## Context

GroceryHub is a Vietnamese online grocery platform requiring strong consistency across catalog pricing, stock reservation (`onHand - reserved`), coupon redemption limits, order placement, and SePay VietQR / COD payment states (`BR-01`..`BR-08`, `NFR-008`, `NFR-009`).

The system encompasses 12 distinct bounded modules (`Identity`, `Customers`, `Catalog`, `Inventory`, `Purchasing`, `Promotions`, `Cart`, `Ordering`, `Payments`, `Reviews`, `Reporting`, `Audit`), deployed to a single Linux VPS via Docker Compose (`DEC-010`).

We need an architecture that:
1. Preserves strict logical boundaries between business domains so that code does not degrade into a "Big Ball of Mud".
2. Allows atomic ACID transactions across order placement, stock reservation, coupon redemption, payment creation, and transactional outbox persistence without distributed two-phase commits (2PC) or fragile network sagas.
3. Enforces module encapsulation at compile time and via automated architecture tests in CI (`NFR-009`).

---

## Decision

We adopt a **Modular Monolith** architecture hosted in a single ASP.NET Core 10 Web API deployable (`GroceryHub.Api`):

1. **Project Structure per Module:**
   - Each module lives in `src/GroceryHub.Modules.<Name>/` (`GroceryHub.Modules.<Name>.csproj`) with internal vertical/layered folders:
     - `Domain/` (`internal` aggregates, entities, value objects, domain invariants)
     - `Application/` (`internal` application services, command/query handlers, validators)
     - `Infrastructure/` (`internal` query services, external adapters; EF Core persistence configurations live in `GroceryHub.Persistence` owned by `database-engineer`)
     - `Api/` (`internal` minimal API endpoint groups + a single `public static` module registration entry point `Add<Name>Module` and `Map<Name>Endpoints`)
   - Cross-module interfaces, DTOs, and integration events live in a separate contract assembly: `src/GroceryHub.Modules.<Name>.Contracts/` (`GroceryHub.Modules.<Name>.Contracts.csproj`).

2. **Compile-Time & Test-Time Boundary Rules:**
   - All types in `GroceryHub.Modules.<Name>` are `internal` by default (`InternalsVisibleTo` is granted only to `GroceryHub.UnitTests`, `GroceryHub.IntegrationTests`, and `GroceryHub.Persistence` where needed for EF entity mapping).
   - A module implementation project `GroceryHub.Modules.<Caller>` may reference **only**:
     - `GroceryHub.SharedKernel`
     - `GroceryHub.Persistence` (for unit-of-work / repository / `DbSet` access within its own schema)
     - `GroceryHub.Modules.<Caller>.Contracts`
     - Other modules' **Contracts** projects (`GroceryHub.Modules.<Target>.Contracts`)
   - A module implementation project **must never** reference another module's implementation project (`GroceryHub.Modules.<Other>`).
   - Contracts projects (`GroceryHub.Modules.*.Contracts`) may reference **only** `GroceryHub.SharedKernel` (zero dependencies on other Contracts projects, EF Core, or ASP.NET Core).

3. **Cycle-Free Bidirectional Communication (`Ordering <-> Payments`):**
   - `GroceryHub.Modules.Ordering` references `GroceryHub.Modules.Payments.Contracts` (`IPaymentService`) to initialize payment records and QR attempts during checkout.
   - `GroceryHub.Modules.Payments` references `GroceryHub.Modules.Ordering.Contracts` (`IOrderPaymentPort`) to confirm orders upon verified SePay webhooks, cancel expired QR orders, and inspect order state for COD/refunds.
   - Neither implementation project references the other, guaranteeing an acyclic project dependency graph (DAG).

4. **Automated Architecture Tests (`GroceryHub.ArchitectureTests`):**
   - Enforced on every build from Phase 04 onward:
     1. Modules do not reference other module implementation assemblies.
     2. `Api` layer types do not return or expose EF Core/Domain entity types directly.
     3. Every Minimal API endpoint explicitly declares `.RequireAuthorization("<policy>")` or `.AllowAnonymous()`.
     4. No direct usage of `DateTime.Now`, `DateTime.UtcNow`, or `Thread.Sleep` in `src/`.
     5. Zero references to banned commercial-license packages (`MediatR`, `AutoMapper`, `FluentAssertions`, `MassTransit`).

---

## Alternatives Considered

1. **Microservices (separate deployables & databases per domain):**
   - *Rejected:* Introduces distributed transaction hazards between `Ordering`, `Inventory`, `Promotions`, and `Payments`, network latency, operational overhead on a single VPS (`DEC-010`), and complex eventual consistency for stock reservation at checkout.
2. **Single-Project Layered Monolith ("Clean Architecture" with 4 horizontal projects: Domain, Application, Infrastructure, Web):**
   - *Rejected:* Horizontal layers group unrelated domains together (e.g., `Catalog` and `Payments` in the same `Domain` project), allowing accidental cross-domain coupling and entity navigation properties across boundaries.
3. **Single Module Project without separate `.Contracts` assemblies:**
   - *Rejected:* If `Contracts` were just a folder inside `GroceryHub.Modules.<Name>.csproj`, referencing another module's contract would require a project reference to its implementation assembly, making bidirectional interactions (`Ordering <-> Payments`) a circular MSBuild reference error.

---

## Consequences

- **Positive:**
  - Single deployment unit (`GroceryHub.Api` container) with low memory footprint and zero inter-service network hops.
  - ACID database transactions across module ports when required (e.g., `PlaceOrder` reserving stock, redeeming a coupon, creating a payment attempt, and writing `OutboxMessages` atomically).
  - Clear module ownership and testability; any module can be extracted to a standalone service in the future because all cross-module calls already pass through explicit asynchronous `Contracts` interfaces and DTOs.
- **Negative / Trade-offs:**
  - Solution contains 12 module projects + 12 contract projects + `SharedKernel` + `Persistence` + `Api` + 3 test projects, slightly increasing initial `.sln` scaffolding size (mitigated by `Directory.Build.props` and `Directory.Packages.props`).
