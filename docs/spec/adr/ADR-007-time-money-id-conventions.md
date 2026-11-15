# ADR-007: Time (`TimeProvider` + `Asia/Ho_Chi_Minh`), Money (`long` VND), and Identifier (`Guid` v7 + SQL Sequences) Conventions

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** `backend-developer`, `database-engineer`, `payment-specialist`
- **References:** `AGENTS.md` §6, `docs/spec/02-PROJECT-REQUIREMENTS.md` (`BR-01`, `BR-06`, `BR-13`), `docs/spec/06-DATABASE-ERD.md`, `.agents/rules/backend.md`

---

## Context

GroceryHub operates in the Vietnamese e-commerce market where:
1. **Currency (`BR-01`, `AGENTS.md` §6):** All prices, discounts, shipping fees, costs, and payment amounts are denominated in **Vietnamese Đồng (VND)**, which has no fractional subunits in retail transactions. Floating-point (`double`/`float`) or unguarded decimal rounding causes penny/đồng discrepancies between order totals, coupon allocations, moving-average costs, and SePay bank transfer amounts.
2. **Time & Timezones (`BR-13`, `NFR-007`):** Servers and containers run in UTC, but Vietnamese business-day boundaries (daily revenue reports, order number date prefixes `DHyyMMdd-NNNN`, SePay `transactionDate` strings `"YYYY-MM-DD HH:mm:ss"`, and coupon validity windows) operate in **Vietnam Standard Time (`Asia/Ho_Chi_Minh`, `UTC+07:00`, no DST)**. Furthermore, unit and integration tests for QR payment expiry (`30m + 5m grace`), refresh token rotation, and 7-day review editing windows must control time deterministically without `Thread.Sleep`.
3. **Identifiers (`docs/spec/06-DATABASE-ERD.md`, `docs/spec/11-SEPAY-INTEGRATION.md`):** Random `Guid.NewGuid()` (UUID v4) causes severe B-tree index fragmentation on SQL Server clustered primary keys, while customers and bank transfer memos require compact, human-readable sequential codes (`DH261115-0001`, `GH1000000001`).

---

## Decision

We standardize time, money, and identifier representations across all modules in `GroceryHub.SharedKernel` and `GroceryHub.Persistence`:

1. **Time (`TimeProvider` / `VietnamTime`):**
   - **Injected `TimeProvider`:** All domain services, application handlers, and background jobs obtain current time exclusively via .NET's injected `System.TimeProvider` (`timeProvider.GetUtcNow()`). Direct calls to `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, or `DateTimeOffset.UtcNow` in `src/` are banned and fail Architecture Test #4.
   - **Storage & Wire Format:** All timestamps in SQL Server use `datetimeoffset(7)` stored in UTC (`+00:00`) or normalized ISO-8601 with offset on the wire (`2026-11-15T03:30:00Z` / `+07:00`).
   - **Business Timezone Helper (`VietnamTime`):** `GroceryHub.SharedKernel.Time.VietnamTime` resolves `TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")` (with cross-platform fallback `"SE Asia Standard Time"` on Windows) and provides deterministic conversions for:
     - Daily reporting boundaries (`BR-13`: start/end of day in `+07:00` converted to UTC `DateTimeOffset` range for index-friendly SQL queries).
     - Order number date prefix (`yyMMdd` in `+07:00`).
     - Parsing SePay's `transactionDate` (`"yyyy-MM-dd HH:mm:ss"` assumed `+07:00`).

