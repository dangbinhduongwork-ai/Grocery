---
name: efcore-sqlserver
description: Designs and reviews EF Core 10 + SQL Server persistence for GroceryHub - DbContext with schema-per-module, entity configurations, migrations, indexes, constraints, concurrency control for stock/orders/payments, query performance and seeding. Use for any database schema, migration, EF query, stock/inventory atomicity or performance question.
---

# efcore-sqlserver

## Goal
A correct, race-safe, observable data layer that matches `docs/spec/06-DATABASE-ERD.md` and `.agents/rules/database.md`.

## Process
1. Read the ERD section for the module. Confirm table names, schemas, keys, constraints.
2. Write/modify `IEntityTypeConfiguration<T>` in the module's `Infrastructure/Persistence`. Set max lengths, `nvarchar`, `bigint` money, indexes, unique constraints, check constraints, `IsRowVersion()` where needed.
3. `dotnet ef migrations add <YYYYMMDD_Module_Change> --project src/GroceryHub.Persistence --startup-project src/GroceryHub.Api`. Inspect the generated SQL (`dotnet ef migrations script`). Never hand-edit a merged migration.
4. Apply to a clean SQL Server container; run integration tests.
5. For each list/search query: write it with projection, run with logging of SQL, check the execution plan (SSMS/`SET STATISTICS IO ON` in the container), add or adjust indexes and record the evidence.

## Standards
- Concurrency: stock reservation uses a single conditional `ExecuteUpdateAsync` (`WHERE OnHand - Reserved >= @qty`) and checks affected rows; aggregates with `rowversion` retry on `DbUpdateConcurrencyException` (bounded retries).
- Transactions: one `SaveChanges` per use-case where possible; explicit transaction only for multi-statement atomicity (stock + order + payment).
- Inbox/outbox tables for webhook events and domain events (`payments.WebhookEvents`, `shared.OutboxMessages`).
- Search: normalized accent-less column `SearchText` (lower, no diacritics) + index/full-text; decide by measurement on >= 20k seeded dev products.
- Migrations in production: `dotnet ef migrations bundle` run by a one-off container; backward-compatible (expand/contract) changes only.
- Dev sample data in a separate seeder gated by `IHostEnvironment.IsDevelopment()`.

## Best practices
- `AsNoTracking` for reads; compiled queries only after measuring.
- `AsSplitQuery` when including collections; avoid cartesian explosions.
- Use `HasConversion` for strongly-typed ids/enum-as-string for readable data.
- Keep `DbContext` scoped; never cache entities.

## Common mistakes
- Read-modify-write of stock in application code (lost updates).
- Missing unique index backing a "must be unique" business rule.
- `Contains` on huge lists; unbounded `ToList()`.
- Forgetting `nvarchar` (Vietnamese text becomes `?`).
- Editing an applied migration.
- Using `Database.Migrate()` at Production startup.

## Checklist
- [ ] Migration applies on empty DB and on previous version
- [ ] Unique/check constraints for each business invariant
- [ ] Concurrency test for stock/orders passes
- [ ] Query plan evidence for search and order list
- [ ] Reference seeders idempotent; dev seeder gated

## Official documentation
- https://learn.microsoft.com/ef/core/
- https://learn.microsoft.com/ef/core/providers/sql-server/
- https://learn.microsoft.com/ef/core/saving/concurrency
- https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying
- https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete
