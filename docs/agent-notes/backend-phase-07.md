# Backend Implementation Notes — Phase 07: Customers, Shipping, Coupons, Cart & Ordering

## 1. Scope & Accomplishments
Completed full implementation for Task `T07-02` covering backend modules, business logic, invariant enforcement, state machine transitions, Minimal API endpoints, and OpenAPI contract regeneration:

### 1.1 Customers & Locations (Step 7A)
- **Locations Endpoints (`anon`):**
  - `GET /api/v1/locations/provinces`: returns all 34 provinces, annotated with active shipping zone serviceability.
  - `GET /api/v1/locations/provinces/{code}/wards`: returns wards belonging to a province.
- **Addresses & Profile (`auth`):**
  - `GET /api/v1/me/profile`, `PUT /api/v1/me/profile`: personal profile management with Vietnamese phone number validation (`BR-17`).
  - `GET /api/v1/me/addresses`: active address list for authenticated customer.
  - `POST /api/v1/me/addresses`: creates address with maximum 10 active addresses invariant enforcement (`ADDRESS_LIMIT_REACHED`), Vietnamese phone validation (`BR-17`), single default address guarantee.
  - `PUT /api/v1/me/addresses/{id}`: updates address with object ownership check (`resource.UserId == currentUser.Id`).
  - `DELETE /api/v1/me/addresses/{id}`: soft-deletes address (`DeletedAt = now`).
  - `PUT /api/v1/me/addresses/{id}/default`: switches default address atomically.
- **Admin Customer Operations:**
  - `GET /api/v1/admin/customers` (`customers.read`): search and paged customer listing.
  - `GET /api/v1/admin/customers/{id}` (`customers.read`): customer details with lifetime order statistics.
  - `POST /api/v1/admin/customers/{id}/lock` (`customers.lock`): lock account, revoke refresh tokens, write audit log.
  - `POST /api/v1/admin/customers/{id}/unlock` (`customers.lock`): unlock account.
  - `POST /api/v1/admin/customers/{id}/anonymize` (`staff.manage`): GDPR/data privacy anonymization with audit log.

### 1.2 Promotions & Coupons (Step 7B)
- **Admin Coupon CRUD (`promotions.manage`):**
  - `GET /api/v1/admin/coupons`, `POST /api/v1/admin/coupons`, `GET /api/v1/admin/coupons/{id}`, `PUT /api/v1/admin/coupons/{id}`, `DELETE /api/v1/admin/coupons/{id}`, `GET /api/v1/admin/coupons/{id}/usage`.
  - Optimistic concurrency control using `If-Match` rowversion headers.
  - Enforced business rule: if `UsedCount > 0`, coupon is soft-deactivated (`IsActive = false`) rather than physically deleted.
- **Coupon Validation Engine (`BR-04`, `BR-13`, `DEC-011`):**
  - Evaluates coupon state: active flag, validity date window (`StartsAt <= now <= EndsAt`), `MinSubtotal` threshold, `UsageLimit`, and per-user redemption limit (`PerUserLimit`).
  - Supported types:
    - `Percent`: discount percentage capped by `MaxDiscount`.
    - `FixedAmount`: fixed VND reduction clamped to eligible subtotal.
    - `FreeShipping`: sets `CouponDiscount = ShippingFee` and zero line discounts (`DEC-011` / `Q-009`).
  - Scopes: category-specific and product-specific scopes filtering eligible lines.
  - Proportional discount allocation (`BR-13`): discounts divided proportionally across eligible items with integer division remainder allocated to the highest-subtotal item.

### 1.3 Cart & Guest-to-User Merge (Step 7C)
- **Cart Management (`anon` / `auth`):**
  - `gh_cart` cookie for guest carts; user cart bound to `UserId`.
  - `GET /api/v1/cart`: evaluates live catalog prices and stock from `inventory.StockItems`, returns items, subtotal, coupon discount, estimated total, and stock warnings (`LOW_STOCK`, `OUT_OF_STOCK`, `PRICE_CHANGED`).
  - `POST /api/v1/cart/items`: validates active status, checks available stock (`OnHand - Reserved`), caps quantity at 1..20 per line (`BR-16`).
  - `PUT /api/v1/cart/items/{id}`: adjusts item quantity (0 removes item).
  - `DELETE /api/v1/cart/items/{id}`: removes line item.
  - `POST /api/v1/cart/coupon`: applies coupon (requires `auth` per `DEC-011` / `Q-008`).
  - `DELETE /api/v1/cart/coupon`: clears coupon.
- **Login Cart Merge (`ICartMergeService`):**
  - Hooked into `AuthService.LoginAsync`: on successful login, guest cart lines from `gh_cart` cookie are merged into user cart, capped at `min(available, 20)`, guest cart entity is deleted, and cookie cleared.

