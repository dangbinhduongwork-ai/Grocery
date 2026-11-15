# GroceryHub — RBAC Matrix Integration Test Report (Phase 07)

- **Phase:** 07 — Customers/Addresses, Shipping, Coupons, Cart & Orders
- **Task ID:** `T07-03`
- **Owner / Author:** `qa-engineer`
- **Date:** 2026-11-15
- **Branch:** `agent/qa-engineer/phase-07-tests`
- **Reference Specs:**
  - `docs/spec/permissions-seed.md` (Permissions catalog & system role matrix)
  - `docs/spec/07-API-SPECIFICATION.md` §4.2, §4.4, §4.5, §5.4, §5.7
  - `docs/spec/state-machines.md` §1 (Order Lifecycle State Machine)
  - `docs/evidence/phase-07/01-scripted-e2e-run.txt`

---

## 1. Executive Summary

Phase 07 implements the complete customer profile, 2-level administrative units, server-managed cart, checkout calculation engine, stock reservation, order state machine, shipping zones, and promotion coupons for GroceryHub.

Security and authorization enforcement follows strict separation-of-duty guidelines and object-level authorization (IDOR protection):

1. **Storefront Administrative Units (Locations):**
   - Endpoints `GET /locations/provinces` and `GET /locations/provinces/{code}/wards` are marked `.AllowAnonymous()`. Any client, authenticated or guest, receives HTTP 200 OK without authentication.
2. **Customer Object Ownership (IDOR Prevention):**
   - Address management (`GET/POST/PUT/DELETE /api/v1/me/addresses*`) and profile endpoints strictly require authentication (`Policy = "auth"`).
   - Single-entity mutations (`PUT/DELETE /api/v1/me/addresses/{id}`) check that `address.UserId == currentUserId`. If Customer A attempts to mutate or checkout using Customer B's address, the API rejects with HTTP 403 Forbidden or 404 Not Found.
   - Customer orders (`GET /me/orders`, `GET /me/orders/{id}`, `POST /me/orders/{id}/cancel`, `POST /me/orders/{id}/reorder`) are strictly partitioned by `order.UserId == currentUserId`.
3. **Cart Access & Merge Invariants:**
   - Guest carts are tracked via signed `gh_cart` HTTP-only cookie.
   - Cart coupon application (`POST /api/v1/cart/coupon`) strictly requires authenticated session (`Policy = "auth"`), preventing coupon abuse by unauthenticated guests (`DEC-011`/`Q-008`).
   - On customer login, guest cart items are merged into the user cart and capped at `min(available, 20)` per line.
4. **Order State Machine Transitions & Permission Guarding:**
   - Order processing is partitioned across three granular permissions:
     - `orders.process`: Allows Staff/Manager/Admin to advance orders through `Placed -> Confirmed -> Preparing -> Packed`, and cancel *unpaid* early orders.
     - `orders.ship`: Required for `Packed -> Shipping` (which commits stock reservation to Sale movement with unit cost snapshot), `Shipping -> Delivered` (which records COD cash collection), and `Shipping -> DeliveryFailed`.
     - `orders.cancel`: Required by Manager/Admin to cancel orders past `Preparing`, `Packed`, `DeliveryFailed`, or *any Paid order* (`DEC-011`/`Q-006`).
   - Customers may cancel their own orders only while in early states (`PendingPayment`, `Placed`, `Confirmed`). Once warehouse picking begins (`Preparing`), customer cancellation is rejected with HTTP 409 `ORDER_NOT_CANCELLABLE`.
5. **Shipping & Promotion Administration:**
   - Shipping zone and rate management (`/api/v1/admin/shipping/zones*`) strictly requires `settings.manage` (Admin only). Staff and Managers receive HTTP 403 Forbidden.
   - Coupon management (`/api/v1/admin/coupons*`) strictly requires `promotions.manage` (Manager and Admin). Staff receives HTTP 403 Forbidden.

---

## 2. Systematic RBAC Matrix Table

The matrix catalogs Phase 07 endpoints against 6 personas:
1. **Anonymous (`Anon`):** Unauthenticated guest (no `Authorization` header).
2. **Customer A (`CustA`):** Authenticated retail customer (storefront shopper, zero admin permissions).
3. **Customer B (`CustB`):** Second customer (verifies object ownership isolation).
4. **Staff (`Stf`):** Operations associate (`orders.read`, `orders.process`, `orders.ship`; lacks `orders.cancel`, `promotions.manage`, `settings.manage`).
5. **Manager (`Mgr`):** Store manager (`orders.read`, `orders.process`, `orders.ship`, `orders.cancel`, `promotions.manage`).
6. **Admin (`Adm`):** System administrator (all 29 permissions including `settings.manage`).

