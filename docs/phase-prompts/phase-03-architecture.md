# Phase 03 — Architecture and Database Design (implementation-ready specs)

**Goal:** Turn docs 05–07 and 11 into precise, implementable contracts so developers do not improvise: module contracts, events, permissions seed, ERD v1.0, final API, ADRs.
**Agents:** backend-developer (architecture lead), database-engineer, payment-specialist, security-reviewer (review), project-manager.
**Inputs:** `docs/spec/02,05,06,07,08,11`, decisions from Phase 01.
**Outputs:** `docs/spec/adr/ADR-*.md`, `docs/spec/module-contracts.md`, `docs/spec/events.md`, `docs/spec/permissions-seed.md`, updated `06` and `07`, `docs/spec/state-machines.md`, `docs/progress/phase-03-report.md`. **Documents only; no production code.**
**Dependencies:** Phase 02 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 03 — ARCHITECTURE & DATABASE DESIGN. Documents only; do not create the solution or any source code.

Delegate (parallel where independent; owners write only their files):
1. backend-developer -> docs/spec/module-contracts.md (for each module: public interfaces in Contracts with method signatures, DTOs, error codes; which module calls which; resolve the Ordering<->Payments port design), docs/spec/events.md (domain events, payload, publisher, handlers, idempotency), docs/spec/state-machines.md (Order, Payment, PurchaseOrder, Refund, Review: states, transitions, guards, side effects, error codes), ADRs: ADR-001 modular monolith, ADR-002 single DbContext + schemas, ADR-003 auth/tokens/cookies, ADR-004 no mediator/mapper libs (licences), ADR-005 contract-first OpenAPI, ADR-006 error model, ADR-007 time/money/id conventions. Each ADR: context, decision, alternatives, consequences.
2. database-engineer -> update docs/spec/06-DATABASE-ERD.md to v1.0: every column with type, nullability, default, constraint, index, with the final enum values; add the sequence/seed lists; add a "queries to be optimised" list (product search, order list, stock reserve, dashboard) with the index strategy and the plan to measure them; define the Vietnamese search approach and test cases.
3. payment-specialist -> verify docs/spec/11 against 11-SEPAY-DIFFS.md; finalize payments tables/state machine/jobs; add sequence diagrams (QR happy path, duplicate, late payment, reconciliation, refund) in Mermaid.
   Also backend-developer finalizes docs/spec/07-API-SPECIFICATION.md v1.0 (paths, schemas, examples, error codes; availablePaymentMethods in the checkout quote) and updates docs/spec/05 if the contracts require it.
4. You (PM) -> docs/spec/permissions-seed.md (roles x permissions final matrix from spec 07 with rationale) and a requirements-to-module map (FR/NFR/BR -> module -> phase) in docs/progress/TRACEABILITY.md (status NotStarted).
5. security-reviewer reviews all of the above against docs/security/threat-model.md; open CR notes for issues.
6. Consistency check (you, using searches): every endpoint in 07 maps to a module contract and a permission; every table in 06 is owned by one module; every state transition in state-machines.md has an actor/permission; no document contradicts another. List and fix contradictions.
7. /wf-progress-review -> docs/progress/phase-03-report.md. Vietnamese summary for the user including decisions needing confirmation.
```

## Acceptance criteria
- Seven ADRs exist with alternatives and consequences.
- Module contracts cover all 12 modules; no cycles (verified by a dependency table).
- ERD v1.0 lists every column/constraint; every uniqueness rule from specs is present.
- Each API endpoint has permission + owning module; each transition has actor + error code.
- Traceability file maps every FR/NFR/BR to a module and phase.
- Security review has no open Critical/High design findings.

## How to verify
Pick 5 random endpoints and trace endpoint → contract → table → permission; pick 3 requirements and trace to a phase; check no source files were added (`git diff --stat` shows only docs).

## Gate to Phase 04
Specs v1.0 frozen (changes via CR only); PM GO.
