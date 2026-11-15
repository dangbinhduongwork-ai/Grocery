# GroceryHub — Approved Dependencies Registry

Every NuGet and npm dependency added to GroceryHub must be verified for permissive open-source licensing and active maintenance before being committed (`AGENTS.md` §1, `DEC-003`).

> [!CAUTION]
> **Strictly Banned Commercial-License Libraries (`DEC-003`, `ADR-004`):**
> `MediatR`, `AutoMapper`, `FluentAssertions` (v8+), `MassTransit` (v9+), `IdentityServer` / `Duende`.

## 1. Backend NuGet Packages (`Directory.Packages.props` — Added in Phase 04)

| Package Name | Version | License | SPDX | Owner / Maintainer | Purpose in GroceryHub | Verified Date |
|---|---|---|---|---|---|---|
| `Microsoft.AspNetCore.OpenApi` | `10.0.15` | MIT | `MIT` | Microsoft | Built-in ASP.NET Core 10 OpenAPI 3.1 document generation (`ADR-005`) | 2026-11-15 |
| `Microsoft.EntityFrameworkCore` | `10.0.15` | MIT | `MIT` | Microsoft | Core ORM for `GroceryHubDbContext` (`ADR-002`) | 2026-11-15 |
| `Microsoft.EntityFrameworkCore.Relational` | `10.0.15` | MIT | `MIT` | Microsoft | Relational mapping, schemas, sequences, and migrations | 2026-11-15 |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.15` | MIT | `MIT` | Microsoft | SQL Server provider for EF Core 10 | 2026-11-15 |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.15` | MIT | `MIT` | Microsoft | Design-time migration scaffolding & `dotnet ef migrations bundle` | 2026-11-15 |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | `10.0.15` | MIT | `MIT` | Microsoft | Database readiness check on `/health/ready` (`NFR-004`) | 2026-11-15 |
| `FluentValidation` | `11.11.0` | Apache 2.0 | `Apache-2.0` | Jeremy Skinner | Request DTO validation rules in module Application/Api layers | 2026-11-15 |
| `FluentValidation.DependencyInjectionExtensions` | `11.11.0` | Apache 2.0 | `Apache-2.0` | Jeremy Skinner | DI registration of `IValidator<T>` across module assemblies | 2026-11-15 |
| `Serilog.AspNetCore` | `9.0.0` | Apache 2.0 | `Apache-2.0` | Serilog Contributors | Structured request and application logging (`NFR-005`) | 2026-11-15 |
| `Serilog.Formatting.Compact` | `3.0.0` | Apache 2.0 | `Apache-2.0` | Serilog Contributors | Compact JSON log formatting for container stdout | 2026-11-15 |
| `Microsoft.NET.Test.Sdk` | `17.12.0` | MIT | `MIT` | Microsoft | Test host SDK for Unit, Architecture, and Integration test projects | 2026-11-15 |
| `xunit` | `2.9.3` | Apache 2.0 | `Apache-2.0` | xUnit.net / .NET Foundation | Test framework and assertions (`Assert.*` replacing `FluentAssertions`) | 2026-11-15 |
| `xunit.runner.visualstudio` | `3.0.2` | Apache 2.0 | `Apache-2.0` | xUnit.net / .NET Foundation | Visual Studio / `dotnet test` runner adapter | 2026-11-15 |
| `coverlet.collector` | `6.0.4` | MIT | `MIT` | Coverlet Contributors | Code coverage collector for `dotnet test` | 2026-11-15 |
| `NetArchTest.Rules` | `1.3.2` | MIT | `MIT` | Ben Morris | Architecture boundary & banned-dependency tests (`GroceryHub.ArchitectureTests`) | 2026-11-15 |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.15` | MIT | `MIT` | Microsoft | `WebApplicationFactory<Program>` in-memory HTTP server for integration tests | 2026-11-15 |
| `Microsoft.Extensions.TimeProvider.Testing` | `9.0.0` | MIT | `MIT` | Microsoft | Deterministic `FakeTimeProvider` for unit and integration tests | 2026-11-15 |
| `Testcontainers.MsSql` | `4.16.0` | MIT | `MIT` | Testcontainers | Disposable SQL Server container management in `GroceryHub.IntegrationTests` | 2026-11-15 |
| `Respawn` | `6.2.1` | MIT | `MIT` | Jimmy Bogard | Fast database table reset between integration tests (`MIT` licensed) | 2026-11-15 |
| `Microsoft.OpenApi` | `3.11.0` | MIT | `MIT` | Microsoft | Secure OpenAPI object model resolving GHSA-v5pm-xwqc-g5wc | 2026-11-15 |
| `System.Security.Cryptography.Pkcs` | `10.0.15` | MIT | `MIT` | Microsoft | Secure PKCS cryptography resolving GHSA-846f-f764-76mx | 2026-11-15 |

## 2. Local CLI Tools (`.config/dotnet-tools.json` — Added in Phase 04)

| Tool Package | Version | License | Command | Purpose |
|---|---|---|---|---|
| `dotnet-ef` | `10.0.0` | `MIT` | `dotnet ef` | EF Core 10 migrations (`migrations add`, `migrations script`, `migrations bundle`) |