2. **Money (`Money` Value Object wrapping `long` VND):**
   - **Database & C# Type:** Every monetary column in SQL Server is `bigint` (`long` in C#), representing whole integer VND (`0`, `25000`, `190000`). `decimal`, `double`, and `float` are forbidden for prices and totals (`BR-01`).
   - **`Money` Value Object (`GroceryHub.SharedKernel.Money`):**
     - `readonly record struct Money(long Amount)` with checked arithmetic (`+`, `-`, `MultiplyQuantity(int qty)`), non-negative guards (`Money.Zero`), and deterministic percentage/pro-rata allocation:
     - **Percentage Discount Formula:** `discount = (subtotal * percent) / 100L` (integer division truncated to whole VND, capped by `MaxDiscount` if present).
     - **Coupon Line Allocation (`BR-13`, `DEC-011`/`Q-009`):**
       - For `FreeShipping` coupons: `Order.CouponDiscount = Order.ShippingFee`, and every `OrderItem.DiscountAllocated = 0`.
       - For `Percent` / `FixedAmount` coupons: allocated pro-rata across eligible `OrderItems` by `lineSubtotal`, with any integer rounding remainder (`1..N` VND) assigned to the last eligible line so that $\sum \text{DiscountAllocated} == \text{CouponDiscount}$ holds identically.
     - **Moving-Average Cost Formula (`BR-01`, `BR-11`):**
       $$\text{NewCostPrice} = \text{round\_half\_up}\left(\frac{\max(0, \text{OldOnHand}) \times \text{OldCostPrice} + \text{ReceivedQty} \times \text{ReceiptUnitCost}}{\max(0, \text{OldOnHand}) + \text{ReceivedQty}}\right)$$
       computed using `Int128` intermediate math to prevent 64-bit overflow, rounded half-up to the nearest integer VND.

3. **Identifiers (`Guid.CreateVersion7()` + SQL Server `SEQUENCE`s):**
   - **Entity Primary Keys:** All entity `Id` columns (except high-volume append-only ledgers `inventory.StockMovements.Id` and `audit.AuditLogs.Id` which use `bigint IDENTITY(1,1)`, and natural code keys `identity.Permissions.Code`, `shared.Provinces.Code`, `shared.Wards.Code`, `shared.Settings.Key`) use `uniqueidentifier` (`Guid`) generated on the server via .NET 10's **`Guid.CreateVersion7(timeProvider.GetUtcNow())`**.
   - **Why UUID v7:** Time-ordered leading 48-bit timestamp eliminates SQL Server clustered index page splits while remaining globally unique without a DB round-trip before insert.
   - **Human-Readable Business Codes (Separate Unique Columns via SQL `SEQUENCE`):**
     - **Order Number (`ordering.Orders.OrderNumber`):** Format `DHyyMMdd-NNNN` (e.g., `DH261115-0001`), generated using `NEXT VALUE FOR ordering.OrderNumberSeq` formatted with the Vietnam date prefix (`yyMMdd`).
     - **Payment Code (`payments.PaymentAttempts.PaymentCode`):** Format `GH` + 10 digits (`GH1000000001`), generated from `NEXT VALUE FOR payments.PaymentCodeSeq` (`START WITH 1000000001 INCREMENT BY 1 NO CACHE` / small cache, never reused). Matches SePay payment-code structure (`DEC-006`, `docs/spec/11-SEPAY-INTEGRATION.md` §3).
     - **Purchase Order & Goods Receipt Codes:** `POyyMMdd-NNNN` and `GRyyMMdd-NNNN`.

---

## Alternatives Considered

1. **Using `decimal(18,2)` for VND amounts:**
   - *Rejected:* VND does not use fractional cents in grocery retail; `decimal` adds unnecessary fractional formatting rules on JSON/TypeScript and violates `AGENTS.md` §6 and `BR-01` (`long`, `bigint`).
2. **Using `Guid.NewGuid()` (UUID v4) for Primary Keys:**
   - *Rejected:* Random UUID v4 inserts scatter across B-tree pages in SQL Server clustered indexes, causing heavy fragmentation and page splits under load.
3. **Deriving Payment Code from `Order.Id` (UUID substring):**
   - *Rejected:* Hex/alphanumeric substrings collide or get stripped/altered by Vietnamese banking apps; a monotonic 10-digit numeric suffix (`GH1000000001`) from `payments.PaymentCodeSeq` is 100% compatible with all 23 SePay-supported banks (`docs/spec/11-SEPAY-DIFFS.md` §2d).

---

## Consequences

- **Positive:**
  - Zero floating-point or rounding drift across cart quotes, orders, coupons, SePay webhooks, and profit reports.
  - Fast, time-deterministic unit and integration tests via `FakeTimeProvider`.
  - Sequential clustered index inserts via `Guid.CreateVersion7` and clean, bank-friendly human codes via SQL `SEQUENCE`s.
- **Negative / Trade-offs:**
  - Developers must always inject `TimeProvider` and use `Money` / `long` rather than `DateTime.UtcNow` or `decimal` (enforced by Architecture Test #4 and code review).
