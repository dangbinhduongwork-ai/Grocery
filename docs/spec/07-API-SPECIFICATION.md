# 07 — API Specification (REST, `/api/v1`)

The running API publishes OpenAPI 3.1; this document is the **design contract**. After implementation, `docs/contracts/openapi.json` is generated from code and must match this spec (differences require a Change Request). Auth column: `anon` = anonymous allowed; `auth` = any authenticated user (owner checks apply); otherwise a permission code.

## 1. Conventions
- JSON, camelCase, UTC ISO-8601 timestamps with offset; money as integer VND (`number`), never decimals.
- Errors: RFC 7807 `application/problem+json` with `type`, `title`, `status`, `detail`, `code` (domain error code, §3), `traceId`, and `errors` (field map) for validation (HTTP 422 for validation, 400 for malformed).
- Pagination: `?page=1&pageSize=24` (max 100) → `{ items, page, pageSize, totalItems, totalPages }`. Sorting: `?sort=price_asc`. Filtering via named query params.
- Idempotency: `Idempotency-Key: <uuid>` required on `POST /checkout/orders` and `POST /me/orders/{id}/payment-attempts`; same key + same body → same response; same key + different body → 409 `IDEMPOTENCY_KEY_REUSED`.
- Concurrency: resources with `rowVersion` use `If-Match` on `PUT/PATCH` (412 on mismatch).
- Rate limits (429 with `Retry-After`): auth endpoints 5/min/IP+email; checkout 10/min/user; public reads 120/min/IP.
- Correlation: `X-Correlation-Id` accepted/returned.
- Cookies: `gh_refresh` (HttpOnly, Secure, SameSite=Strict, Path=`/api/v1/auth`), `gh_cart` (HttpOnly, Secure, SameSite=Lax) for guest carts.

## 2. Permission catalog and role matrix
Codes (seeded to `identity.Permissions`):
`catalog.read`, `catalog.write`, `catalog.price.write`, `catalog.cost.read`, `inventory.read`, `inventory.adjust`, `purchasing.read`, `purchasing.write`, `purchasing.receive`, `orders.read`, `orders.process`, `orders.ship`, `orders.cancel`, `customers.read`, `customers.lock`, `staff.read`, `staff.create`, `staff.manage`, `roles.manage`, `payments.read`, `payments.review`, `refunds.request`, `refunds.approve`, `promotions.manage`, `reviews.moderate`, `reports.revenue`, `reports.profit`, `audit.read`, `settings.manage`.

| Permission | Staff | Manager | Admin |
|---|:-:|:-:|:-:|
| catalog.read | ✓ | ✓ | ✓ |
| catalog.write | – | ✓ | ✓ |
| catalog.price.write, catalog.cost.read | – | ✓ | ✓ |
| inventory.read | ✓ | ✓ | ✓ |
| inventory.adjust | – | ✓ | ✓ |
| purchasing.read | ✓ | ✓ | ✓ |
| purchasing.write | – | ✓ | ✓ |
| purchasing.receive | grant per user | ✓ | ✓ |
| orders.read, orders.process, orders.ship | ✓ | ✓ | ✓ |
| orders.cancel | – | ✓ | ✓ |
| customers.read | ✓ | ✓ | ✓ |
| customers.lock | – | ✓ | ✓ |
| staff.read | – | ✓ | ✓ |
| staff.create | – | grant per user | ✓ |
| staff.manage, roles.manage, settings.manage | – | – | ✓ |
| payments.read | – | ✓ | ✓ |
| payments.review | – | ✓ | ✓ |
| refunds.request | ✓ | ✓ | ✓ |
| refunds.approve | – | ✓ | ✓ |
| promotions.manage, reviews.moderate | – | ✓ | ✓ |
| reports.revenue | – | ✓ | ✓ |
| reports.profit | – | ✓ | ✓ |
| audit.read | – | ✓ | ✓ |
Customer has no admin permissions. Admin always has all. `UserPermissions` can add any permission to a specific user (audited).

## 3. Error codes (non-exhaustive; each has an automated test)
`VALIDATION_FAILED`, `UNAUTHENTICATED`, `FORBIDDEN`, `NOT_FOUND`, `CONFLICT`, `RATE_LIMITED`, `IDEMPOTENCY_KEY_REUSED`, `CONCURRENCY_CONFLICT`, `EMAIL_ALREADY_REGISTERED`, `ACCOUNT_LOCKED`, `EMAIL_NOT_VERIFIED`, `REFRESH_TOKEN_INVALID`, `PRODUCT_UNAVAILABLE`, `INSUFFICIENT_STOCK`, `QUANTITY_LIMIT_EXCEEDED`, `CART_EMPTY`, `COUPON_INVALID`, `COUPON_EXPIRED`, `COUPON_MIN_SUBTOTAL`, `COUPON_LIMIT_REACHED`, `ADDRESS_LIMIT_REACHED`, `ADDRESS_NOT_SERVICEABLE`, `COD_LIMIT_EXCEEDED`, `ORDER_INVALID_TRANSITION`, `ORDER_NOT_CANCELLABLE`, `PAYMENT_METHOD_UNAVAILABLE`, `PAYMENT_EXPIRED`, `PAYMENT_ALREADY_PAID`, `COD_AMOUNT_MISMATCH`, `REFUND_INVALID_AMOUNT`, `REFUND_SAME_APPROVER`, `REVIEW_NOT_ELIGIBLE`, `REVIEW_ALREADY_EXISTS`, `PO_INVALID_STATE`, `FILE_TYPE_NOT_ALLOWED`, `FILE_TOO_LARGE`.