### 1.4 Ordering, Checkout & State Machine (Step 7D)
- **Checkout Quote & Order Placement:**
  - `POST /api/v1/shipping/quote`: calculates shipping fee based on configured shipping zones and rates, evaluating free shipping threshold (300,000đ default).
  - `POST /api/v1/checkout/quote`: generates authoritative server-calculated order quote with live catalog prices, coupons, and shipping. Bounded payment methods: returns `availablePaymentMethods: ["COD"]`.
  - `POST /api/v1/checkout/orders` (requires `Idempotency-Key` header and rate limit policy `checkout`):
    - Replay idempotency: exact key and request hash returns cached 201 response; key reuse with differing payload returns 409 Conflict.
    - If `paymentMethod == "SEPAY_QR"`: returns 409 `PAYMENT_METHOD_UNAVAILABLE` until Phase 10.
    - If `paymentMethod == "COD"`: validates COD maximum limit (2,000,000đ per `DEC-007`).
    - Single atomic database transaction:
      - Reserves stock atomically via `IStockPersistenceService.ReserveStockAsync`.
      - Atomically redempts coupon (increments `UsedCount`, records `CouponRedemption`).
      - Fetches next sequence value from `ordering.OrderNumberSeq` -> `ORD-{yyyyMMdd}-{seq:D6}`.
      - Snapshots recipient address and item list/unit prices, computes proportional line discount allocations (`BR-13`).
      - Creates `Order` (`Status = "Placed"`), `Payment` (`Method = "COD"`, `Status = "Unpaid"`), and `OrderStatusHistory`.
      - Clears customer's cart.
      - Stores idempotency record.
      - Enqueues outbox message `OrderPlacedEvent`.
- **Order State Machine Transitions:**
  - Pure state machine implemented in `GroceryHub.Modules.Ordering.Domain.OrderStateMachine`.
  - Customer cancel (`POST /api/v1/me/orders/{id}/cancel`): permitted only in `PendingPayment`, `Placed`, `Confirmed` (`BR-07`). Releases stock reservation and coupon redemption.
  - Staff transitions (`POST /api/v1/admin/orders/{id}/transition`):
    - `Placed` -> `Confirmed` (`orders.process`)
    - `Confirmed` -> `Preparing` (`orders.process`)
    - `Preparing` -> `Packed` (`orders.process`)
    - `Packed` -> `Shipping` (`orders.ship`): commits stock via `IStockPersistenceService.CommitStockReservationAsync`, snapshots unit costs, and emits `StockMovement(Type='Sale')`.
    - `Shipping` -> `Delivered` (`orders.ship`): validates collected cash equals order total (`COD_AMOUNT_MISMATCH`), creates `CodCollection` record, marks payment `Paid`, emits `OrderDeliveredEvent`.
    - `Shipping` -> `DeliveryFailed` (`orders.ship`): records delivery failure reason.
    - `DeliveryFailed` -> `Shipping` (`orders.ship`): delivery retry.
  - Cancellation permissions (`DEC-011` / `Q-006`):
    - Unpaid orders in `PendingPayment`/`Placed`/`Confirmed` can be cancelled by staff with `orders.process` or `orders.cancel`.
    - Orders in `Preparing`/`Packed`/`DeliveryFailed` or ANY `Paid` order strictly require `orders.cancel`.
  - Operations & Printing endpoints:
    - `GET /api/v1/admin/orders/{id}/pick-list` (`orders.read`): warehouse pick list grouped by category.
    - `GET /api/v1/admin/orders/{id}/delivery-note` (`orders.read`): delivery note with recipient info and COD cash amount to collect.
    - `PUT /api/v1/admin/orders/{id}/note` (`orders.process`): append internal staff note to timeline.
    - `POST /api/v1/admin/orders/{id}/assign` (`orders.process`): assign order to staff member.
- **Admin Shipping Zones & Settings:**
  - `GET /api/v1/admin/shipping/zones`, `POST /api/v1/admin/shipping/zones`, `PUT /api/v1/admin/shipping/zones/{id}`, `DELETE /api/v1/admin/shipping/zones/{id}` (`settings.manage`).
  - `GET /api/v1/admin/settings`, `PUT /api/v1/admin/settings` (`settings.manage`): manages store settings (`FreeShippingThreshold`, `CodMaxAmount`, `MaxQuantityPerLine`, etc.) with audit logging.
- **Background Cleanup:**
  - `SweepExpiredReservationsAsync`: releases stock reservations and coupon redemptions for unpaid orders exceeding expiry timeout.

---

## 2. Evidence & Verification
- **Compilation:** `dotnet build -warnaserror` passes with 0 warnings, 0 errors.
- **Formatting:** `dotnet format --verify-no-changes` passes with 0 changes.
- **Architecture Tests:** `dotnet test tests/GroceryHub.ArchitectureTests` passed 6/6 tests:
  - Strict module isolation: no cross-module assembly or namespace leaks.
  - Default-deny security: 100% of mapped endpoints require explicit authorization policy or `AllowAnonymous()`.
  - Ban on forbidden commercial libraries (MediatR, AutoMapper, FluentAssertions).
  - Prohibition of `DateTime.Now`, `DateTime.UtcNow`, `Thread.Sleep`.
- **Unit Tests:** `dotnet test tests/GroceryHub.UnitTests` passed 205/205 tests (151 baseline + 54 new unit tests):
  - Order state machine transitions, permissions, and COD collection validation.
  - Coupon calculation engine across Percent, MaxDiscount, FixedAmount, FreeShipping, MinSubtotal, and scope filters.
  - Proportional discount allocation algorithm (`BR-13`).
  - Vietnamese phone validation (`BR-17`) and customer address limit ($\le 10$).
- **OpenAPI Contract:** Successfully regenerated `docs/contracts/openapi.json` via `--export-openapi`. All new endpoints and security requirements are included.
