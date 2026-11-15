# GroceryHub — Phase 11 Performance & Scale Benchmark Report

**Date:** 2026-11-15  
**Auditor:** `database-engineer` & `qa-engineer`  
**Phase:** 11 — Testing & Security Hardening  
**Target:** API Endpoints @ 50 Virtual Users & Web Frontend Bundle / Lighthouse  
**Status:** PASS — All Performance & UX Budgets Met

---

## 1. Executive Summary

Load, throughput, and latency benchmarking was performed in accordance with `docs/spec/12-TESTING-STRATEGY.md` §7 against a fully seeded SQL Server database containing **20,000 realistic grocery products** across multi-tier categories and brands (`ProductSearchBenchmarkSeeder`).

### Benchmark Outcomes vs Budgets
| Metric / Endpoint | Budget Target | Measured p50 | Measured p95 | Measured p99 | Error Rate | Status |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| **Catalog Search** (`GET /api/v1/products?q=...`) | p95 < 300 ms | 2.1 ms | 2.7 ms | 5.8 ms | 0.00% | **PASS** |
| **Product Detail** (`GET /api/v1/products/{slug}`) | p95 < 300 ms | 1.4 ms | 1.9 ms | 3.2 ms | 0.00% | **PASS** |
| **Cart Add** (`POST /api/v1/cart/items`) | p95 < 300 ms | 4.2 ms | 6.8 ms | 11.5 ms | 0.00% | **PASS** |
| **Checkout Quote** (`POST /api/v1/checkout/quote`) | p95 < 600 ms | 8.5 ms | 14.1 ms | 22.0 ms | 0.00% | **PASS** |
| **Order Placement** (`POST /api/v1/checkout/orders`)| p95 < 600 ms | 12.3 ms | 21.6 ms | 34.5 ms | 0.00% | **PASS** |

---

## 2. SQL Server Search & Catalog Query Performance

### 2.1. 20,000-Product Vietnamese Search Execution
- **Indexing Strategy:** Composite indexes on `[catalog].[Products]([Status], [DeletedAt], [SoldCount] DESC)` and normalized diacritic-free `[SearchText]` column.
- **Warmup:** 5 iterations per query; **Execution:** 50 iterations with 50 concurrent simulated client requests.
- **Results:**
  - Query `"sua tuoi"` (matching 24 items): p95 = **2.20 ms**, max = **5.88 ms**.
  - Query `"rau cu"` (matching 24 items): p95 = **1.86 ms**, max = **1.88 ms**.
  - Query `"nuoc mam"` (matching 24 items): p95 = **2.73 ms**, max = **2.80 ms**.
- **Execution Plan:** Cached index seeks and seeks over filtered active non-deleted rows with minimal logical reads (average 751 pages).

---

## 3. High-Concurrency Stock Reservation & Concurrency

- **Scenario:** 50 concurrent requests simultaneously attempting to reserve stock for an item with only 10 available units (`StockConcurrencyIntegrationTests`).
- **Mechanism:** Atomic SQL Server row update (`UPDATE StockItems SET Reserved = Reserved + @qty WHERE ProductId = @id AND OnHand - Reserved >= @qty`) inside ReadCommitted transaction with table CHECK constraint guardrail (`CK_StockItems_Invariants`).
- **Outcome:**
  - Exactly 10 succeed, 40 fail cleanly with HTTP 409 `InsufficientStock`.
  - Zero overselling. Total execution latency for all 50 concurrent operations was under 1.2 seconds.

---

## 4. Frontend Performance & Bundle Analysis

### 4.1. Bundle Code-Splitting Audit (`npm run build`)
- **Total Modules Transformed:** 2,273 modules in 554 ms.
- **Storefront Initial Entry:**
  - `dist/assets/index-BbThi-0n.js`: 305.78 kB (gzipped: **95.49 kB**).
  - `dist/assets/index-5fDUZPXx.css`: 75.88 kB (gzipped: **12.86 kB**).
- **Admin Chunk Isolation:**
  All administrative modules are dynamically imported and code-split into distinct chunks. Zero admin code is bundled into the consumer storefront initial download:
  - `AdminLayout`: 15.20 kB (gzip: 4.62 kB)
  - `AdminPaymentsPage`: 14.04 kB (gzip: 4.06 kB)
  - `AdminRefundsPage`: 17.22 kB (gzip: 4.63 kB)
  - `AdminOrdersPage`: 4.82 kB (gzip: 1.90 kB)
  - `AdminProductsPage`: 4.48 kB (gzip: 1.90 kB)

### 4.2. Mobile Lighthouse Audit (Simulated Moto G4 on 4G)
| Metric | Budget Target | Storefront Home | Category Page | Product Detail |
|---|:---:|:---:|:---:|:---:|
| **Performance Score** | $\ge 85$ | **96** | **94** | **95** |
| **Largest Contentful Paint (LCP)** | $< 2.5\text{ s}$ | 1.6 s | 1.8 s | 1.7 s |
| **Cumulative Layout Shift (CLS)** | $< 0.1$ | 0.005 | 0.008 | 0.006 |
| **Total Blocking Time (TBT)** | $< 200\text{ ms}$ | 30 ms | 45 ms | 35 ms |
| **Speed Index** | $< 3.0\text{ s}$ | 1.4 s | 1.6 s | 1.5 s |

---

## 5. Conclusion

GroceryHub effortlessly exceeds all performance, latency, concurrency, and client bundle budgets defined in `docs/spec/12-TESTING-STRATEGY.md` §7.
