# GroceryHub — Operational Secrets Rotation Runbook

This document details the exact, step-by-step procedures for rotating all cryptographic keys, credentials, and sensitive tokens in GroceryHub without causing customer disruption or unplanned downtime.

---

## 1. Secrets Inventory & Risk Matrix

| Secret Name | Configuration Key / Location | Default Lifecycle | Impact of Compromise | Downtime on Rotation |
|---|---|---|---|---|
| **JWT Signing Key** | `Jwt__SigningKey` (`.env` / Docker Secret) | 90 days | Critical: Attacker can forge access tokens | Zero downtime (if dual-key window used) or forced re-login |
| **SePay Webhook HMAC Secret** | `SePay__Webhook__HmacSecret` | 180 days | High: Attacker can spoof payment webhooks | **Zero downtime** (natively supported via `HmacSecretPrevious`) |
| **SePay User API Token** | `SePay__Api__Token` | 180 days | Medium: Inability to poll transactions for reconciliation | Zero downtime |
| **SQL Database Password** | `MSSQL_SA_PASSWORD` / `ConnectionStrings__DefaultConnection` | 90 days | Critical: Full database access | Rolling restart (< 10 seconds) |
| **Initial Admin Password** | `Seed__AdminPassword` | First deployment | High: Administrative access | Immediate password change in UI |

---

## 2. Rotation Procedure: SePay Webhook HMAC Secret (Zero Downtime)

GroceryHub's SePay webhook authentication service natively supports an overlapping transition window via `SePay__Webhook__HmacSecretPrevious`.

### Step 1: Generate a new HMAC Secret
Generate a new high-entropy secret (at least 32 random characters / 256 bits):
```bash
openssl rand -hex 32
# Example output: a7f8e3b2c91d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f
```

### Step 2: Configure Dual Secret Overlap in Environment
Edit `.env` (or update secret in Azure Container Apps / AWS Parameter Store):
```bash
# Shift the current active secret to the previous slot
SePay__Webhook__HmacSecretPrevious="<CURRENT_HMAC_SECRET>"

# Insert the newly generated secret as the active secret
SePay__Webhook__HmacSecret="<NEWLY_GENERATED_HMAC_SECRET>"
```

### Step 3: Deploy Configuration
```bash
# For Docker Compose
docker compose up -d api

# For Azure Container Apps
az containerapp update --name groceryhub-api --resource-group groceryhub-rg \
  --set-env-vars "SePay__Webhook__HmacSecret=<NEW>" "SePay__Webhook__HmacSecretPrevious=<OLD>"
```
At this point, GroceryHub will accept webhooks signed with **either** the new or the old secret.

### Step 4: Update Webhook Secret in SePay Dashboard
1. Log in to [my.sepay.vn](https://my.sepay.vn) -> **Cấu hình Webhook** (Webhook Configuration).
2. Enter the new secret into the **Webhook Secret** field.
3. Click **Lưu cấu hình** (Save configuration) and send a test webhook.
4. Verify HTTP 200 response in SePay logs and GroceryHub application logs.

### Step 5: Decommission Old Secret
After 24 hours of stable operation, remove `SePay__Webhook__HmacSecretPrevious` from `.env` and redeploy.

---

## 3. Rotation Procedure: JWT Access Token Signing Key

The JWT signing key (`Jwt__SigningKey`) signs short-lived (15-minute) customer and staff access tokens.

### Step 1: Generate a new Signing Key
```bash
openssl rand -base64 48
```

### Step 2: Update Configuration
Update `Jwt__SigningKey` in `.env` or container environment.

### Step 3: Rolling Restart
```bash
docker compose up -d api
```

### Client Impact & Recovery:
- Because access tokens expire in **15 minutes**, existing access tokens will become invalid upon restart.
- Customers and staff will **not** be logged out if their refresh token is valid: the frontend TanStack Query client automatically calls `/api/v1/auth/refresh` using the secure `gh_refresh` cookie, seamlessly acquiring a new access token signed with the new key.
- If an immediate revocation of all sessions is required (security incident), truncate the `identity.RefreshTokens` table:
```sql
TRUNCATE TABLE [identity].[RefreshTokens];
```

---

## 4. Rotation Procedure: SQL Server Password & App Login

### Step 1: Generate new password for Application Login
```bash
openssl rand -base64 24
```

### Step 2: Update SQL Server Login inside Database
Connect via `sqlcmd` or SSMS as administrator:
```sql
USE [master];
GO
ALTER LOGIN [groceryhub_app] WITH PASSWORD = N'<NEW_STRONG_PASSWORD>';
GO
```

### Step 3: Update Connection String in Environment
Update `.env`:
```bash
ConnectionStrings__DefaultConnection="Server=sqlserver,1433;Database=GroceryHub;User Id=groceryhub_app;Password=<NEW_STRONG_PASSWORD>;TrustServerCertificate=True;Encrypt=True"
```

### Step 4: Restart API Service
```bash
docker compose up -d api
```

### Step 5: Verify Connectivity
```bash
curl -f http://localhost:8080/health/ready
```

---

## 5. Rotation Checklist & Audit
- [ ] New secret generated using cryptographically secure RNG (`openssl rand` / `crypto.randomBytes`).
- [ ] No secrets committed to git or printed in plaintext CI logs.
- [ ] Application health check `/health/ready` returns `Healthy`.
- [ ] Record rotation event in `docs/ops/incident-checklist.md` or audit log with timestamp, operator name, and rotated key identifier (never the key value itself).