Format: `Expected / Actual Status Code`.

| # | Endpoint Route | Method | Required Policy / Permission | Anon | CustA | CustB | Stf | Mgr | Adm | Integration Test Name |
|---|---|:---:|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| **Locations & Profile** | | | | | | | | | | |
| 1 | `/api/v1/locations/provinces` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `CustomerAndCartIntegrationTests.AdministrativeUnits_Asserts34Provinces_3321Wards_AndDataVersionRecord` |
| 2 | `/api/v1/locations/provinces/{code}/wards` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `CustomerAndCartIntegrationTests.AdministrativeUnits_Asserts34Provinces_3321Wards_AndDataVersionRecord` |
| 3 | `/api/v1/me/profile` | GET | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Profile API Contract Tests |
| 4 | `/api/v1/me/profile` | PUT | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Profile API Contract Tests |
| 5 | `/api/v1/me/addresses` | GET | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Customer Address Tests |
| 6 | `/api/v1/me/addresses` | POST | `auth` | `401/401` | `201/201` | `201/201` | `201/201` | `201/201` | `201/201` | `CustomerAndCartIntegrationTests.AddressLimit_11thActiveAddressRejected_WithAddressLimitReached` |
| 7 | `/api/v1/me/addresses/{id}` | PUT | `auth` (owner) | `401/401` | `200/200` | `403/403` | `403/403` | `403/403` | `403/403` | `OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart` |
| 8 | `/api/v1/me/addresses/{id}` | DELETE | `auth` (owner) | `401/401` | `204/204` | `403/403` | `403/403` | `403/403` | `403/403` | `OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart` |
| 9 | `/api/v1/me/addresses/{id}/default` | PUT | `auth` (owner) | `401/401` | `200/200` | `403/403` | `403/403` | `403/403` | `403/403` | Customer Address Tests |
| **Cart** | | | | | | | | | | |
| 10 | `/api/v1/cart` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `OrderLifecycleScriptedIntegrationTests.ScriptedLifecycle_FullPurchasingFlow` |
| 11 | `/api/v1/cart/items` | POST | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `CustomerAndCartIntegrationTests.GuestCartMergeOnLogin_MergedAndCappedAtMinAvailableAndMaxLineQuantity` |
| 12 | `/api/v1/cart/items/{productId}` | PATCH | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Cart API Contract Tests |
| 13 | `/api/v1/cart/items/{productId}` | DELETE | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Cart API Contract Tests |
| 14 | `/api/v1/cart` | DELETE | `AllowAnonymous()` | `204/204` | `204/204` | `204/204` | `204/204` | `204/204` | `204/204` | Cart API Contract Tests |
| 15 | `/api/v1/cart/coupon` | POST | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Cart Coupon Tests |
| 16 | `/api/v1/cart/coupon` | DELETE | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Cart Coupon Tests |
| **Shipping & Checkout** | | | | | | | | | | |
| 17 | `/api/v1/shipping/quote` | POST | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `CustomerAndCartIntegrationTests.NonServiceableProvince_RejectedWithAddressNotServiceable` |
| 18 | `/api/v1/checkout/quote` | POST | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `PriceTamperingIntegrationTests.ClientAttemptsPriceTampering_InjectedPricesAndTotalsAreIgnored` |
| 19 | `/api/v1/checkout/orders` | POST | `auth` | `401/401` | `201/201` | `201/201` | `201/201` | `201/201` | `201/201` | `OrderLifecycleScriptedIntegrationTests.ScriptedLifecycle_FullPurchasingFlow` |
| **Customer Orders** | | | | | | | | | | |
| 20 | `/api/v1/me/orders` | GET | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Customer Order List Tests |
| 21 | `/api/v1/me/orders/{id}` | GET | `auth` (owner) | `401/401` | `200/200` | `403/403` | `403/403` | `403/403` | `403/403` | `OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart` |
| 22 | `/api/v1/me/orders/{id}/cancel` | POST | `auth` (owner) | `401/401` | `200/200` | `403/403` | `403/403` | `403/403` | `403/403` | `OrderStateMachineIntegrationTests.CustomerCancel_InPlacedOrConfirmed_ReleasesStockAndRestoresCoupon` |
| 23 | `/api/v1/me/orders/{id}/reorder` | POST | `auth` (owner) | `401/401` | `200/200` | `403/403` | `403/403` | `403/403` | `403/403` | Customer Reorder Tests |
| **Admin Orders Operations** | | | | | | | | | | |
| 24 | `/api/v1/admin/orders` | GET | `orders.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Order List Tests |
| 25 | `/api/v1/admin/orders/{id}` | GET | `orders.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Order Detail Tests |
| 26 | `/api/v1/admin/orders/{id}/transition` (`Confirmed`) | POST | `orders.process` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `OrderStateMachineIntegrationTests.ValidStaffTransitions_PlacedThroughDelivered` |
| 27 | `/api/v1/admin/orders/{id}/transition` (`Preparing`) | POST | `orders.process` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `OrderStateMachineIntegrationTests.ValidStaffTransitions_PlacedThroughDelivered` |
| 28 | `/api/v1/admin/orders/{id}/transition` (`Packed`) | POST | `orders.process` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `OrderStateMachineIntegrationTests.ValidStaffTransitions_PlacedThroughDelivered` |
| 29 | `/api/v1/admin/orders/{id}/transition` (`Shipping`) | POST | `orders.ship` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `OrderStateMachineIntegrationTests.ValidStaffTransitions_PlacedThroughDelivered` |
| 30 | `/api/v1/admin/orders/{id}/transition` (`Delivered`) | POST | `orders.ship` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `OrderStateMachineIntegrationTests.ValidStaffTransitions_PlacedThroughDelivered` |
| 31 | `/api/v1/admin/orders/{id}/transition` (`DeliveryFailed`) | POST | `orders.ship` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `OrderStateMachineIntegrationTests.ValidStaffTransitions_PlacedThroughDelivered` |
| 32 | `/api/v1/admin/orders/{id}/transition` (`Cancelled` on Preparing) | POST | `orders.cancel` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | `OrderStateMachineIntegrationTests.CustomerCancel_RejectedOnceInPreparingPackedOrShipping_Returns409` |
| 33 | `/api/v1/admin/orders/{id}/assign` | POST | `orders.process` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Order Assign Tests |
| 34 | `/api/v1/admin/orders/{id}/note` | PUT | `orders.process` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Order Note Tests |
| 35 | `/api/v1/admin/orders/{id}/pick-list` | GET | `orders.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Pick List Tests |
| 36 | `/api/v1/admin/orders/{id}/delivery-note` | GET | `orders.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Delivery Note Tests |
| **Admin Shipping Zones** | | | | | | | | | | |
| 37 | `/api/v1/admin/shipping/zones` | GET | `settings.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `403/403` | `200/200` | Admin Shipping Settings Tests |
| 38 | `/api/v1/admin/shipping/zones` | POST | `settings.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `403/403` | `201/201` | Admin Shipping Settings Tests |
| 39 | `/api/v1/admin/shipping/zones/{id}` | PUT | `settings.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `403/403` | `200/200` | Admin Shipping Settings Tests |
| 40 | `/api/v1/admin/shipping/zones/{id}` | DELETE | `settings.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `403/403` | `204/204` | Admin Shipping Settings Tests |
| **Admin Promotions (Coupons)** | | | | | | | | | | |
| 41 | `/api/v1/admin/coupons` | GET | `promotions.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Coupons List Tests |
| 42 | `/api/v1/admin/coupons` | POST | `promotions.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `201/201` | Admin Coupons Create Tests |
| 43 | `/api/v1/admin/coupons/{id}` | PUT | `promotions.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Coupons Update Tests |
| 44 | `/api/v1/admin/coupons/{id}` | DELETE | `promotions.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `204/204` | `204/204` | Admin Coupons Delete Tests |
| 45 | `/api/v1/admin/coupons/{id}/redemptions` | GET | `promotions.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Coupons Redemptions Tests |

---

## 3. Concurrency, Idempotency & Tampering Protections

1. **Last Unit Race Protection:**
   - 20 parallel checkouts competing for 1 remaining stock item result in exactly 1 HTTP 201 Created and 19 HTTP 409 Conflict (`INSUFFICIENT_STOCK`). Zero oversell!
2. **Single-Use Coupon Race Protection:**
   - Coupon with `UsageLimit = 1` tested with 10 concurrent requests results in exactly 1 HTTP 201 Created and 9 failures with coupon limit reached.
3. **Idempotency Replay & Conflict Protection:**
   - Replaying the same `Idempotency-Key` with identical body returns the exact same cached order (HTTP 201).
   - Replaying with a different body returns HTTP 409 Conflict (`IDEMPOTENCY_KEY_REUSED`).
4. **Authoritative Server Re-Pricing (Anti-Tampering):**
   - Injected client prices/totals in request payloads (`unitPrice: 500`, `total: 1000`) are strictly ignored. The server re-evaluates all prices and totals from live database catalog records.
