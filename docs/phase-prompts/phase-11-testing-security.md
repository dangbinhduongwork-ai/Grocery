# Phase 11 — Testing and Security hardening

**Goal:** Close all gaps: complete test coverage by requirement, performance/accessibility budgets, a full security review with fixes verified.
**Agents:** qa-engineer ‖ security-reviewer → owners fix → qa/security retest; devops (CI gates); PM.
**Inputs:** all previous phases; docs 12, security skill; `TRACEABILITY.md`.
**Outputs:** complete test suites, `docs/qa/test-plan.md` (final), `docs/qa/coverage-report.md`, `docs/security/review-phase-11.md`, `docs/security/rbac-test-final.md`, performance reports, fixed defects.
**Dependencies:** Phase 10 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 11 — TESTING & SECURITY. Load skills testing-qa, security-review. Rule: no feature work; only tests, fixes, hardening.

1. QA gap analysis: from docs/progress/TRACEABILITY.md list every FR/NFR/BR without a passing automated test; write the gap table in docs/qa/test-plan.md. Implement missing tests (qa-engineer) — integration/E2E/a11y/perf per doc 12 §3-§4 (all journeys J1..J7 must exist and pass, none skipped). Run the whole suite 3 times, once in random order, to expose flakiness; fix root causes (no sleeps/retries).
2. Mutation sanity: in a scratch branch break 8 critical rules one at a time (stock check, coupon limit, order transition guard, ownership check, price recompute, webhook signature, webhook amount check, refund two-person rule) and prove at least one test fails for each; save results. Revert.
3. Performance: seed 20,000 products; run k6 (or equivalent) for catalog search, product detail, cart add, checkout at 50 virtual users; record p95/p99 and error rate; fix hotspots (database-engineer owns index/query fixes); run Lighthouse mobile on Home/Category/Product; admin bundle analysis.
4. Accessibility: axe on all key pages (storefront + admin) and manual keyboard walkthrough of checkout and payment; fix findings via owners.
5. Security: security-reviewer runs /wf-security-review in full (RBAC matrix for ALL endpoints, payments negatives, uploads, auth hardening, headers/CSP in the compose stack, dependency/secret/image scans); produce docs/security/review-phase-11.md with severity table; owners fix via CR; security-reviewer retests each fix from a fresh build.
6. Data/ops checks (database-engineer): migrations apply on empty DB and from Phase-04 baseline step by step; seeds idempotent; backup/restore drill executed (script + output) against a populated DB.
7. Update TRACEABILITY.md: only QA/PM may set Verified, with evidence links. Run /wf-progress-review; write phase-11-report.md.
```

## Acceptance criteria
- Every Must (M) requirement is `Verified` in TRACEABILITY.md with test + evidence; Should items are Verified or explicitly deferred with user approval.
- Suites pass three consecutive times including one random-order run; zero skipped tests.
- Mutation sanity table shows 8/8 detections.
- Performance budgets (doc 12 §7) met or exceptions approved by the user in DECISIONS.md.
- axe: 0 serious/critical; keyboard walkthrough notes included.
- No open Critical/High security findings; Medium findings have owners and dates or documented risk acceptance by the user.
- Restore drill succeeded with output saved.

## How to verify
Run `dotnet test` and the Playwright suite yourself; open the mutation table and re-break one rule; read the security report's retest sections.

## Gate to Phase 12
All PASS; PM GO; tag `phase-11-done`.
