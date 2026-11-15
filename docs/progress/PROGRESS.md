# GroceryHub — Master Progress Tracker

## 1. Branching & Integration Strategy Note
- **Baseline branch:** `main` (protected integration target; only `project-manager` integrates into `main` after a phase gate is `GO`).
- **Role agent feature branches:** `agent/<role>/<phase>-<slug>` (e.g., `agent/backend-developer/04-solution-skeleton`, `agent/database-engineer/04-dbcontext-baseline`).
- **Phase integration branches:** `integration/phase-XX` — `project-manager` merges agent branches in dependency order into `integration/phase-XX`, runs the full build, test suite, and `scripts/verify-reality.ps1`, and tags `phase-XX-done` upon `GO`.

## 2. Phase Roadmap & Gate Status

| Phase | Title | Status | Started | Gate Decision | GO Date | Plan | Report & Evidence |
|---|---|---|---|---|---|---|---|
| **00** | Audit repository and environment | **Done** | 2026-11-15 | **GO** | 2026-11-15 | `docs/progress/phase-00-plan.md` | `docs/progress/phase-00-report.md` (`docs/evidence/phase-00/`) |
| **01** | Research and Requirements | NotStarted | — | Pending | — | — | — |
| **02** | Agents, Skills, Rules (install & prove) | NotStarted | — | Pending | — | — | — |
| **03** | Architecture and Database Design | NotStarted | — | Pending | — | — | — |
| **04** | Backend Foundation | NotStarted | — | Pending | — | — | — |
| **05** | Authentication and RBAC | NotStarted | — | Pending | — | — | — |
| **06** | Products, Categories and Inventory | NotStarted | — | Pending | — | — | — |
| **07** | Cart, Checkout and Order Lifecycle | NotStarted | — | Pending | — | — | — |
| **08** | Customer Frontend (Storefront) | NotStarted | — | Pending | — | — | — |
| **09** | Admin, Manager and Staff Dashboard | NotStarted | — | Pending | — | — | — |
| **10** | SePay VietQR + Webhook | NotStarted | — | Pending | — | — | — |
| **11** | Testing and Security Hardening | NotStarted | — | Pending | — | — | — |
| **12** | Docker and Deployment | NotStarted | — | Pending | — | — | — |
| **13** | Final Review and Handover | NotStarted | — | Pending | — | — | — |
