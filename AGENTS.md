# GroceryHub — Project Charter (always-on)

GroceryHub is a real online grocery store (Vietnam market, VND, Vietnamese UI). You are working inside this repository under the rules below. These rules are cumulative with `.agents/rules/*.md`. Detailed specs live in `docs/spec/`.

## 1. Fixed technology (do NOT change or add alternatives without a written Change Request approved by the user)
- Backend: C# on .NET 10 LTS, ASP.NET Core Web API (minimal APIs + endpoint classes), EF Core 10, **modular monolith**.
- Database: SQL Server (EF Core migrations, schema-per-module, single DbContext).
- Auth: ASP.NET Core Identity (user store + password hashing) + JWT access token + rotating refresh token + permission-based RBAC.
- Frontend: React 19 + TypeScript (strict) + Vite, Tailwind CSS v4, shadcn/ui, TanStack Router + TanStack Query.
- Payments: COD and SePay VietQR + webhook (see `docs/spec/11-SEPAY-INTEGRATION.md`).
- Infra: Docker + Docker Compose. Source control: Git + GitHub.
- Roles: Guest (unauthenticated), Customer, Staff, Manager, Admin.
- Before adding ANY NuGet/npm package: check its license and maintenance status. Several popular .NET libraries changed to commercial licenses in 2025 (e.g. MediatR, AutoMapper, FluentAssertions 8+). Do not use them. Record every added dependency in `docs/progress/DEPENDENCIES.md` (name, version, license, reason).

## 2. Truthfulness rules (most important)
1. Never claim something is done, working or passing unless you ran the real command in this session and saw the real output. Paste the command and key output lines into the phase report under `docs/evidence/phase-XX/`.
2. Never fabricate data, test results, screenshots, API responses or file contents. Never write placeholder code that pretends to work (hard-coded responses, empty handlers returning success, `NotImplementedException` left in shipped paths, `Thread.Sleep` to "simulate" work).
3. Never weaken, skip, delete or mock-away a test to make it pass. Mock only true external boundaries (SePay HTTP, SMTP, clock). The system under test (handlers, EF Core queries, authorization) must run for real; integration tests use a real SQL Server container.
4. Do not assume earlier phases are complete. At the start of every phase run the audit steps in the phase prompt and report the real state.
5. If a requirement is ambiguous or a task is impossible, stop and write a question in `docs/progress/QUESTIONS.md` instead of guessing silently.
6. If you are unsure a library/API/Antigravity feature exists, verify in official docs or by running it. Do not invent tool names, CLI flags, config keys or file formats.

## 3. Ownership and collaboration
- One owner per file/directory (see `docs/spec/09-AGENT-ORCHESTRATION.md` ownership map). Never edit a file you do not own; open a Change Request note `docs/agent-notes/CR-<nnn>-<slug>.md` addressed to the owner instead.
- The OpenAPI document `docs/contracts/openapi.json` is the contract between backend and frontend. Backend regenerates it; frontend generates TS types from it. No hand-written duplicate DTOs on the frontend.
- Work on a branch `agent/<role>/<phase>-<slug>`. Commit small, with Conventional Commits (`feat(orders): ...`). Never push to `main` directly; the Project Manager integrates.
- Secrets never enter the repo. Use `.env` (git-ignored) + `.env.example` with placeholders.

## 4. Definition of Done (applies to every task)
Code compiles with zero warnings treated as errors where configured; tests for the change exist and pass (real run); lint/format pass; acceptance criteria in the phase prompt are each checked with evidence; docs/spec updated if behavior changed; no new TODO without an issue entry in `docs/progress/QUESTIONS.md`.

## 5. Where to look
- Requirements (IDs `FR-*`, `NFR-*`): `docs/spec/02-PROJECT-REQUIREMENTS.md`
- Architecture, ERD, API: `docs/spec/05`, `06`, `07`
- Design system: `docs/spec/08-UI-DESIGN-SYSTEM.md`
- Orchestration, ownership, phases: `docs/spec/09-AGENT-ORCHESTRATION.md`
- Skills: `.agents/skills/*/SKILL.md` (load the matching skill before working). Roles: `.agents/agents/*.md`.
- Progress: `docs/progress/PROGRESS.md`, traceability: `docs/progress/TRACEABILITY.md`.

## 6. Language
Code, identifiers, commit messages, API fields: English. User-facing UI text: Vietnamese (all strings in `web/src/shared/i18n/vi.ts`). Money is VND integers (`long`), never floating point.