## 4. Public and customer endpoints
### Auth
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | /auth/register | anon | `{email, password, fullName, phone?}` → 201; sends verification email |
| POST | /auth/verify-email | anon | `{userId, token}` |
| POST | /auth/resend-verification | anon | always 202 |
| POST | /auth/login | anon | `{email,password}` → `{accessToken, expiresIn, user}` + refresh cookie; merges guest cart if `gh_cart` present |
| POST | /auth/refresh | anon (cookie) | rotates refresh; requires header `X-Requested-With: XMLHttpRequest` |
| POST | /auth/logout | auth | revokes refresh family; clears cookie |
| POST | /auth/forgot-password | anon | always 202 |
| POST | /auth/reset-password | anon | `{userId, token, newPassword}` |
| POST | /auth/change-password | auth | `{currentPassword,newPassword}`; revokes other sessions |
| GET | /auth/me | auth | `{id,email,fullName,roles,permissions}` |

### Profile and addresses
| Method | Path | Auth | Description |
|---|---|---|---|
| GET/PUT | /me/profile | auth | name, phone |
| GET/POST | /me/addresses | auth | list / create (max 10) |
| PUT/DELETE | /me/addresses/{id} | auth (owner) | update / soft delete |
| PUT | /me/addresses/{id}/default | auth (owner) | set default |
| GET | /locations/provinces | anon | 34 provinces/cities (cacheable) |
| GET | /locations/provinces/{code}/wards | anon | wards/communes |

### Catalog
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | /categories | anon | tree with product counts |
| GET | /brands | anon | |
| GET | /products | anon | `q, categoryId, brandId[], minPrice, maxPrice, inStock, onSale, sort, page, pageSize` |
| GET | /products/{slug} | anon | detail incl. images, rating summary, availability (`inStock`, `lowStock`), related ids |
| GET | /products/{id}/related | anon | |
| GET | /products/{id}/reviews | anon | approved reviews, paged |
| GET | /banners | anon | active banners by placement |
| GET | /home | anon | aggregated home sections (featured, new, best sellers, promos) |

### Cart and checkout
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | /cart | anon (cookie) / auth | cart view with live prices, warnings |
| POST | /cart/items | anon/auth | `{productId, quantity}` |
| PATCH | /cart/items/{productId} | anon/auth | `{quantity}` (0 removes) |
| DELETE | /cart/items/{productId}, /cart | anon/auth | |
| POST | /cart/coupon, DELETE /cart/coupon | auth | `{code}` |
| POST | /shipping/quote | auth | `{addressId or provinceCode, subtotal}` → `{fee, freeShippingRemaining, serviceable}` |
| POST | /checkout/quote | auth | `{addressId, couponCode?, paymentMethod?}` → totals, `availablePaymentMethods` (e.g. `["COD"]` until SePay is enabled), warnings (nothing persisted) |
| POST | /checkout/orders | auth + Idempotency-Key | `{addressId, couponCode?, paymentMethod, note?}` → 201 `{order, payment}`; for `SEPAY_QR` includes `payment.qr {url, bank, accountNumber, accountName, amount, content, expiresAt}` |

### Orders and payments (customer)
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | /me/orders | auth | filters `status, from, to`, paged |
| GET | /me/orders/{id} | auth (owner) | detail + timeline |
| POST | /me/orders/{id}/cancel | auth (owner) | `{reason}`; allowed per BR-07 |
| POST | /me/orders/{id}/reorder | auth (owner) | adds available items to cart, returns report |
| POST | /me/orders/{id}/payment-attempts | auth (owner) + Idempotency-Key | new QR for an expired/unpaid QR order still `PendingPayment` |
| GET | /payments/{paymentCode}/status | auth (owner) | `{status, orderStatus, paidAt?, expiresAt}` — **read-only**, source of truth for the UI |
| POST | /payments/sepay/webhook | anon + HMAC/API key | see doc 11 |
| POST | /products/{id}/reviews | auth | verified purchase; `{orderItemId, rating, title?, body}` |
| PUT | /me/reviews/{id} | auth (owner) | within 7 days |

