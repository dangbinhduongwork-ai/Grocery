# 12 — Testing Strategy

## 1. Goals
Prove each requirement (FR/NFR/BR) through real execution; make fake implementations impossible to pass; keep feedback fast.

## 2. Levels and tools
| Level | Tooling | Scope | Owner |
|---|---|---|---|
| Unit (BE) | xUnit, Shouldly/built-in asserts | Domain rules, state machines, calculators (totals, coupons, shipping, moving-average cost), validators | code owner |
| Architecture | NetArchTest/ArchUnitNET (licence-checked) | module boundaries, default-deny endpoints, banned APIs/packages | QA |
| Integration (BE) | `WebApplicationFactory` + Testcontainers SQL Server, real migrations, real JWT | endpoints end-to-end through DB, authz matrix, concurrency, idempotency, payments webhook | QA |
| Contract | compare generated OpenAPI with `docs/contracts/openapi.json`; TS client generation must succeed | contract drift | QA/frontend |
| Unit/component (FE) | Vitest, Testing Library, MSW (unit only) | hooks, forms, formatting, guards | frontend |
| E2E | Playwright on the compose stack | critical journeys (§4) | QA |
| Accessibility | axe-core via Playwright | key pages | QA |
| Performance | k6 (or equivalent; licence-checked), Lighthouse CI | API p95, web vitals | QA/devops |
| Security | `wf-security-review`, scans | see doc 10/skills | security-reviewer |

## 3. Mandatory test areas (cannot be skipped at any gate)
1. **Authorization matrix**: every endpoint × (anon, Customer A, Customer B, Staff, Manager, Admin) with expected status; IDOR cases.
2. **Concurrency**: last unit of stock (N parallel checkouts → exactly 1 succeeds, others `INSUFFICIENT_STOCK`); coupon usage limit; duplicate `Idempotency-Key`; double webhook.
3. **State machine**: every allowed and disallowed order transition; side effects (stock, coupon, payment, refund).
4. **Money math**: totals, discount allocation across lines, rounding (integers), shipping thresholds, moving-average cost, profit report vs hand-computed fixtures.
5. **Payments**: matrix T-PAY-01…24 (doc 11).
6. **Search**: Vietnamese accent-insensitive search ("sua tuoi" finds "Sữa tươi"), filters, sorting, paging.
7. **Address**: 2-level data loaded (34 provinces; ward counts consistent), validation, max 10 addresses, default uniqueness.
8. **Security regression**: lockout, refresh reuse detection, upload validation.
9. **Migrations**: apply to empty DB; apply bundle on previous schema version; seeds idempotent.

## 4. E2E journeys (Playwright, no mocks of the backend)
J1 Guest browse → search → filter → product → add to cart → register → verify email (Mailpit API) → login (cart merged) → add address → checkout COD → order appears → staff confirms → … Delivered → customer reviews.
J2 Checkout with QR → signed webhook posted by test harness using the real HMAC → page updates to paid without reload → order Confirmed.
J3 QR expiry → order cancelled → stock restored.
J4 Admin creates product with images and price → appears on storefront → stock via goods receipt → customer buys → inventory decremented at Shipping.
J5 Manager reviews anomaly (underpaid) → resolves; refund request approved by different user.
J6 RBAC UI: Staff cannot see cost/profit or settings; direct URL access denied.
J7 Mobile viewport (360 px) of J1 critical steps.

## 5. Test data
- Integration/E2E data created through public APIs or dedicated test builders that call the real application services — never by inserting rows that bypass invariants.
- `DevSampleDataSeeder` (Development only) provides ~200 realistic grocery products across categories for manual QA, plus a perf seed of 20,000 products (separate command) for search benchmarking.

## 6. Quality gates
| Gate | Requirement |
|---|---|
| PR | build `-warnaserror`, unit + architecture tests, lint, typecheck, FE unit tests, `verify-reality` |
| Phase | clean-checkout run of all suites + the phase's acceptance tests + security review (when listed) |
| Release | full E2E suite, a11y, perf budgets, vulnerable-package scan clean, restore drill |

## 7. Performance and UX budgets
API (50 concurrent users, dev hardware, seeded 20k products): catalog reads p95 < 300 ms; checkout p95 < 600 ms; no N+1 (assert query counts in tests for list endpoints). Web (mobile Lighthouse): Performance ≥ 85, LCP < 2.5 s, CLS < 0.1, TBT < 200 ms on home/category/product; admin initial bundle not loaded on storefront.

## 8. Evidence format
For each run, save under `docs/evidence/phase-XX/`: command line, date, git commit hash, exit code, and the summary lines (e.g. `Passed! - Failed: 0, Passed: 214, Skipped: 0`). Screenshots under `docs/evidence/ui/`. Reports must never be edited by hand to change results.

## 9. Anti-fake measures
`scripts/verify-reality`; mutation sanity (break a rule → a test must fail); forbid skipped tests; forbid mocking DB/auth in integration/E2E; random-order test execution once per phase; independent re-run by a different agent.

## 10. Accessibility checklist per key page
Keyboard-only completion, focus order, visible focus, form labels/errors, headings/landmarks, contrast, zoom 200%, reduced motion, screen-reader names for icon buttons, axe: 0 serious/critical.
