# Phase 09 — Admin Dashboard (Admin, Manager, Staff)

**Goal:** Complete back-office UI and the remaining admin backend (reports, dashboard, audit viewer, settings, review moderation) with role-aware navigation.
**Agents:** ui-ux-designer → frontend-developer ‖ backend-developer → qa-engineer → security-reviewer; PM.
**Inputs:** FR-ADM-001..017, FR-STF-001..006, BR-13, NFR-010 (customer anonymisation tool); specs 07 §5, 08 §6; admin endpoints from Phases 05–08.
**Outputs:** `/admin` route tree, Reporting + Audit read endpoints, settings endpoints, E2E journeys J4/J6, screenshots, report fixtures.
**Dependencies:** Phase 08 GO. (Payments admin screens for review/refunds are completed in Phase 10.)

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 09 — ADMIN DASHBOARD. Requirements: FR-ADM-001..FR-ADM-017 (payments queue UI is completed in Phase 10), FR-STF-001..FR-STF-006, BR-13, NFR-006. Reuse structural ideas from satnaing/shadcn-admin (MIT) selectively — never wholesale — and re-skin with GroceryHub tokens (admin theme).

1. ui-ux-designer: admin recipes (shell, dashboard, order queue, order detail, product form, inventory, purchasing editor, reports, picking mode), admin tokens (surface/panel/border), StatusChip, DataTable styles, CommandPalette; update docs/design.
2. backend-developer (parallel): Reporting module: dashboard KPIs, revenue report (day/week/month; by category/product/payment method), profit report (revenue - COGS snapshot per BR-13; shipping and refunds separate lines), top products, stock valuation, CSV export; Audit viewer endpoint with filters; customer anonymise endpoint (Admin; keeps order/accounting records, removes personal data; NFR-010) with audit and test; settings endpoints (store info, shipping zones handled earlier, free-ship threshold, COD max, QR expiry, low-stock default); review moderation endpoints (approve/reject/reply); picking-list grouping by category. Reports use SQL projections/views in Infrastructure (database-engineer reviews queries and indexes). Timezone Asia/Ho_Chi_Minh for day boundaries.
3. frontend-developer: admin shell with role-aware menu and command palette; pages: Dashboard, Products (list/form with images/price/cost permission), Categories/Brands, Inventory (+movements, adjust), Suppliers, Purchase orders + goods receipt, Orders queue/detail with valid-transition actions, Customers, Staff, Roles/Permissions matrix editor, Coupons, Banners (S), Reviews moderation, Reports (+CSV), Audit logs, Settings; staff 'picking mode' (S). Route guards by permissions; direct URL access to forbidden pages shows a 403 page and the API still denies.
4. qa-engineer: report fixtures (create deterministic orders through public flows and verify revenue/profit numbers against hand-calculated values); E2E J4 (admin creates product -> storefront -> receipt -> sale -> inventory) and J6 (RBAC UI); axe on key admin pages; data-table keyboard tests.
5. ui-ux-designer: /wf-ui-review on all admin pages with screenshots at 1280 and 1920 px (+768 for picking mode).
6. security-reviewer: RBAC UI vs API consistency, cost/profit data exposure, CSV injection (formula escaping in exports), audit completeness.
7. You: integrate, clean-checkout, verify-reality, /wf-progress-review, report.
```

## Acceptance criteria
- Every FR-ADM/FR-STF item is mapped in TRACEABILITY.md to a page + test (payments queue part deferred explicitly to Phase 10).
- Profit/revenue numbers equal the hand-calculated fixture values exactly.
- CSV exports escape formulas (`=`, `+`, `-`, `@` prefixes) — test exists.
- A Staff user cannot see cost, profit, settings, roles; API returns 403 for the same calls (matrix evidence).
- Admin bundle is not downloaded by storefront routes (build analysis evidence).
- Orders can be moved through every valid transition from the UI; invalid actions are not offered.
- UI review PASS with screenshots; axe 0 serious/critical.

## How to verify
Log in as each role (seeded test users for QA only in Development), walk the menus; compare a report to a spreadsheet you compute from fixture orders.

## Gate to Phase 10
All PASS with evidence; PM GO.