## 5. Admin / staff endpoints (prefix `/admin`)
| Area | Endpoints | Permission |
|---|---|---|
| Products | GET/POST `/admin/products`, GET/PUT `/admin/products/{id}` (+`If-Match`), PUT `/admin/products/{id}/status`, PUT `/admin/products/{id}/price` (writes history), POST `/admin/products/{id}/images` (multipart), PUT `/admin/products/{id}/images/order`, DELETE image | read: `catalog.read`; write: `catalog.write`; price: `catalog.price.write`; cost fields only with `catalog.cost.read` |
| Taxonomy | CRUD `/admin/categories`, `/admin/brands` | `catalog.write` |
| Inventory | GET `/admin/inventory`, GET `/admin/inventory/{productId}/movements`, POST `/admin/inventory/{productId}/adjust` `{delta, reason}`, GET `/admin/inventory/low-stock`, GET `/admin/inventory/near-expiry`, PUT `/admin/inventory/{productId}/reorder-level` | `inventory.read` / `inventory.adjust` |
| Purchasing | CRUD `/admin/suppliers`; POST/GET `/admin/purchase-orders`, POST `/admin/purchase-orders/{id}/order`, POST `/admin/purchase-orders/{id}/cancel`; POST `/admin/goods-receipts` (against PO or direct) | `purchasing.read/write`; receipts: `purchasing.receive` |
| Orders | GET `/admin/orders` (filters), GET `/admin/orders/{id}`, POST `/admin/orders/{id}/transition` `{to, reason?, codAmount?}`, POST `/admin/orders/{id}/assign`, PUT `/admin/orders/{id}/note`, GET `/admin/orders/{id}/pick-list`, GET `/admin/orders/{id}/delivery-note` | `orders.read`; transitions: `orders.process`/`orders.ship`; cancel: `orders.cancel` (+ allowed customer-side cancel) |
| Customers | GET `/admin/customers`, GET `/admin/customers/{id}`, POST `/admin/customers/{id}/lock|unlock`, POST `/admin/customers/{id}/anonymize` (Admin) | `customers.read`, `customers.lock`; anonymize: `staff.manage` |
| Staff | CRUD `/admin/staff`, PUT `/admin/staff/{id}/roles`, PUT `/admin/staff/{id}/permissions` | `staff.read/create/manage` |
| Roles | GET `/admin/roles`, GET `/admin/permissions`, PUT `/admin/roles/{id}/permissions` | `roles.manage` |
| Payments | GET `/admin/payments`, GET `/admin/payment-transactions` (filters incl. `reviewStatus`), POST `/admin/payment-transactions/{id}/resolve` `{action: AttachToOrder|Refund|Dismiss, orderId?, note}`, POST `/admin/refunds` (request), POST `/admin/refunds/{id}/approve|reject|complete` | `payments.read`, `payments.review`, `refunds.request`, `refunds.approve` |
| Promotions | CRUD `/admin/coupons`, GET `/admin/coupons/{id}/redemptions`, CRUD `/admin/banners` | `promotions.manage` |
| Reviews | GET `/admin/reviews`, POST `/admin/reviews/{id}/approve|reject|reply` | `reviews.moderate` |
| Reports | GET `/admin/reports/dashboard`, `/admin/reports/revenue`, `/admin/reports/profit`, `/admin/reports/top-products`, `/admin/reports/stock-valuation`; `?format=csv` | `reports.revenue` / `reports.profit` |
| Audit | GET `/admin/audit-logs` | `audit.read` |
| Settings | GET/PUT `/admin/settings`, CRUD `/admin/shipping/zones` and rates | `settings.manage` |

## 6. Response shape examples (abbreviated)
```json
// POST /checkout/orders (SEPAY_QR) -> 201
{
  "order": { "id": "…", "orderNumber": "DH261114-0007", "status": "PendingPayment",
            "subtotal": 185000, "couponDiscount": 10000, "shippingFee": 15000, "total": 190000 },
  "payment": { "status": "Pending", "method": "SEPAY_QR", "paymentCode": "GH1000000123",
               "expiresAt": "2026-11-14T10:30:00+07:00",
               "qr": { "url": "https://qr.sepay.vn/img?acc=…&bank=…&amount=190000&des=GH1000000123",
                       "bank": "…", "accountNumber": "…", "accountName": "…", "amount": 190000, "content": "GH1000000123" } }
}
```
```json
// GET /payments/GH1000000123/status
{ "status": "Paid", "orderStatus": "Confirmed", "paidAt": "2026-11-14T10:12:41+07:00", "expiresAt": "2026-11-14T10:30:00+07:00" }
```

## 7. Non-functional API requirements
- All list endpoints cap `pageSize`; unbounded queries are forbidden.
- All write endpoints are audited when they touch price, stock, roles/permissions, orders (cancel), payments (review/refund), settings.
- OpenAPI must include auth requirements, error responses, and examples for checkout and payment status.
- Dev-only endpoints (e.g. sample data, SePay simulator) live under `/dev/*`, are registered only when `IsDevelopment()`, and have a test proving they return 404 in Production configuration.
