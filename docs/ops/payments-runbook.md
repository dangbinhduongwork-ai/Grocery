# GroceryHub — SePay Payments & VietQR Operational Runbook

**Document ID:** `OPS-PAY-001`  
**Version:** 1.0.0  
**Phase:** 10  
**Target Roles:** DevOps Engineer, System Administrator, Operations Manager, Finance Staff  
**Related Specs:** [`docs/spec/11-SEPAY-INTEGRATION.md`](../spec/11-SEPAY-INTEGRATION.md), [`docs/spec/06-DATABASE-DESIGN.md`](../spec/06-DATABASE-DESIGN.md)

---

## 1. Architecture & Security Overview

GroceryHub integrates with **SePay** for automated VietQR payment verification.
The integration strictly adheres to **Zero Client-Side Confirmation**: no client or mobile app endpoint can mark an order or payment as paid. All payment confirmations occur exclusively through:
1. Validated incoming SePay webhooks (`POST /api/v1/payments/sepay-webhook`) signed with HMAC-SHA256.
2. Background reconciliation service polling SePay API v2.
3. Operations Manager resolution via the Payments Review Queue (`AttachToOrder`).

All monetary amounts are strictly VND 64-bit integers (`long`).

---

## 2. Zero-Downtime HMAC Secret Rotation Procedure

SePay webhooks are authenticated via HMAC-SHA256 with timestamp verification (`X-SePay-Signature`, `X-SePay-Timestamp`). To rotate the HMAC secret without dropping incoming webhook calls during propagation:

### Step 1: Pre-configure Previous Secret
1. Identify the current active secret: `SEPAY_WEBHOOK_SECRET` (let's call it `Secret_A`).
2. Generate the new secret in SePay dashboard or environment: `Secret_B`.
3. In GroceryHub deployment configuration (`.env` or Docker secrets / Kubernetes secret):
   ```bash
   SEPAY_WEBHOOK_SECRET="Secret_B"
   SEPAY_WEBHOOK_SECRET_PREVIOUS="Secret_A"
   ```
4. Restart or reload GroceryHub API.
   *Verification:* GroceryHub's `SePaySignatureValidator` now checks `Secret_B` first; if signature fails, it falls back to `Secret_A`.

### Step 2: Update SePay Webhook Dashboard
1. Log in to [SePay Dashboard](https://my.sepay.vn).
2. Go to **Webhooks** -> Edit Webhook -> Update **HMAC Secret** to `Secret_B`.
3. Save changes. Incoming webhooks will immediately be signed using `Secret_B`.

### Step 3: Monitor & Clean Up
1. Check application logs for any `401 Unauthorized` responses on `/api/v1/payments/sepay-webhook`.
2. Verify incoming webhooks process successfully with HTTP `200 {"success": true}`.
3. Once all webhook traffic uses `Secret_B` (typically after 15–30 minutes), remove `SEPAY_WEBHOOK_SECRET_PREVIOUS` from `.env`:
   ```bash
   SEPAY_WEBHOOK_SECRET="Secret_B"
   SEPAY_WEBHOOK_SECRET_PREVIOUS=""
   ```
4. Restart/reload the API. Rotation is complete with zero downtime and zero lost webhooks.

---

## 3. Webhook Delivery Failures & Replay Procedure

When SePay attempts to deliver a webhook and encounters a network glitch or a temporary 500 error:
- GroceryHub records the event in `payments.WebhookEvents` with `ProcessingStatus = Failed` and stores the verbatim `RawBody`.
- SePay automatically retries delivery up to 10 times with exponential backoff (up to 24 hours).

### Manual Replay from Admin UI or API:
If a webhook failed permanently or needs operator re-triggering:
1. Navigate to **Admin Dashboard** -> **Thanh toán & Thu chi** -> **Webhook Logs** (`/admin/payments`).
2. Filter by status `Failed`.
3. Inspect `ErrorMessage` and `RawBody`.
4. Click **"Thử lại" (Replay)** or call:
   ```http
   POST /api/v1/admin/payments/webhooks/{webhookEventId}/replay
   Authorization: Bearer <Admin_Or_Manager_Token>
   ```
5. The API re-executes `ProcessWebhookAsync` using the exact stored `RawBody`.
6. Result: Upon success, status updates to `Processed`, and the associated order transitions to `Confirmed`.

---

## 4. Anomaly Review Queue Resolution Protocol (`FR-ADM-011`)

When a customer pays with an incorrect amount, after order expiry, or with a typo in the transfer content, the transaction is safely captured in `payments.PaymentTransactions` with `ReviewStatus = NeedsReview`.

Access the Review Queue at: **Admin -> Thanh toán -> Hàng đợi soát xét** (`/admin/payments?tab=review`).

### Anomaly Types & Handling:

| Anomaly Type | Cause | Resolution Action |
|---|---|---|
| `Underpaid` | Customer transferred less than required total (e.g. 150k instead of 180k) | Contact customer for remaining transfer OR Click **"Hoàn tiền" (Refund)**. |
| `Overpaid` | Customer transferred more than order total | Click **"Gán vào đơn hàng" (AttachToOrder)** to confirm order, then click **"Hoàn tiền"** for the excess amount. |
| `LateAfterExpiry` | Transfer arrived after 30m QR expiry + 5m grace period | If inventory is still available: Click **"Gán vào đơn hàng"** (revives order to `Confirmed`). If out of stock: Click **"Hoàn tiền"**. |
| `DuplicatePayment` | Customer sent two transfers for the same order | Order is already paid by 1st transfer. For 2nd transfer: Click **"Hoàn tiền"**. |
| `UnknownCode` | Customer omitted `GH...` code or typed an invalid code | Search customer name / amount in phone inquiries. If order found: **"Gán vào đơn hàng"**. If unknown: **"Hoàn tiền"** or wait for customer inquiry. |
| `WrongAccount` | Transfer received on an unrecognized sub-account | Investigate bank statement -> **"Gán vào đơn hàng"** or **"Bỏ qua" (Dismiss)**. |

### Available Actions:
1. **Gán vào đơn hàng (Attach to Order):**
   - Provide target `orderId`.
   - Links transaction to order and marks order as `Confirmed` (if stock allows).
2. **Tạo yêu cầu hoàn tiền (Refund):**
   - Automatically routes transaction to the Two-Person Refund Queue (`/admin/refunds`).
3. **Bỏ qua (Dismiss):**
   - Marks transaction as `Dismissed` with an operational audit note (e.g., test transaction or resolved externally).

---

## 5. Two-Person Refund Workflow (`BR-15`)

To prevent internal fraud, **GroceryHub enforces the Two-Person Rule** both at the API layer and via a database CHECK constraint:
$$\text{DecidedBy} \neq \text{RequestedBy}$$

### Workflow Steps:
```
[Staff A: Request] -> [Manager B: Approve] -> [Manager B: Bank Transfer] -> [Complete]
```

1. **Step 1: Request Refund (`POST /api/v1/admin/refunds`)**
   - User: Any Staff or Manager with `refunds.request` permission.
   - Enters: Order ID, Amount (must be $\le \text{Paid} - \text{Refunded}$), and Reason.
   - Status: `Requested`.
2. **Step 2: Approval (`POST /api/v1/admin/refunds/{id}/approve`)**
   - User: A *different* Manager or Admin with `refunds.approve` permission.
   - **Enforcement:** If Staff A attempts to approve their own refund request, the system rejects with `409 Conflict (REFUND_SAME_APPROVER)`.
   - Status: `Approved`.
3. **Step 3: Manual Bank Payout**
   - Finance Manager transfers the refund via business banking portal.
   - Obtains the Bank Transfer Reference (e.g. `FT26319000111`).
4. **Step 4: Complete Refund (`POST /api/v1/admin/refunds/{id}/complete`)**
   - User enters `bankReference` and optional notes.
   - Status: `Completed`.
   - Underlying `Payment.Status` updates to `Refunded` (or `PartiallyRefunded`).

---

## 6. Daily Reconciliation & Health Checks

The background service `ReconciliationService` runs every **15 minutes** (configurable via `SePay:Reconciliation:IntervalMinutes`).

### How It Works:
1. Queries SePay API (`GET /v2/transactions/list`) with Bearer token.
2. Compares transactions against local `payments.PaymentTransactions`.
3. If an incoming transaction exists in SePay but was missed due to webhook delivery outage:
   - System ingests the transaction with `Source = "Reconciliation"`.
   - Evaluates matching logic and confirms order or flags for review.
4. If transaction was already processed by webhook:
   - Skipped without creating duplicates.

### Daily Verification Checklist:
- [ ] Check `/health/ready` endpoint: ensure `sepay` dependency is Healthy.
- [ ] Review Unresolved Anomalies: check `/admin/payments?tab=review` count is zero or being handled.
- [ ] Review Pending Refunds: ensure refunds in `Requested` or `Approved` state are completed within 24 hours.

---

## 7. Emergency Nginx IP Allowlist & DoS Protection

In production environments, `deploy/nginx/sepay-webhook.conf` restricts webhook traffic to SePay egress IP addresses:
- `172.236.138.20`
- `172.233.83.68`
- `171.244.35.2`
- `151.158.108.68`
- `151.158.109.79`
- `103.255.238.139`
- `45.57.137.67`
- `2400:8905::2000:8cff:fe98:45cd`
- `2600:3c15::2000:8aff:fedd:874b`

If SePay adds new egress IPs, update `deploy/nginx/sepay-webhook.conf` and reload Nginx:
```bash
nginx -t && nginx -s reload
```
Additionally, `client_max_body_size 64k;` strictly prevents body flooding.
