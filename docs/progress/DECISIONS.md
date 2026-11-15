# GroceryHub — Architectural & Project Decisions Log

All project, orchestration, business, and technical decisions approved by the user or recorded at phase gates live here.

| ID | Date | Phase | Topic | Decision | Source / Evidence |
|---|---|---|---|---|---|
| DEC-001 | 2026-11-15 | 00 | Fixed Tech Stack | Backend: C# / .NET 10 LTS, ASP.NET Core 10 Minimal APIs, EF Core 10, SQL Server (modular monolith, single `GroceryHubDbContext`, schema-per-module). Frontend: React 19 + TypeScript strict + Vite, Tailwind CSS v4, shadcn/ui, TanStack Router/Query/Table. Payments: COD + SePay VietQR & Webhook. Infra: Docker + Docker Compose, GitHub Actions. | `AGENTS.md`, `docs/spec/03-TECH-STACK-RESEARCH.md` |
| DEC-002 | 2026-11-15 | 00 | Branching & Integration Strategy | Role agents work on `agent/<role>/<phase>-<slug>`, merged by `project-manager` in dependency order into `integration/phase-XX`, verified with full build/tests/`verify-reality`, and merged to `main` with tag `phase-XX-done` only after a `GO` gate decision. | `AGENTS.md`, `docs/spec/09-AGENT-ORCHESTRATION.md` §6 |
| DEC-003 | 2026-11-15 | 00 | Commercial-License Ban | Libraries that switched to commercial licenses (`MediatR`, `AutoMapper`, `FluentAssertions` 8+, `MassTransit` commercial v9+) are strictly banned. Every added NuGet/npm package must be checked and recorded in `docs/progress/DEPENDENCIES.md`. | `AGENTS.md` §1, `scripts/verify-reality.*` |
