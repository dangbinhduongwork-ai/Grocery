# Phase 03 Plan — Architecture and Database Design (Implementation-Ready Specs)

- **Date:** 2026-11-15
- **Phase:** `03`
- **Lead / Integration Owner:** `project-manager`
- **Role Agents:** `backend-developer`, `database-engineer`, `payment-specialist`, `security-reviewer`
- **Constraint:** **Documents only; no production code or `.sln`/`.csproj` files.**

---

## 1. Task Breakdown & File Ownership

| Task ID | Owner | Branch Name | Owned Paths | Execution Order | Deliverable & Definition of Done |
|---|---|---|---|---|---|
| **T03-01** | `project-manager` | `integration/phase-03` | `docs/progress/phase-03-plan.md`, `docs/spec/permissions-seed.md`, `docs/progress/TRACEABILITY.md` | Parallel (Batch 1) | Final `Roles x Permissions` seed matrix (`permissions-seed.md`) with rationale & `DEC-011` rules; complete `FR/NFR/BR -> Owning Module -> Phase` map in `TRACEABILITY.md`. |
| **T03-02** | `backend-developer` | `agent/backend-developer/phase-03-arch-contracts` | `docs/spec/module-contracts.md`, `docs/spec/events.md`, `docs/spec/state-machines.md`, `docs/spec/adr/ADR-001..007-*.md`, `docs/spec/05-SYSTEM-ARCHITECTURE.md`, `docs/spec/07-API-SPECIFICATION.md` | Parallel (Batch 1) | 12-module contracts with C# signatures/DTOs/error codes & acyclic dependency matrix (`IOrderPaymentPort` / `IPaymentService` port design); domain events + outbox/inbox idempotency; 5 state machines (`Order`, `Payment`, `PurchaseOrder`, `Refund`, `Review`) with actors/permissions/guards/side-effects/error codes; 7 ADRs (`ADR-001`..`ADR-007`); `07-API-SPECIFICATION.md` v1.0 (every endpoint has owning module + permission + request/response schema + error codes + `availablePaymentMethods`). |
| **T03-03** | `database-engineer` | `agent/database-engineer/phase-03-erd-v1` | `docs/spec/06-DATABASE-ERD.md` | Parallel (Batch 1) | `06-DATABASE-ERD.md` v1.0: every table mapped to its module schema, every column with SQL type/nullability/default/constraint/index/final enum values; sequence & seed lists; "Queries to be Optimised" (product search, order list, stock reserve, dashboard) with index strategy & measurement plan; Vietnamese search strategy (`Latin1_General_100_CI_AI_SC_UTF8` + `SearchText` normalized column) and test cases. |
| **T03-04** | `payment-specialist` | `agent/payment-specialist/phase-03-sepay-v1` | `docs/spec/11-SEPAY-INTEGRATION.md` | Parallel (Batch 1) | `11-SEPAY-INTEGRATION.md` v1.0 incorporating all findings from `11-SEPAY-DIFFS.md` (QR params `acc/bank/amount/des/template`, 7 retries / 8 total deliveries over ~33m, `/userapi/transactions/list|details|count` + sandbox `userapi-sandbox.sepay.vn/v2`, 10-char suffix configuration for `GH`), finalized payment tables/state machine/jobs, and 5 Mermaid sequence diagrams (`QR happy path`, `duplicate webhook`, `late payment`, `reconciliation`, `refund`). |
| **T03-05** | `security-reviewer` | `agent/security-reviewer/phase-03-design-review` | `docs/security/review-phase-03.md`, `docs/agent-notes/CR-*.md` (if needed) | Sequential (Batch 2 — after T03-01..04 merged) | Audit all Phase 03 specs (`module-contracts.md`, `events.md`, `state-machines.md`, `ADR-001..007`, `05`, `06`, `07`, `11`, `permissions-seed.md`) against `docs/security/threat-model.md` (`T-ID-*`, `T-ORD-*`, `T-PAY-*`, `T-UPL-*`, `T-ADM-*`); verify zero open Critical/High design findings. |
| **T03-06** | `project-manager` | `integration/phase-03` → `main` | `docs/evidence/phase-03/*`, `docs/progress/phase-03-report.md`, `docs/progress/PROGRESS.md` | Sequential (Batch 3 — Gate) | Run cross-document consistency checks, trace 5 random endpoints (`endpoint -> contract -> table -> permission`) and 3 requirements (`FR/BR -> module -> phase`), verify `git diff --stat` has only `docs/**`, run `scripts/verify-reality.ps1`, write `phase-03-report.md`, merge to `main`, tag `phase-03-done`, push to `origin`. |
