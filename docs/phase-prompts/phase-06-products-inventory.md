# Phase 06 — Products, Categories, Brands, Inventory, Purchasing

**Goal:** Catalog and stock backbone with race-safe inventory, purchasing and moving-average cost, image handling, Vietnamese-friendly search.
**Agents:** database-engineer → backend-developer ‖ ui-ux-designer (design docs started) → qa-engineer; security-reviewer (uploads).
**Inputs:** FR-CUS-002/003/004 (API side), FR-ADM-002..006, FR-STF-004/005, BR-01, BR-02, BR-11, BR-12, NFR-001, NFR-008; spec 06 catalog/inventory/purchasing schema.
**Outputs:** Catalog, Inventory, Purchasing modules; migrations; file storage abstraction; sample-data seeder (Development only); perf seed command; tests; query-plan evidence.
**Dependencies:** Phase 05 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 06 — PRODUCTS, CATEGORY, INVENTORY. Requirements: FR-CUS-002, FR-CUS-003, FR-CUS-004 (API), FR-ADM-002, FR-ADM-003, FR-ADM-004, FR-ADM-005, FR-ADM-006, FR-STF-004, FR-STF-005, BR-01, BR-02, BR-11, BR-12. Load skills ecommerce-business, efcore-sqlserver, dotnet-api.

Delegate in this order:
1. database-engineer: catalog, inventory, purchasing schemas + migrations; constraints (CHECK SellingPrice<=ListPrice, StockItems OnHand/Reserved checks), unique Sku/Slug/Barcode index, accent-insensitive search design: implement `SearchText` normalisation (lowercase, remove diacritics incl. đ->d) + index/full-text decision backed by measurements on 20,000 generated products (separate perf-seed command, Development only); atomic stock reserve/release/commit primitives (ExecuteUpdate with affected-row check) exposed through a persistence service; moving-average cost update in the goods-receipt transaction.
2. backend-developer: Catalog (categories tree, brands, products CRUD with publish status, price update with history + audit, cost visible only with catalog.cost.read, images upload: allowlist JPEG/PNG/WebP, magic-byte check, size limit 5 MB, re-encode + derivatives via an approved permissively-licensed library (check licence), IFileStorage local-volume implementation, random file keys), public read endpoints (categories tree with counts, products search/filter/sort/paging, product by slug, related, brands), Inventory (stock list, movements ledger, adjust with reason, reorder level, low-stock, near-expiry), Purchasing (suppliers, POs state machine, goods receipts immutable, receive updates stock + cost + movement), all admin endpoints with permissions from doc 07 §5. Domain unit tests for state machine and cost math (including fractional rounding to integer VND rules documented).
3. ui-ux-designer (parallel, no backend dependency; documents only because web/ does not exist yet): docs/design/{tokens,contrast,font-check,components,recipes}.md — tokens (colors, type, spacing, radii) with a real contrast table, Vietnamese font test results, list of shadcn primitives to theme, page recipes for storefront pages. No code in web/ until Phase 08.
4. qa-engineer: integration tests incl. concurrency (50 parallel reservations for stock of 10 -> exactly 10 succeed, Reserved never exceeds OnHand), price history on price change, cost update after two receipts with different unit costs (hand-calculated fixture), Staff cannot read cost / adjust stock, search accent-insensitive cases, image upload validation (bad MIME, polyglot file, oversize), RBAC matrix extension, query count assertions for list endpoints, dev sample seeder not run in Production config.
5. security-reviewer: uploads and RBAC review.
6. You: integrate, clean-checkout, verify-reality, /wf-progress-review, report.
```

## Acceptance criteria
- Concurrency test proves no oversell; DB check constraints reject invalid stock states (direct SQL attempt shown in evidence).
- Moving-average cost matches hand-computed expected values in 3 scenarios.
- Search "sua tuoi" returns "Sữa tươi ..." products; p95 < 300 ms on 20k products (k6 or timed script; evidence) with the chosen index strategy documented.
- Upload validation tests pass; stored files re-encoded; no original filename used.
- All admin endpoints have permission tests (allowed/denied).
- Dev sample seeder creates ~200 products only in Development.

## How to verify
Run the perf seed + search timing; run the concurrency test; inspect the DB with sqlcmd to confirm constraints; try uploading a `.php` renamed `.jpg`.

## Gate to Phase 07
All PASS with evidence; security sign-off for uploads; PM GO.
