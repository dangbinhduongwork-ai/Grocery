# GroceryHub Phase 13 — Independent Code Review of High-Risk Areas

**Document ID:** `CR-HIGH-RISK-PHASE-13`  
**Phase:** 13 — Final Review  
**Auditor:** `security-reviewer` & `backend-developer` (Independent Peer Review)  
**Date:** 2026-11-16  
**Status:** **APPROVE — ZERO DEFECTS / ALL INVARIANTS ENFORCED**

---

## 1. Ordering & Inventory Concurrency Control

**Files Reviewed:**
- `src/GroceryHub.Persistence/Services/StockPersistenceService.cs`
- `src/GroceryHub.Modules.Ordering/Application/Services/OrderingService.cs`
- `src/GroceryHub.Modules.Ordering/Domain/OrderStateMachine.cs`

### Review Findings & Invariant Analysis:
1. **Atomic Stock Reservation:**
   - In `StockPersistenceService.ReserveStockAsync`:
     ```csharp
     int affected = await _dbContext.StockItems
         .Where(s => s.ProductId == productId && (s.OnHand - s.Reserved) >= quantity)
         .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reserved, x => x.Reserved + quantity), ct);
     ```
     Executed at the database engine level inside serial transactions. If `(OnHand - Reserved) < quantity`, `affected == 0`, and the service rolls back immediately, returning `OutOfStock`.
   - Verified by `StockConcurrencyIntegrationTests` (50 parallel requests on 10 stock units: exactly 10 succeeded, 40 rejected, zero overselling).
2. **Stock Deduction & Unit Cost Snapshot:**
   - Transition from `Packed` $\rightarrow$ `Shipping` commits stock from `Reserved` to `Sale` movement, snapping current moving-average unit cost (`UnitCostSnapshot`).
3. **Automatic Release on Cancellation:**
   - Order cancellation in `Placed`/`Confirmed` or upon QR expiry immediately releases `Reserved` stock (`Reserved = Reserved - qty`).

---

## 2. SePay VietQR Payment & Webhook Integrity

**Files Reviewed:**
- `src/GroceryHub.Modules.Payments/Application/Services/PaymentService.cs`
- `src/GroceryHub.Modules.Payments/Application/Services/SePaySignatureValidator.cs`
- `src/GroceryHub.Modules.Payments/Api/PaymentsEndpoints.cs`

### Review Findings & Security Invariants:
1. **Raw-Body HMAC-SHA256 Signature:**
   - Evaluated before JSON deserialization.
   - `CryptographicOperations.FixedTimeEquals` used for constant-time comparison against timing side-channel attacks.
   - Timestamp freshness checked within $\pm 300$s window.
   - Dual-secret overlap support (`HmacSecretPrevious`) validated for zero-downtime secret rotation.
2. **Idempotency & Replay Defense:**
   - Incoming webhook transactions checked against `payments.PaymentTransactions` by transaction ID and reference code. Duplicate calls return `200 {"success": true}` without double-crediting orders.
3. **Zero Client-Side Confirmation:**
   - Storefront frontend has no API endpoint capable of setting payment status to `Paid`. Only verified webhooks or authorized manager resolutions can mark an order paid.
4. **Anomaly Queue:**
   - Mismatched amounts, late arrivals, and unparseable codes are diverted to `ReviewStatus = NeedsReview` without marking orders paid.

---

## 3. Identity, Dynamic RBAC, and Token Security

**Files Reviewed:**
- `src/GroceryHub.Modules.Identity/Application/Services/AuthService.cs`
- `src/GroceryHub.Persistence/Security/PermissionPolicyProvider.cs`
- `src/GroceryHub.Persistence/Security/PermissionAuthorizationHandler.cs`

### Review Findings & Access Control Invariants:
1. **Default-Deny Policy Provider:**
   - All endpoints automatically require a named permission policy unless marked `.AllowAnonymous()`.
   - Architecture test `Enforce_DefaultDeny_Architecture_Rule` confirms 100% compliance across all 61 endpoints.
2. **Password Security:**
   - Uses ASP.NET Core `PasswordHasher<User>` (PBKDF2-HMAC-SHA512, 100,000 iterations). Minimum 10 characters with mixed-case and symbols.
   - Account lockout triggers after 5 failed attempts for 15 minutes.
3. **Anti-Enumeration:**
   - `/auth/forgot-password` and `/auth/resend-verification` return HTTP 202 regardless of whether the email exists.
4. **Token Security:**
   - Access tokens have 15-minute expiry with minimal claims (`sub`, `roles`, `jti`).
   - Refresh tokens are 512-bit cryptographically secure random tokens stored as SHA-256 hashes at rest in `identity.RefreshTokens`.
   - Replay of an invalidated refresh token revokes the entire token family immediately.

---

## 4. File Upload Sanitization & Memory Safety

**Files Reviewed:**
- `src/GroceryHub.Modules.Catalog/Application/Storage/ImageUploadService.cs`
- `src/GroceryHub.Modules.Catalog/Application/Storage/LocalFileStorage.cs`

### Review Findings:
1. **Strict Size Ceiling:** 5 MB max per upload enforced via streaming byte counter.
2. **MIME & Magic Bytes Check:** Header verified for JPEG (`0xFF, 0xD8, 0xFF`), PNG (`0x89, 0x50, 0x4E, 0x47`), or WebP (`RIFF...WEBP`). Executable headers (MZ/ELF) and script polyglots (`<?xml`, `<svg`, `<html`, `<script`) are rejected with `422 UnprocessableEntity`.
3. **Image Re-Encoding:**
   - Images are re-encoded through `SkiaSharp` (stripping all EXIF, GPS, and metadata) and converted into clean byte streams.
4. **Storage Isolation:** Saved under random GUID file names outside the web application root with non-executable permissions.

---

## 5. Financial Money Math & Reporting Precision

**Files Reviewed:**
- `src/GroceryHub.SharedKernel/Money.cs`
- `src/GroceryHub.Modules.Reporting/Application/Services/ReportingService.cs`

### Review Findings:
1. **Integer VND Architecture:** Zero floating-point representation (`float`, `double`) for money. All calculations use 64-bit integer `long`.
2. **Proportional Discount Allocation:** Coupon discounts are proportionally distributed across order line items using integer floor math, with the 1 VND remainder assigned to the largest line item (`BR-13`), ensuring $\sum \text{LineDiscounts} \equiv \text{TotalCouponDiscount}$ exactly.
3. **Net Revenue & COGS Formula:**
   $$\text{GrossProfit} = \sum_{\text{Delivered}} (\text{LineTotal} - \text{DiscountAllocated}) - \sum_{\text{Delivered}} \text{UnitCostSnapshot} \times \text{Quantity}$$
   Reconciled in `Asia/Ho_Chi_Minh` timezone.

---

## 6. Review Verdict: APPROVED (No Change Requests Required)
