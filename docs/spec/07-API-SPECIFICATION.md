# 07 — API Specification (REST, `/api/v1` — v1.0 Baseline)

> **Version & Change Control Note (Phase 03 Freeze — 2026-11-15):** This document is frozen at **v1.0**. The running API publishes OpenAPI 3.1; after implementation, `docs/contracts/openapi.json` is generated from code and must match this specification (`ADR-005`). Any breaking change to an endpoint path, method, permission policy, or request/response schema requires a written Change Request (`docs/agent-notes/CR-<nnn>-<slug>.md`).

---

## 1. Conventions & Cross-Cutting Rules

- **Base URL:** `/api/v1` (except health endpoints `/health/live`, `/health/ready`, and Development-only `/dev/*`).
- **Serialization:** JSON (`application/json`), `camelCase` property names, string enums (`"PendingPayment"`, `"SEPAY_QR"`), UTC / ISO-8601 timestamps with offset (`2026-11-15T10:30:00+07:00`).
- **Money (`ADR-007`, `BR-01`):** Whole integer VND (`long` in C#, `number` in JSON), never floating point or decimal fractions.
- **Error Envelope (`ADR-006`):** RFC 7807 `application/problem+json`:
  ```json
  {
    "type": "https://groceryhub.vn/errors/INSUFFICIENT_STOCK",
    "title": "Insufficient stock",
    "status": 409,
    "detail": "Product 'Sữa tươi Vinamilk 1L' only has 2 units available.",
    "code": "INSUFFICIENT_STOCK",
    "traceId": "01932d8c-7c20-7a11-9b3e-00155d4a8102",
    "errors": {}
  }
  ```
  - HTTP `400 Bad Request`: Malformed JSON or unparseable query/route syntax.
  - HTTP `401 Unauthorized`: Unauthenticated or invalid/expired credentials (`UNAUTHENTICATED`, `REFRESH_TOKEN_INVALID`, `ACCOUNT_LOCKED`, `EMAIL_NOT_VERIFIED`).
  - HTTP `403 Forbidden`: Insufficient permission or failed object-ownership check (`FORBIDDEN`, `REVIEW_NOT_ELIGIBLE`).
  - HTTP `404 Not Found`: Entity not found (`NOT_FOUND`).
  - HTTP `409 Conflict`: Domain state/invariant conflict (`INSUFFICIENT_STOCK`, `ORDER_INVALID_TRANSITION`, `IDEMPOTENCY_KEY_REUSED`, `REFUND_SAME_APPROVER`, etc.).
  - HTTP `412 Precondition Failed`: Optimistic concurrency `If-Match` / `rowVersion` mismatch (`CONCURRENCY_CONFLICT`).
  - HTTP `422 Unprocessable Entity`: Request validation failure (`VALIDATION_FAILED` with field map `errors: Record<string, string[]>`) or business input rule (`QUANTITY_LIMIT_EXCEEDED`, `COUPON_INVALID`, `COD_LIMIT_EXCEEDED`).
  - HTTP `429 Too Many Requests`: Rate limit exceeded (`RATE_LIMITED`, includes `Retry-After` header).
- **Pagination:** Query params `?page=1&pageSize=24` (`page >= 1`, `1 <= pageSize <= 100`) returning:
  `PagedResponse<T> = { items: T[], page: number, pageSize: number, totalItems: number, totalPages: number }`.
- **Idempotency (`ADR-006`, `docs/spec/events.md` §2):** Header `Idempotency-Key: <uuid>` is mandatory on `POST /checkout/orders` and `POST /me/orders/{id}/payment-attempts`. Same key + same request hash returns the stored response; same key + different request hash returns `409 IDEMPOTENCY_KEY_REUSED`.
- **Optimistic Concurrency:** Resources exposing `rowVersion` (Base64 string) require `If-Match: "<rowVersion>"` on `PUT`/`PATCH` (`412 CONCURRENCY_CONFLICT` on mismatch).
- **Rate Limits:**
  - `auth` policy: `5 req / min` per IP + email (`/auth/login`, `/auth/register`, `/auth/forgot-password`).
  - `checkout` policy: `10 req / min` per authenticated user (`/checkout/orders`, `/me/orders/{id}/payment-attempts`).
  - `public-read` policy: `120 req / min` per IP.
- **Correlation:** `X-Correlation-Id` accepted from caller or generated per request and echoed in response headers and `ProblemDetails.traceId`.
- **Cookies (`ADR-003`):**
  - `gh_refresh`: `HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Max-Age=1209600` (14 days).
  - `gh_cart`: `HttpOnly; Secure; SameSite=Lax; Path=/api/v1; Max-Age=2592000` (30 days) for anonymous guest carts.

---

## 2. Permission Catalog & Role Matrix (`docs/spec/permissions-seed.md`)

All **29 permission codes** seeded in `identity.Permissions`:
`catalog.read`, `catalog.write`, `catalog.price.write`, `catalog.cost.read`, `inventory.read`, `inventory.adjust`, `purchasing.read`, `purchasing.write`, `purchasing.receive`, `orders.read`, `orders.process`, `orders.ship`, `orders.cancel`, `customers.read`, `customers.lock`, `staff.read`, `staff.create`, `staff.manage`, `roles.manage`, `payments.read`, `payments.review`, `refunds.request`, `refunds.approve`, `promotions.manage`, `reviews.moderate`, `reports.revenue`, `reports.profit`, `audit.read`, `settings.manage`.

| Permission | Staff | Manager | Admin | Notes (`DEC-011` & `permissions-seed.md`) |
|---|:-:|:-:|:-:|---|
| `catalog.read`, `inventory.read`, `purchasing.read`, `customers.read` | ✓ | ✓ | ✓ | Read-only operational views (`CostPrice` hidden unless `catalog.cost.read`) |
| `catalog.write`, `catalog.price.write`, `catalog.cost.read` | – | ✓ | ✓ | Catalog CRUD, price history (`BR-01`), and moving-average cost visibility |
| `inventory.adjust` | – | ✓ | ✓ | Manual stock adjustment with reason + reorder level (`BR-12`) |
| `purchasing.write` | – | ✓ | ✓ | Suppliers & Purchase Orders CRUD/transition (`BR-11`) |
| `purchasing.receive` | *Grant per user* | ✓ | ✓ | Post immutable `GoodsReceipts` (`FR-STF-005`) |
| `orders.read`, `orders.process`, `orders.ship` | ✓ | ✓ | ✓ | `orders.process` also allows cancelling **unpaid** orders in `PendingPayment`/`Placed`/`Confirmed` (`DEC-011`/`Q-006`) |
| `orders.cancel` | – | ✓ | ✓ | Required to cancel orders in `Preparing`/`Packed`/`DeliveryFailed` or any **Paid** order (`DEC-011`/`Q-006`) |
| `customers.lock`, `staff.read` | – | ✓ | ✓ | Lock/unlock customer accounts; view staff directory |
| `staff.create` | – | *Grant per user* | ✓ | Create `Staff` accounts (`BR-14`) |
| `staff.manage`, `roles.manage`, `settings.manage` | – | – | ✓ | Strictly Admin-only (`T-ID-03`) |
| `payments.read`, `payments.review` | – | ✓ | ✓ | View payments & resolve anomalous `NeedsReview` transactions |
| `refunds.request` | ✓ | ✓ | ✓ | Request refund (`Requested` state) |
| `refunds.approve` | – | ✓ | ✓ | Approve/reject/complete refund (`RequestedBy != DecidedBy` per `BR-15`) |
| `promotions.manage`, `reviews.moderate` | – | ✓ | ✓ | Coupons, banners, and product review moderation |
| `reports.revenue`, `reports.profit`, `audit.read` | – | ✓ | ✓ | Dashboard, revenue, profit, stock valuation, and audit logs |

- `anon` = `.AllowAnonymous()` (explicitly marked).
- `auth` = any authenticated active user (`.RequireAuthorization("auth")`); customer endpoints enforce object ownership (`resource.UserId == currentUser.Id`).

---

## 3. Canonical Domain Error Codes

| Error Code | HTTP Status | Meaning |
|---|:-:|---|
| `VALIDATION_FAILED` | `422` | Request DTO failed FluentValidation rules (`errors` dictionary populated) |
| `UNAUTHENTICATED` | `401` | Missing, invalid, or expired Bearer token or login credentials |
| `FORBIDDEN` | `403` | Caller lacks required permission or does not own the target resource |
| `NOT_FOUND` | `404` | Target resource does not exist |
| `CONFLICT` | `409` | Generic state or uniqueness conflict |
| `RATE_LIMITED` | `429` | Rate limit quota exceeded (`Retry-After` header set) |
| `IDEMPOTENCY_KEY_REUSED` | `409` | Same `Idempotency-Key` sent with a different request payload hash |
| `CONCURRENCY_CONFLICT` | `412` | `If-Match` / `rowVersion` mismatch on concurrent update |
| `EMAIL_ALREADY_REGISTERED` | `409` | Registration email already exists |
| `ACCOUNT_LOCKED` | `401`/`403` | User account is `Locked` or `Disabled` |
| `EMAIL_NOT_VERIFIED` | `403` | Login attempted before email verification completed |
| `REFRESH_TOKEN_INVALID` | `401` | Refresh cookie missing, expired, revoked, or reused (triggers family revocation) |
| `PRODUCT_UNAVAILABLE` | `409` | Product is `Draft`, `Hidden`, or soft-deleted |
| `INSUFFICIENT_STOCK` | `409` | Requested quantity exceeds `Available = OnHand - Reserved` |
| `QUANTITY_LIMIT_EXCEEDED` | `422` | Line quantity exceeds `MaxQuantityPerLine` (default `20`, DB hard cap `99` per `DEC-011`/`Q-007`) |
| `CART_EMPTY` | `422` | Checkout attempted with an empty cart |
| `COUPON_INVALID` | `422` | Coupon code not found, inactive, or no cart items match coupon scope |
| `COUPON_EXPIRED` | `422` | Current time is outside `[StartsAt, EndsAt]` |
| `COUPON_MIN_SUBTOTAL` | `422` | Cart subtotal is below `Coupon.MinSubtotal` |
| `COUPON_LIMIT_REACHED` | `409` | Coupon `UsageLimit` or `PerUserLimit` exhausted |
| `ADDRESS_LIMIT_REACHED` | `422` | Customer already has 10 active addresses (`FR-CUS-008`) |
| `ADDRESS_NOT_SERVICEABLE` | `422` | Selected `ProvinceCode` is not mapped to an active `ShippingZone` (`BR-05`, `DEC-007`) |
| `COD_LIMIT_EXCEEDED` | `422` | `PaymentMethod == COD` and `Total > CodMaxAmount` (default `2.000.000 ₫` per `DEC-007`) |
| `ORDER_INVALID_TRANSITION` | `409` | Requested state transition violates `docs/spec/state-machines.md` §1 |
| `ORDER_NOT_CANCELLABLE` | `409` | Customer attempted to cancel an order past `Confirmed` (`BR-07`) |
| `PAYMENT_METHOD_UNAVAILABLE` | `422` | Selected payment method is disabled or disallowed for this order |
| `PAYMENT_EXPIRED` | `409` | QR payment attempt has expired |
| `PAYMENT_ALREADY_PAID` | `409` | Order/payment is already `Paid` |
| `COD_AMOUNT_MISMATCH` | `422` | Recorded COD cash at delivery does not equal `Order.Total` (`T-PAY-24`) |
| `REFUND_INVALID_AMOUNT` | `422` | Refund amount $\le 0$ or exceeds `PaidAmount - AlreadyRefundedAmount` (`T-PAY-19`) |
| `REFUND_SAME_APPROVER` | `409` | Approver/rejector user ID equals `Refund.RequestedBy` (`BR-15`, `T-PAY-18`) |
| `REVIEW_NOT_ELIGIBLE` | `403` | `OrderItemId` does not belong to a `Delivered` order owned by the caller (`BR-09`) |
| `REVIEW_ALREADY_EXISTS` | `409` | A review already exists for this `OrderItemId` |
| `PO_INVALID_STATE` | `409` | Purchase Order transition or Goods Receipt violates `PurchaseOrderStatus` rules (`BR-11`) |
| `FILE_TYPE_NOT_ALLOWED` | `422` | Uploaded image MIME/magic bytes not in `[image/jpeg, image/png, image/webp]` |
| `FILE_TOO_LARGE` | `413`/`422` | Uploaded file exceeds `5 MB` |

---

## 4. Public & Customer Endpoints (`/api/v1`)

### 4.1 Authentication (`Identity` Module)

| Method | Path | Module | Auth / Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `POST` | `/auth/register` | `Identity` | `anon` (`auth` rate limit) | `{ email: string, password: string, fullName: string, phone?: string }` | `201 Created`: `{ userId: uuid, email: string, verificationRequired: bool }` | `VALIDATION_FAILED`, `EMAIL_ALREADY_REGISTERED`, `RATE_LIMITED` |
| `POST` | `/auth/verify-email` | `Identity` | `anon` | `{ userId: uuid, token: string }` | `200 OK`: `{ verified: true }` | `VALIDATION_FAILED`, `NOT_FOUND` |
| `POST` | `/auth/resend-verification` | `Identity` | `anon` (`auth` rate limit) | `{ email: string }` | `202 Accepted`: `{ accepted: true }` *(never reveals email existence)* | `VALIDATION_FAILED`, `RATE_LIMITED` |
| `POST` | `/auth/login` | `Identity` | `anon` (`auth` rate limit) | `{ email: string, password: string }` *(reads `gh_cart` cookie if present to merge guest cart)* | `200 OK` (+ sets `gh_refresh` cookie): `{ accessToken: string, expiresIn: 900, user: AuthUserDto }` | `UNAUTHENTICATED`, `ACCOUNT_LOCKED`, `EMAIL_NOT_VERIFIED`, `RATE_LIMITED` |
| `POST` | `/auth/refresh` | `Identity` | `anon` (requires `gh_refresh` cookie + header `X-Requested-With: XMLHttpRequest`) | Empty body | `200 OK` (+ rotates `gh_refresh` cookie): `{ accessToken: string, expiresIn: 900, user: AuthUserDto }` | `REFRESH_TOKEN_INVALID`, `ACCOUNT_LOCKED` |
| `POST` | `/auth/logout` | `Identity` | `auth` | Empty body | `204 No Content` (revokes refresh token family & clears `gh_refresh` cookie) | `UNAUTHENTICATED` |
| `POST` | `/auth/forgot-password` | `Identity` | `anon` (`auth` rate limit) | `{ email: string }` | `202 Accepted`: `{ accepted: true }` *(never reveals email existence)* | `VALIDATION_FAILED`, `RATE_LIMITED` |
| `POST` | `/auth/reset-password` | `Identity` | `anon` (`auth` rate limit) | `{ userId: uuid, token: string, newPassword: string }` | `200 OK`: `{ reset: true }` (revokes all refresh tokens) | `VALIDATION_FAILED`, `RATE_LIMITED` |
| `POST` | `/auth/change-password` | `Identity` | `auth` | `{ currentPassword: string, newPassword: string }` | `200 OK`: `{ changed: true }` (revokes other refresh token families) | `VALIDATION_FAILED`, `UNAUTHENTICATED` |
| `GET` | `/auth/me` | `Identity` | `auth` | — | `200 OK`: `AuthUserDto = { id: uuid, email: string, fullName: string, phone?: string, roles: string[], permissions: string[] }` | `UNAUTHENTICATED`, `ACCOUNT_LOCKED` |

### 4.2 Customer Profile, Addresses & Locations (`Customers` Module)

| Method | Path | Module | Auth / Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/me/profile` | `Customers` | `auth` | — | `200 OK`: `{ id: uuid, email: string, fullName: string, phone?: string, emailConfirmed: bool, createdAt: datetime }` | `UNAUTHENTICATED` |
| `PUT` | `/me/profile` | `Customers` | `auth` | `{ fullName: string, phone?: string }` (`BR-17` VN phone validation) | `200 OK`: `{ id: uuid, email: string, fullName: string, phone?: string }` | `VALIDATION_FAILED`, `UNAUTHENTICATED` |
| `GET` | `/me/addresses` | `Customers` | `auth` | — | `200 OK`: `AddressDto[]` (`{ id, recipientName, phone, provinceCode, provinceName, wardCode, wardName, street, note?, isDefault }`) | `UNAUTHENTICATED` |
| `POST` | `/me/addresses` | `Customers` | `auth` | `{ recipientName: string, phone: string, provinceCode: string, wardCode: string, street: string, note?: string, isDefault: bool }` | `201 Created`: `AddressDto` | `VALIDATION_FAILED`, `ADDRESS_LIMIT_REACHED` (`max 10`) |
| `PUT` | `/me/addresses/{id}` | `Customers` | `auth` (owner) | `{ recipientName: string, phone: string, provinceCode: string, wardCode: string, street: string, note?: string, isDefault: bool }` | `200 OK`: `AddressDto` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/me/addresses/{id}` | `Customers` | `auth` (owner) | — | `204 No Content` (soft delete `DeletedAt = now`) | `NOT_FOUND`, `FORBIDDEN` |
| `PUT` | `/me/addresses/{id}/default` | `Customers` | `auth` (owner) | Empty body | `200 OK`: `AddressDto` | `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/locations/provinces` | `Customers` | `anon` | — | `200 OK`: `{ version: string, items: ProvinceDto[] }` (`{ code, name, fullName, type, isServiceable }`; 34 provinces per `BR-10`) | `RATE_LIMITED` |
| `GET` | `/locations/provinces/{code}/wards` | `Customers` | `anon` | — | `200 OK`: `{ provinceCode: string, items: WardDto[] }` (`{ code, provinceCode, name, fullName, type }`) | `NOT_FOUND`, `RATE_LIMITED` |

### 4.3 Storefront Catalog (`Catalog` & `Reviews` Modules)

| Method | Path | Module | Auth / Policy | Request / Query Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/categories` | `Catalog` | `anon` | — | `200 OK`: `CategoryTreeDto[]` (`{ id, parentId?, name, slug, sortOrder, imageUrl?, activeProductCount, children: CategoryTreeDto[] }`) | `RATE_LIMITED` |
| `GET` | `/brands` | `Catalog` | `anon` | — | `200 OK`: `BrandDto[]` (`{ id, name, slug, logoUrl? }`) | `RATE_LIMITED` |
| `GET` | `/products` | `Catalog` | `anon` | Query: `q?, categoryId?, categorySlug?, brandId[]?, minPrice?, maxPrice?, inStock?, onSale?, sort? (relevance\|price_asc\|price_desc\|newest\|best_selling), page=1, pageSize=24` | `200 OK`: `PagedResponse<StorefrontProductCardDto>` (`{ id, sku, name, slug, unit, packSize?, listPrice, sellingPrice, discountPercent, primaryImageUrl?, ratingAvg, ratingCount, soldCount, inStock, lowStock }`) | `VALIDATION_FAILED`, `RATE_LIMITED` |
| `GET` | `/products/{slug}` | `Catalog` | `anon` | Route: `slug` | `200 OK`: `StorefrontProductDetailDto` (`{ id, sku, barcode?, name, slug, category: {id,name,slug}, brand?: {id,name,slug}, unit, packSize?, listPrice, sellingPrice, discountPercent, shortDescription?, description?, specs: object, images: ProductImageDto[], ratingAvg, ratingCount, soldCount, inStock, lowStock, availableQuantityCap, maxQuantityPerLine }`) | `NOT_FOUND`, `RATE_LIMITED` |
| `GET` | `/products/{id}/related` | `Catalog` | `anon` | Route: `id (uuid)` | `200 OK`: `StorefrontProductCardDto[]` (up to 8 active products in same category) | `NOT_FOUND`, `RATE_LIMITED` |
| `GET` | `/products/{id}/reviews` | `Reviews` | `anon` | Route: `id (uuid)`, Query: `rating?, page=1, pageSize=10` | `200 OK`: `{ summary: { ratingAvg: number, ratingCount: number, starCounts: Record<1..5, number> }, reviews: PagedResponse<PublicReviewDto> }` (`{ id, authorName, rating, title?, body, createdAt, editedAt?, replyBody?, replyAt? }`) | `NOT_FOUND`, `RATE_LIMITED` |
| `GET` | `/banners` | `Catalog` | `anon` | Query: `placement?` | `200 OK`: `BannerDto[]` (`{ id, title, imageUrl, linkUrl?, placement, sortOrder }`) | `RATE_LIMITED` |
| `GET` | `/home` | `Catalog` | `anon` | — | `200 OK`: `{ banners: BannerDto[], categories: CategoryTreeDto[], featuredProducts: StorefrontProductCardDto[], newArrivals: StorefrontProductCardDto[], bestSellers: StorefrontProductCardDto[], onSaleProducts: StorefrontProductCardDto[] }` | `RATE_LIMITED` |

### 4.4 Shopping Cart & Checkout (`Cart` & `Ordering` Modules)

| Method | Path | Module | Auth / Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/cart` | `Cart` | `anon` (`gh_cart` cookie) / `auth` | — | `200 OK`: `CartViewDto` (`{ id, items: CartLineViewDto[], subtotal: number, couponCode?: string, couponDiscount: number, estimatedTotal: number, maxQuantityPerLine: number, warnings: CartWarningDto[] }`) | `RATE_LIMITED` |
| `POST` | `/cart/items` | `Cart` | `anon` / `auth` | `{ productId: uuid, quantity: int }` (`1 <= quantity <= MaxQuantityPerLine`, default `20` per `DEC-011`/`Q-007`) | `200 OK` (+ sets `gh_cart` if new guest): `CartViewDto` | `PRODUCT_UNAVAILABLE`, `INSUFFICIENT_STOCK`, `QUANTITY_LIMIT_EXCEEDED`, `VALIDATION_FAILED` |
| `PATCH` | `/cart/items/{productId}` | `Cart` | `anon` / `auth` | `{ quantity: int }` (`0` removes item; `1..MaxQuantityPerLine` updates) | `200 OK`: `CartViewDto` | `PRODUCT_UNAVAILABLE`, `INSUFFICIENT_STOCK`, `QUANTITY_LIMIT_EXCEEDED`, `NOT_FOUND` |
| `DELETE` | `/cart/items/{productId}` | `Cart` | `anon` / `auth` | — | `200 OK`: `CartViewDto` | `NOT_FOUND` |
| `DELETE` | `/cart` | `Cart` | `anon` / `auth` | — | `204 No Content` | — |
| `POST` | `/cart/coupon` | `Cart` | `auth` (`DEC-011`/`Q-008`: Guest forbidden) | `{ code: string }` | `200 OK`: `CartViewDto` | `UNAUTHENTICATED`, `CART_EMPTY`, `COUPON_INVALID`, `COUPON_EXPIRED`, `COUPON_MIN_SUBTOTAL`, `COUPON_LIMIT_REACHED` |
| `DELETE` | `/cart/coupon` | `Cart` | `auth` (`DEC-011`/`Q-008`) | — | `200 OK`: `CartViewDto` | `UNAUTHENTICATED` |
| `POST` | `/shipping/quote` | `Ordering` | `auth` | `{ addressId?: uuid, provinceCode?: string, subtotal: number }` | `200 OK`: `{ provinceCode: string, provinceName: string, serviceable: bool, zoneName?: string, fee: number, freeShippingThreshold?: number, freeShippingRemaining: number }` | `ADDRESS_NOT_SERVICEABLE`, `NOT_FOUND`, `VALIDATION_FAILED` |
| `POST` | `/checkout/quote` | `Ordering` | `auth` | `{ addressId: uuid, couponCode?: string, paymentMethod?: "COD" \| "SEPAY_QR" }` | `200 OK`: `CheckoutQuoteDto` (see §6.1: includes `subtotal`, `couponDiscount`, `shippingFee`, `total`, **`availablePaymentMethods`** `("COD" \| "SEPAY_QR")[]` computed from `total <= CodMaxAmount` and `IsSePayQrEnabled`, `codMaxAmount`, `warnings`) | `CART_EMPTY`, `ADDRESS_NOT_SERVICEABLE`, `PRODUCT_UNAVAILABLE`, `INSUFFICIENT_STOCK`, `COUPON_*`, `PAYMENT_METHOD_UNAVAILABLE`, `COD_LIMIT_EXCEEDED` |
| `POST` | `/checkout/orders` | `Ordering` | `auth` + `Idempotency-Key` (`checkout` rate limit) | `{ addressId: uuid, couponCode?: string, paymentMethod: "COD" \| "SEPAY_QR", note?: string }` | `201 Created`: `PlaceOrderResponseDto` (`{ order: OrderSummaryDto, payment: OrderPaymentViewDto }`; see §6.2) | `IDEMPOTENCY_KEY_REUSED`, `CART_EMPTY`, `PRODUCT_UNAVAILABLE`, `INSUFFICIENT_STOCK`, `QUANTITY_LIMIT_EXCEEDED`, `ADDRESS_NOT_SERVICEABLE`, `COUPON_*`, `COD_LIMIT_EXCEEDED`, `PAYMENT_METHOD_UNAVAILABLE`, `RATE_LIMITED` |

### 4.5 Customer Orders, Payments & Reviews (`Ordering`, `Payments` & `Reviews` Modules)

| Method | Path | Module | Auth / Policy | Request / Query Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/me/orders` | `Ordering` | `auth` | Query: `status?, from?, to?, page=1, pageSize=10` | `200 OK`: `PagedResponse<CustomerOrderListItemDto>` (`{ id, orderNumber, status, paymentMethod, paymentStatus, total, itemCount, firstItemName, firstItemImageUrl?, placedAt, expiresAt? }`) | `UNAUTHENTICATED`, `VALIDATION_FAILED` |
| `GET` | `/me/orders/{id}` | `Ordering` | `auth` (owner) | Route: `id (uuid)` | `200 OK`: `CustomerOrderDetailDto` (`{ id, orderNumber, status, paymentMethod, paymentStatus, subtotal, couponCode?, couponDiscount, shippingFee, total, address: AddressSnapshotDto, customerNote?, items: OrderItemViewDto[], timeline: OrderStatusHistoryDto[], payment: OrderPaymentViewDto, canCancel: bool, canRetryPayment: bool, placedAt, expiresAt? }`) | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/me/orders/{id}/cancel` | `Ordering` | `auth` (owner) | `{ reason: string }` (allowed in `PendingPayment`, `Placed`, `Confirmed` per `BR-07`; if `Paid`, auto-creates `Refund` in `Requested`) | `200 OK`: `CustomerOrderDetailDto` | `ORDER_NOT_CANCELLABLE`, `ORDER_INVALID_TRANSITION`, `NOT_FOUND`, `FORBIDDEN`, `VALIDATION_FAILED` |
| `POST` | `/me/orders/{id}/reorder` | `Ordering` | `auth` (owner) | Empty body | `200 OK`: `{ cart: CartViewDto, addedLines: int, skippedItems: { productId: uuid, name: string, reason: string }[] }` | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/me/orders/{id}/payment-attempts` | `Payments` | `auth` (owner) + `Idempotency-Key` (`checkout` rate limit) | Empty body (allowed while order is `PendingPayment` and `attemptCount < 5` per `DEC-011`/`Q-010`; marks old `Open` attempt `Superseded`) | `201 Created`: `OrderPaymentViewDto` (`{ paymentId, status, method, paymentCode, expiresAt, attemptNumber, maxAttempts: 5, qr: QrPaymentDetailsDto }`) | `IDEMPOTENCY_KEY_REUSED`, `PAYMENT_ALREADY_PAID`, `ORDER_INVALID_TRANSITION`, `CONFLICT` (`max 5 attempts`), `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/payments/{paymentCode}/status` | `Payments` | `auth` (owner) | Route: `paymentCode` (`GH\d{10}`) | `200 OK`: `{ paymentCode: string, status: PaymentStatus, attemptStatus: PaymentAttemptStatus, orderId: uuid, orderNumber: string, orderStatus: OrderStatus, paidAt?: datetime, expiresAt: datetime }` *(strictly read-only)* | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/payments/sepay/webhook` | `Payments` | `anon` + HMAC (`X-SePay-Signature` + `X-SePay-Timestamp`) or `Authorization: Apikey` | `SePayWebhookPayloadDto` (`{ id, gateway, transactionDate, accountNumber, subAccount?, code?, content, transferType, description?, transferAmount, accumulated, referenceCode? }`) | `200 OK`: `{"success": true}` (or `401 {"success": false}` on auth failure per Spec 11 §5) | `401` (invalid HMAC/timestamp/API key), `400` (invalid JSON), `413` (`>64KB`), `500` (transient DB error -> SePay retry) |
| `POST` | `/products/{id}/reviews` | `Reviews` | `auth` | `{ orderItemId: uuid, rating: 1..5, title?: string, body: string }` (`BR-09` verified purchase on `Delivered` order) | `201 Created`: `CustomerReviewDto` (`{ id, productId, orderItemId, rating, title?, body, status: "Pending", createdAt }`) | `REVIEW_NOT_ELIGIBLE`, `REVIEW_ALREADY_EXISTS`, `VALIDATION_FAILED`, `NOT_FOUND` |
| `PUT` | `/me/reviews/{id}` | `Reviews` | `auth` (owner) | `{ rating: 1..5, title?: string, body: string }` (within 7 days of `CreatedAt`; resets `Status = Pending`) | `200 OK`: `CustomerReviewDto` | `CONFLICT` (`>7 days`), `FORBIDDEN`, `NOT_FOUND`, `VALIDATION_FAILED` |

---

## 5. Admin & Staff Endpoints (`/api/v1/admin/*`)

### 5.1 Catalog & Taxonomy Management (`Catalog` Module)

| Method | Path | Module | Permission Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/admin/products` | `Catalog` | `catalog.read` | Query: `q?, categoryId?, brandId?, status?, lowStock?, page=1, pageSize=24` | `200 OK`: `PagedResponse<AdminProductListItemDto>` (`costPrice` populated **only** if caller also has `catalog.cost.read`; `null` otherwise per `BR-01`) | `FORBIDDEN`, `VALIDATION_FAILED` |
| `POST` | `/admin/products` | `Catalog` | `catalog.write` | `{ sku, barcode?, name, slug?, categoryId, brandId?, unit, packSize?, listPrice, sellingPrice, initialCostPrice?, status: "Draft"\|"Active"\|"Hidden", shortDescription?, description?, specs?: object, isFeatured: bool, expiryTracked: bool, initialReorderLevel?: int }` | `201 Created`: `AdminProductDetailDto` (initializes `inventory.StockItems` row with `OnHand=0`) | `VALIDATION_FAILED`, `CONFLICT` (duplicate SKU/slug), `FORBIDDEN` |
| `GET` | `/admin/products/{id}` | `Catalog` | `catalog.read` | Route: `id (uuid)` | `200 OK`: `AdminProductDetailDto` (includes `rowVersion`, `priceHistory` summary; `costPrice` included only with `catalog.cost.read`) | `NOT_FOUND`, `FORBIDDEN` |
| `PUT` | `/admin/products/{id}` | `Catalog` | `catalog.write` (`If-Match: "<rowVersion>"`) | `{ sku, barcode?, name, slug, categoryId, brandId?, unit, packSize?, shortDescription?, description?, specs?: object, isFeatured: bool, expiryTracked: bool }` | `200 OK`: `AdminProductDetailDto` | `CONCURRENCY_CONFLICT` (`412`), `CONFLICT`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `PUT` | `/admin/products/{id}/status` | `Catalog` | `catalog.write` | `{ status: "Draft" \| "Active" \| "Hidden" }` | `200 OK`: `{ id: uuid, status: string, rowVersion: string }` | `NOT_FOUND`, `VALIDATION_FAILED`, `FORBIDDEN` |
| `PUT` | `/admin/products/{id}/price` | `Catalog` | `catalog.price.write` (`If-Match`) | `{ listPrice: number, sellingPrice: number, reason: string }` (`0 <= sellingPrice <= listPrice` per `BR-01`) | `200 OK`: `{ id: uuid, listPrice: number, sellingPrice: number, rowVersion: string }` (writes `ProductPriceHistory` + `AuditLogs`) | `VALIDATION_FAILED`, `CONCURRENCY_CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/products/{id}/images` | `Catalog` | `catalog.write` | `multipart/form-data`: `file` (JPEG/PNG/WebP $\le 5\text{ MB}$), `alt?`, `isPrimary?` | `201 Created`: `ProductImageDto` (`{ id, url, alt, sortOrder, isPrimary, width, height }`) | `FILE_TYPE_NOT_ALLOWED`, `FILE_TOO_LARGE`, `NOT_FOUND`, `FORBIDDEN` |
| `PUT` | `/admin/products/{id}/images/order` | `Catalog` | `catalog.write` | `{ imageIds: uuid[], primaryImageId: uuid }` | `200 OK`: `ProductImageDto[]` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/admin/products/{id}/images/{imageId}` | `Catalog` | `catalog.write` | — | `204 No Content` | `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/categories` | `Catalog` | `catalog.read` | — | `200 OK`: `AdminCategoryDto[]` | `FORBIDDEN` |
| `POST` | `/admin/categories` | `Catalog` | `catalog.write` | `{ parentId?: uuid, name: string, slug?: string, sortOrder: int, isActive: bool, imageKey?: string }` (depth $\le 3$) | `201 Created`: `AdminCategoryDto` | `VALIDATION_FAILED`, `CONFLICT`, `FORBIDDEN` |
| `PUT` | `/admin/categories/{id}` | `Catalog` | `catalog.write` | `{ parentId?: uuid, name: string, slug: string, sortOrder: int, isActive: bool, imageKey?: string }` | `200 OK`: `AdminCategoryDto` | `VALIDATION_FAILED`, `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/admin/categories/{id}` | `Catalog` | `catalog.write` | — | `204 No Content` (rejected with `409 CONFLICT` if category has children or products) | `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/brands` | `Catalog` | `catalog.read` | — | `200 OK`: `AdminBrandDto[]` | `FORBIDDEN` |
| `POST` | `/admin/brands` | `Catalog` | `catalog.write` | `{ name: string, slug?: string, logoKey?: string, isActive: bool }` | `201 Created`: `AdminBrandDto` | `VALIDATION_FAILED`, `CONFLICT`, `FORBIDDEN` |
| `PUT` | `/admin/brands/{id}` | `Catalog` | `catalog.write` | `{ name: string, slug: string, logoKey?: string, isActive: bool }` | `200 OK`: `AdminBrandDto` | `VALIDATION_FAILED`, `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/admin/brands/{id}` | `Catalog` | `catalog.write` | — | `204 No Content` (rejected with `409 CONFLICT` if products reference brand) | `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |

### 5.2 Inventory Management (`Inventory` Module)

| Method | Path | Module | Permission Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/admin/inventory` | `Inventory` | `inventory.read` | Query: `q?, categoryId?, lowStockOnly?, outOfStockOnly?, page=1, pageSize=24` | `200 OK`: `PagedResponse<AdminStockItemDto>` (`{ productId, sku, barcode?, productName, unit, onHand, reserved, available, reorderLevel, isLowStock, costPrice? (only with catalog.cost.read), rowVersion }`) | `FORBIDDEN` |
| `GET` | `/admin/inventory/{productId}/movements` | `Inventory` | `inventory.read` | Query: `type?, from?, to?, page=1, pageSize=24` | `200 OK`: `PagedResponse<StockMovementDto>` (`{ id, productId, type, quantity, unitCost? (only with catalog.cost.read), referenceType?, referenceId?, reason?, createdBy, createdAt }`) | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/inventory/{productId}/adjust` | `Inventory` | `inventory.adjust` | `{ delta: int (non-zero), reason: string }` (`OnHand + delta >= Reserved` per `BR-02`, `BR-12`) | `200 OK`: `AdminStockItemDto` (inserts `StockMovement(Type='Adjustment')` + `AuditLogs` + `StockAdjustedEvent`) | `VALIDATION_FAILED`, `INSUFFICIENT_STOCK` (`OnHand + delta < Reserved`), `NOT_FOUND`, `FORBIDDEN` |
| `PUT` | `/admin/inventory/{productId}/reorder-level` | `Inventory` | `inventory.adjust` | `{ reorderLevel: int (>= 0) }` | `200 OK`: `AdminStockItemDto` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/inventory/low-stock` | `Inventory` | `inventory.read` | Query: `page=1, pageSize=24` | `200 OK`: `PagedResponse<AdminStockItemDto>` (`Available <= ReorderLevel`) | `FORBIDDEN` |
| `GET` | `/admin/inventory/near-expiry` | `Inventory` | `inventory.read` | Query: `withinDays=14, page=1, pageSize=24` | `200 OK`: `PagedResponse<ExpiryLotDto>` (`{ id, productId, sku, productName, receiptLineId, expiryDate, daysRemaining, quantityRemaining }`) | `FORBIDDEN` |

### 5.3 Purchasing & Goods Receipts (`Purchasing` Module)

| Method | Path | Module | Permission Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/admin/suppliers` | `Purchasing` | `purchasing.read` | Query: `q?, page=1, pageSize=24` | `200 OK`: `PagedResponse<SupplierDto>` | `FORBIDDEN` |
| `POST` | `/admin/suppliers` | `Purchasing` | `purchasing.write` | `{ code: string, name: string, phone?: string, email?: string, address?: string, taxCode?: string, note?: string }` | `201 Created`: `SupplierDto` | `VALIDATION_FAILED`, `CONFLICT`, `FORBIDDEN` |
| `PUT` | `/admin/suppliers/{id}` | `Purchasing` | `purchasing.write` | `{ name: string, phone?: string, email?: string, address?: string, taxCode?: string, note?: string }` | `200 OK`: `SupplierDto` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/admin/suppliers/{id}` | `Purchasing` | `purchasing.write` | — | `204 No Content` (soft delete `DeletedAt = now`) | `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/purchase-orders` | `Purchasing` | `purchasing.read` | Query: `status?, supplierId?, from?, to?, page=1, pageSize=24` | `200 OK`: `PagedResponse<PurchaseOrderSummaryDto>` | `FORBIDDEN` |
| `GET` | `/admin/purchase-orders/{id}` | `Purchasing` | `purchasing.read` | Route: `id (uuid)` | `200 OK`: `PurchaseOrderDetailDto` (includes lines with `quantityOrdered`, `quantityReceived`, `unitCost`, and linked `goodsReceipts`) | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/purchase-orders` | `Purchasing` | `purchasing.write` | `{ supplierId: uuid, expectedAt?: datetime, note?: string, lines: { productId: uuid, quantityOrdered: int, unitCost: number }[] }` | `201 Created`: `PurchaseOrderDetailDto` (`Status = Draft`) | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `PUT` | `/admin/purchase-orders/{id}` | `Purchasing` | `purchasing.write` | `{ expectedAt?: datetime, note?: string, lines: { productId: uuid, quantityOrdered: int, unitCost: number }[] }` (allowed only in `Draft`) | `200 OK`: `PurchaseOrderDetailDto` | `PO_INVALID_STATE`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/purchase-orders/{id}/order` | `Purchasing` | `purchasing.write` | Empty body (`Draft -> Ordered`) | `200 OK`: `PurchaseOrderDetailDto` | `PO_INVALID_STATE`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/purchase-orders/{id}/cancel` | `Purchasing` | `purchasing.write` | `{ reason: string }` (`Draft\|Ordered -> Cancelled` when `TotalReceived == 0`) | `200 OK`: `PurchaseOrderDetailDto` | `PO_INVALID_STATE`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/purchase-orders/{id}/close` | `Purchasing` | `purchasing.write` | `{ note?: string }` (`PartiallyReceived\|Received -> Closed`) | `200 OK`: `PurchaseOrderDetailDto` | `PO_INVALID_STATE`, `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/goods-receipts` | `Purchasing` | `purchasing.read` | Query: `supplierId?, purchaseOrderId?, from?, to?, page=1, pageSize=24` | `200 OK`: `PagedResponse<GoodsReceiptSummaryDto>` | `FORBIDDEN` |
| `GET` | `/admin/goods-receipts/{id}` | `Purchasing` | `purchasing.read` | Route: `id (uuid)` | `200 OK`: `GoodsReceiptDetailDto` | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/goods-receipts` | `Purchasing` | `purchasing.receive` | `{ purchaseOrderId?: uuid, supplierId: uuid, invoiceNo: string, note?: string, lines: { productId: uuid, quantity: int, unitCost: number, expiryDate?: date, batchNo?: string }[] }` | `201 Created`: `GoodsReceiptDetailDto` (immutable; increases `OnHand` and updates moving-average `CostPrice` per `BR-01`, `BR-11`) | `PO_INVALID_STATE`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |

### 5.4 Order Fulfillment & Operations (`Ordering` Module)

| Method | Path | Module | Permission Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/admin/orders` | `Ordering` | `orders.read` | Query: `q?, status?, paymentStatus?, paymentMethod?, assignedToMe?, from?, to?, page=1, pageSize=24` | `200 OK`: `PagedResponse<AdminOrderListItemDto>` | `FORBIDDEN` |
| `GET` | `/admin/orders/{id}` | `Ordering` | `orders.read` | Route: `id (uuid)` | `200 OK`: `AdminOrderDetailDto` | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/orders/{id}/transition` | `Ordering` | Dynamic check per transition (`orders.process`, `orders.ship`, or `orders.cancel` — see `state-machines.md` §1.3 & `DEC-011`/`Q-006`) | `{ to: OrderStatus, reason?: string, codAmount?: number }` | `200 OK`: `AdminOrderDetailDto` | `ORDER_INVALID_TRANSITION`, `ORDER_NOT_CANCELLABLE`, `COD_AMOUNT_MISMATCH`, `INSUFFICIENT_STOCK`, `FORBIDDEN`, `VALIDATION_FAILED` |
| `POST` | `/admin/orders/{id}/assign` | `Ordering` | `orders.process` | `{ assignedToUserId?: uuid }` (defaults to current user if omitted) | `200 OK`: `{ id: uuid, assignedToUserId: uuid, assignedToName: string }` | `NOT_FOUND`, `FORBIDDEN` |
| `PUT` | `/admin/orders/{id}/note` | `Ordering` | `orders.process` | `{ internalNote: string }` | `200 OK`: `{ id: uuid, internalNote: string }` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/orders/{id}/pick-list` | `Ordering` | `orders.read` | Route: `id (uuid)` | `200 OK`: `PickListDto` (`{ orderId, orderNumber, status, customerNote?, groups: { categoryName: string, items: { sku, barcode?, productName, unit, packSize?, quantity, imageUrl? }[] }[] }`) | `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/orders/{id}/delivery-note` | `Ordering` | `orders.read` | Route: `id (uuid)` | `200 OK`: `DeliveryNoteDto` (`{ orderId, orderNumber, placedAt, recipientName, phone, fullAddress, paymentMethod, codAmountToCollect: number, items: OrderItemViewDto[], subtotal, couponDiscount, shippingFee, total }`) | `NOT_FOUND`, `FORBIDDEN` |

### 5.5 Customers, Staff & Roles (`Customers` & `Identity` Modules)

| Method | Path | Module | Permission Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/admin/customers` | `Customers` | `customers.read` | Query: `q?, status?, page=1, pageSize=24` | `200 OK`: `PagedResponse<AdminCustomerListItemDto>` | `FORBIDDEN` |
| `GET` | `/admin/customers/{id}` | `Customers` | `customers.read` | Route: `id (uuid)` | `200 OK`: `AdminCustomerDetailDto` (profile, addresses, order stats, recent orders) | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/customers/{id}/lock` | `Customers` | `customers.lock` | `{ reason: string }` | `200 OK`: `{ id: uuid, status: "Locked" }` (revokes refresh tokens, evicts permission cache, writes `AuditLogs`) | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/customers/{id}/unlock` | `Customers` | `customers.lock` | `{ reason: string }` | `200 OK`: `{ id: uuid, status: "Active" }` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/customers/{id}/anonymize` | `Customers` | `staff.manage` (Admin only, `NFR-010`) | `{ reason: string }` | `200 OK`: `{ id: uuid, anonymizedAt: datetime }` (scrubs PII in `Users`/`Addresses`, revokes tokens, preserves historical order totals for accounting) | `CONFLICT` (has active non-terminal orders), `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/staff` | `Identity` | `staff.read` | Query: `q?, role?, status?, page=1, pageSize=24` | `200 OK`: `PagedResponse<StaffAccountDto>` (`{ userId, employeeCode, email, fullName, phone?, position?, status, roles: string[], extraPermissions: string[], hiredAt }`) | `FORBIDDEN` |
| `POST` | `/admin/staff` | `Identity` | `staff.create` or `staff.manage` | `{ email: string, password: string, fullName: string, phone?: string, employeeCode: string, position?: string, role: "Staff" \| "Manager", extraPermissions?: string[] }` (`staff.create` may only assign `"Staff"` and no `extraPermissions`; `Manager` role or `extraPermissions` requires `staff.manage`) | `201 Created`: `StaffAccountDto` | `FORBIDDEN`, `EMAIL_ALREADY_REGISTERED`, `CONFLICT`, `VALIDATION_FAILED` |
| `PUT` | `/admin/staff/{id}` | `Identity` | `staff.manage` | `{ fullName: string, phone?: string, position?: string, status: "Active" \| "Locked" \| "Disabled", note?: string }` | `200 OK`: `StaffAccountDto` | `NOT_FOUND`, `VALIDATION_FAILED`, `FORBIDDEN` |
| `PUT` | `/admin/staff/{id}/roles` | `Identity` | `staff.manage` | `{ roles: ("Staff" \| "Manager" \| "Admin")[] }` | `200 OK`: `StaffAccountDto` (evicts permission cache + writes `AuditLogs`) | `NOT_FOUND`, `VALIDATION_FAILED`, `FORBIDDEN` |
| `PUT` | `/admin/staff/{id}/permissions` | `Identity` | `staff.manage` | `{ extraPermissions: string[] }` (`UserPermissions` grants) | `200 OK`: `StaffAccountDto` (evicts permission cache + writes `AuditLogs`) | `NOT_FOUND`, `VALIDATION_FAILED`, `FORBIDDEN` |
| `GET` | `/admin/roles` | `Identity` | `roles.manage` | — | `200 OK`: `RolePermissionMatrixDto[]` (`{ roleId, name, isImmutableAdmin: bool, permissions: string[] }`) | `FORBIDDEN` |
| `GET` | `/admin/permissions` | `Identity` | `roles.manage` | — | `200 OK`: `PermissionDefinitionDto[]` (`{ code, group, description, allowUserGrant: bool }`) | `FORBIDDEN` |
| `PUT` | `/admin/roles/{id}/permissions` | `Identity` | `roles.manage` | `{ permissions: string[] }` (Admin role cannot lose any of the 29 permissions) | `200 OK`: `RolePermissionMatrixDto` (writes before/after JSON to `AuditLogs` + invalidates permission cache) | `VALIDATION_FAILED`, `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |

### 5.6 Payments, Anomaly Review & Refunds (`Payments` Module)

| Method | Path | Module | Permission Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/admin/payments` | `Payments` | `payments.read` | Query: `method?, status?, orderNumber?, from?, to?, page=1, pageSize=24` | `200 OK`: `PagedResponse<AdminPaymentDto>` | `FORBIDDEN` |
| `GET` | `/admin/payment-transactions` | `Payments` | `payments.read` | Query: `matchStatus?, reviewStatus? (None\|NeedsReview\|Resolved), q?, from?, to?, page=1, pageSize=24` | `200 OK`: `PagedResponse<PaymentTransactionDto>` | `FORBIDDEN` |
| `POST` | `/admin/payment-transactions/{id}/resolve` | `Payments` | `payments.review` | `{ action: "AttachToOrder" \| "Refund" \| "Dismiss", orderId?: uuid, note: string }` | `200 OK`: `PaymentTransactionDto` (audits resolution; `AttachToOrder` marks payment `Paid` & confirms/revives order if stock available; `Refund` creates `Refund` in `Requested`) | `INSUFFICIENT_STOCK`, `PAYMENT_ALREADY_PAID`, `CONFLICT`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/cod-collections` | `Payments` | `payments.read` | Query: `handedOver?, collectedBy?, page=1, pageSize=24` | `200 OK`: `PagedResponse<CodCollectionDto>` | `FORBIDDEN` |
| `POST` | `/admin/cod-collections/{id}/handover` | `Payments` | `payments.review` | `{ note?: string }` | `200 OK`: `CodCollectionDto` (sets `HandedOverAt = now`, `HandedOverTo = CurrentUser.Id`) | `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/refunds` | `Payments` | `payments.read` | Query: `status?, orderId?, page=1, pageSize=24` | `200 OK`: `PagedResponse<RefundDto>` | `FORBIDDEN` |
| `POST` | `/admin/refunds` | `Payments` | `refunds.request` | `{ orderId: uuid, amount: number, reason: string }` (`amount <= paid - refunded`) | `201 Created`: `RefundDto` (`Status = Requested`) | `REFUND_INVALID_AMOUNT`, `CONFLICT`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/refunds/{id}/approve` | `Payments` | `refunds.approve` | `{ note?: string }` (`CurrentUser.Id != RequestedBy` per `BR-15`) | `200 OK`: `RefundDto` (`Status = Approved`) | `REFUND_SAME_APPROVER` (`409`), `REFUND_INVALID_AMOUNT`, `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/refunds/{id}/reject` | `Payments` | `refunds.approve` | `{ note: string }` (`CurrentUser.Id != RequestedBy`) | `200 OK`: `RefundDto` (`Status = Rejected`) | `REFUND_SAME_APPROVER` (`409`), `CONFLICT`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/refunds/{id}/complete` | `Payments` | `refunds.approve` | `{ bankReference: string, note?: string }` | `200 OK`: `RefundDto` (`Status = Completed`; updates `Payment.Status` & `Order.PaymentStatus` to `PartiallyRefunded` or `Refunded`) | `CONFLICT`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |

### 5.7 Promotions, Reviews, Reports, Audit & Settings (`Promotions`, `Reviews`, `Reporting`, `Audit`, `Ordering` Modules)

| Method | Path | Module | Permission Policy | Request Schema | Response Schema | Error Codes |
|---|---|---|---|---|---|---|
| `GET` | `/admin/coupons` | `Promotions` | `promotions.manage` | Query: `q?, isActive?, page=1, pageSize=24` | `200 OK`: `PagedResponse<CouponDto>` | `FORBIDDEN` |
| `POST` | `/admin/coupons` | `Promotions` | `promotions.manage` | `{ code: string, name: string, type: "Percent"\|"FixedAmount"\|"FreeShipping", value: number, maxDiscount?: number, minSubtotal: number, startsAt: datetime, endsAt: datetime, usageLimit?: int, perUserLimit?: int, isActive: bool, scopes?: { scopeType: "Category"\|"Product", scopeId: uuid }[] }` | `201 Created`: `CouponDto` | `VALIDATION_FAILED`, `CONFLICT`, `FORBIDDEN` |
| `PUT` | `/admin/coupons/{id}` | `Promotions` | `promotions.manage` (`If-Match`) | Same fields as `POST /admin/coupons` | `200 OK`: `CouponDto` | `CONCURRENCY_CONFLICT`, `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/admin/coupons/{id}` | `Promotions` | `promotions.manage` | — | `204 No Content` (deactivates if already redeemed, or deletes if `UsedCount == 0`) | `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/coupons/{id}/redemptions` | `Promotions` | `promotions.manage` | Query: `page=1, pageSize=24` | `200 OK`: `PagedResponse<CouponRedemptionDto>` | `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/banners` | `Catalog` | `promotions.manage` | — | `200 OK`: `AdminBannerDto[]` | `FORBIDDEN` |
| `POST` | `/admin/banners` | `Catalog` | `promotions.manage` | `{ title: string, imageKey: string, linkUrl?: string, placement: string, sortOrder: int, startsAt?: datetime, endsAt?: datetime, isActive: bool }` | `201 Created`: `AdminBannerDto` | `VALIDATION_FAILED`, `FORBIDDEN` |
| `PUT` | `/admin/banners/{id}` | `Catalog` | `promotions.manage` | Same fields as `POST /admin/banners` | `200 OK`: `AdminBannerDto` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/admin/banners/{id}` | `Catalog` | `promotions.manage` | — | `204 No Content` | `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/reviews` | `Reviews` | `reviews.moderate` | Query: `status? (Pending\|Approved\|Rejected), productId?, rating?, page=1, pageSize=24` | `200 OK`: `PagedResponse<AdminReviewDto>` | `FORBIDDEN` |
| `POST` | `/admin/reviews/{id}/approve` | `Reviews` | `reviews.moderate` | Empty body | `200 OK`: `AdminReviewDto` (`Status = Approved`; updates `Product.RatingAvg`/`RatingCount`) | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/reviews/{id}/reject` | `Reviews` | `reviews.moderate` | `{ reason?: string }` | `200 OK`: `AdminReviewDto` (`Status = Rejected`; updates `Product.RatingAvg`/`RatingCount`) | `NOT_FOUND`, `FORBIDDEN` |
| `POST` | `/admin/reviews/{id}/reply` | `Reviews` | `reviews.moderate` | `{ replyBody: string }` | `200 OK`: `AdminReviewDto` | `VALIDATION_FAILED`, `NOT_FOUND`, `FORBIDDEN` |
| `GET` | `/admin/reports/dashboard` | `Reporting` | `reports.revenue` | — | `200 OK`: `DashboardKpiDto` (`{ todayRevenue, last7DaysRevenue, last30DaysRevenue, ordersByStatus, lowStockCount, pendingReviewsCount, paymentAnomaliesCount, topProducts }`) | `FORBIDDEN` |
| `GET` | `/admin/reports/revenue` | `Reporting` | `reports.revenue` | Query: `from (date), to (date), granularity=day\|week\|month, format=json\|csv` (`Asia/Ho_Chi_Minh` boundaries per `BR-13`) | `200 OK`: `RevenueReportDto` (net product revenue of `Delivered` orders; `shippingFeeTotal` and `refundsTotal` reported on separate lines; or `text/csv` when `format=csv`) | `VALIDATION_FAILED`, `FORBIDDEN` |
| `GET` | `/admin/reports/top-products` | `Reporting` | `reports.revenue` | Query: `from (date), to (date), limit=20, format=json\|csv` | `200 OK`: `TopProductReportItemDto[]` | `VALIDATION_FAILED`, `FORBIDDEN` |
| `GET` | `/admin/reports/profit` | `Reporting` | `reports.profit` | Query: `from (date), to (date), granularity=day\|week\|month, format=json\|csv` | `200 OK`: `ProfitReportDto` (`{ totalNetRevenue, totalCogsSnapshot, grossProfit, grossMarginPercent, series: ProfitPointDto[] }` per `BR-13`) | `VALIDATION_FAILED`, `FORBIDDEN` |
| `GET` | `/admin/reports/stock-valuation` | `Reporting` | `reports.profit` | Query: `categoryId?, format=json\|csv` | `200 OK`: `StockValuationReportDto` (`{ totalOnHandUnits, totalInventoryValue, items: { productId, sku, name, onHand, costPrice, totalValue }[] }`) | `FORBIDDEN` |
| `GET` | `/admin/audit-logs` | `Audit` | `audit.read` | Query: `actorUserId?, entityType?, entityId?, action?, from?, to?, page=1, pageSize=50` | `200 OK`: `PagedResponse<AuditLogDto>` (`{ id, at, actorUserId?, actorRole?, action, entityType, entityId, beforeJson?, afterJson?, ip?, correlationId? }`) | `FORBIDDEN` |
| `GET` | `/admin/settings` | `Ordering` / `Shared` | `settings.manage` | — | `200 OK`: `StoreSettingsDto` (`{ storeName, supportPhone, supportEmail, sePayEnabled, bankShortName, bankAccountNumber, bankAccountName, transferContentPrefix?, qrExpiryMinutes, freeShippingThreshold, codMaxAmount, maxQuantityPerLine, defaultLowStockThreshold }`) | `FORBIDDEN` |
| `PUT` | `/admin/settings` | `Ordering` / `Shared` | `settings.manage` | `StoreSettingsDto` | `200 OK`: `StoreSettingsDto` (writes `AuditLogs`) | `VALIDATION_FAILED`, `FORBIDDEN` |
| `GET` | `/admin/shipping/zones` | `Ordering` | `settings.manage` | — | `200 OK`: `ShippingZoneDto[]` (`{ id, name, isActive, provinceCodes: string[], rates: { id, minSubtotal, fee, freeAboveSubtotal? }[] }`) | `FORBIDDEN` |
| `POST` | `/admin/shipping/zones` | `Ordering` | `settings.manage` | `{ name: string, isActive: bool, provinceCodes: string[], rates: { minSubtotal: number, fee: number, freeAboveSubtotal?: number }[] }` | `201 Created`: `ShippingZoneDto` | `VALIDATION_FAILED`, `CONFLICT`, `FORBIDDEN` |
| `PUT` | `/admin/shipping/zones/{id}` | `Ordering` | `settings.manage` | Same fields as `POST /admin/shipping/zones` | `200 OK`: `ShippingZoneDto` | `VALIDATION_FAILED`, `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` |
| `DELETE` | `/admin/shipping/zones/{id}` | `Ordering` | `settings.manage` | — | `204 No Content` | `NOT_FOUND`, `FORBIDDEN` |

### 5.8 Health & Development-Only Endpoints

| Method | Path | Module | Auth / Policy | Description |
|---|---|---|---|---|
| `GET` | `/health/live` | `Api` | `anon` | Process liveness probe (`200 OK` `{"status": "Healthy"}`). |
| `GET` | `/health/ready` | `Api` | `anon` | Readiness probe checking SQL Server connectivity and SePay configuration readiness (never exposes secrets). |
| `POST` | `/dev/sepay/simulate` | `Payments` | `anon` (**Development only**) | Registered **only** when `env.IsDevelopment()`. Builds a signed SePay webhook payload for a `paymentCode` / `amount` and invokes the real webhook pipeline. Returns `404 Not Found` in Production (`T-PAY-22`). |

---

## 6. Canonical Request / Response Examples

### 6.1 `POST /api/v1/checkout/quote` (`200 OK`)

`availablePaymentMethods` is computed dynamically by the server:
- Includes `"COD"` if and only if `total <= CodMaxAmount` (`2.000.000 ₫` default per `DEC-007`).
- Includes `"SEPAY_QR"` if and only if SePay QR is enabled in settings/configuration.
- When `total <= 2.000.000 ₫` and SePay is enabled: `["COD", "SEPAY_QR"]`.
- When `total > 2.000.000 ₫` and SePay is enabled: `["SEPAY_QR"]`.
- When `total <= 2.000.000 ₫` and SePay is disabled: `["COD"]`.

```json
{
  "subtotal": 320000,
  "couponCode": "FREESHIP",
  "couponDiscount": 25000,
  "shippingFee": 25000,
  "total": 320000,
  "codMaxAmount": 2000000,
  "availablePaymentMethods": ["COD", "SEPAY_QR"],
  "items": [
    {
      "productId": "01932d80-1111-7000-8000-000000000001",
      "sku": "RAU-001",
      "name": "Cải bó xôi Đà Lạt hữu cơ 300g",
      "unit": "gói",
      "unitPrice": 32000,
      "listPrice": 35000,
      "quantity": 10,
      "lineSubtotal": 320000,
      "discountAllocated": 0,
      "lineTotal": 320000
    }
  ],
  "warnings": []
}
```
*(Note per `DEC-011` / `Q-009`: Because `"FREESHIP"` is a `FreeShipping` coupon, `couponDiscount == shippingFee` (`25000`) and each item's `discountAllocated == 0`.)*

### 6.2 `POST /api/v1/checkout/orders` (`SEPAY_QR` -> `201 Created`)

```json
{
  "order": {
    "id": "01932d8a-4b10-7c21-9001-00155d4a8101",
    "orderNumber": "DH261115-0007",
    "status": "PendingPayment",
    "paymentMethod": "SEPAY_QR",
    "paymentStatus": "Pending",
    "subtotal": 185000,
    "couponCode": "CHAO10K",
    "couponDiscount": 10000,
    "shippingFee": 25000,
    "total": 200000,
    "placedAt": "2026-11-15T10:00:00+07:00",
    "expiresAt": "2026-11-15T10:30:00+07:00"
  },
  "payment": {
    "paymentId": "01932d8a-4b12-7c21-9002-00155d4a8101",
    "status": "Pending",
    "method": "SEPAY_QR",
    "paymentCode": "GH1000000123",
    "attemptNumber": 1,
    "maxAttempts": 5,
    "expiresAt": "2026-11-15T10:30:00+07:00",
    "qr": {
      "url": "https://qr.sepay.vn/img?acc=0000000000&bank=MBBank&amount=200000&des=GH1000000123",
      "bank": "MBBank",
      "accountNumber": "0000000000",
      "accountName": "CONG TY TNHH GROCERYHUB",
      "amount": 200000,
      "content": "GH1000000123"
    }
  }
}
```

### 6.3 `GET /api/v1/payments/GH1000000123/status` (`200 OK`)

```json
{
  "paymentCode": "GH1000000123",
  "status": "Paid",
  "attemptStatus": "Paid",
  "orderId": "01932d8a-4b10-7c21-9001-00155d4a8101",
  "orderNumber": "DH261115-0007",
  "orderStatus": "Confirmed",
  "paidAt": "2026-11-15T10:12:41+07:00",
  "expiresAt": "2026-11-15T10:30:00+07:00"
}
```

---

## 7. Non-Functional API Requirements

1. **Bounded Pagination:** Every list endpoint caps `pageSize <= 100`; unbounded list queries are forbidden.
2. **Mandatory Audit Trail:** All mutations to product prices/costs, inventory adjustments, goods receipts, role/staff permissions, customer lock/anonymize, order cancellations, payment anomaly resolutions, refunds, and store/shipping settings write an entry to `audit.AuditLogs`.
3. **OpenAPI Completeness (`ADR-005`):** `docs/contracts/openapi.json` includes security schemes, endpoint permission tags, `ProblemDetails` error responses, and request/response schemas for all endpoints in §4 and §5.
4. **Development Isolation:** `/dev/*` endpoints are registered only when `app.Environment.IsDevelopment()` is true and verified by an integration test to return `404` under `Production` environment configuration.
