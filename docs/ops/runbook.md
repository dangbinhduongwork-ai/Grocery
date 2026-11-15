# GroceryHub — Operations & Systems Runbook

**Document ID:** `OPS-SYS-001`  
**Version:** 1.0.0  
**Phase:** 12 — Docker & Deployment  
**Target Roles:** System Administrator, DevOps Engineer, Technical Support  
**Domain:** `grocery.dangbinhduong.io.vn`

---

## 1. Routine Service Monitoring

### 1.1. Health Endpoints
GroceryHub exposes standard ASP.NET Core health checks:
- **Liveness Probe:** `GET /health/live`
  - Returns `200 OK` (`"Healthy"`) if the process is responsive and event loops are functional.
  - Frequency: Every 15 seconds. Timeout: 5 seconds.
  - Container action on failure: Container runtime automatically restarts the container.
- **Readiness Probe:** `GET /health/ready`
  - Returns `200 OK` (`"Healthy"`) if the database connection pool is healthy and database queries succeed.
  - Returns `503 Service Unavailable` (`"Unhealthy"`) if SQL Server is unreachable.
  - Gateway action on failure: Load balancer halts routing traffic to this instance.

### 1.2. Automated Health Check Command
```bash
curl -i https://grocery.dangbinhduong.io.vn/health/ready
```

---

## 2. Container Log Management & Inspection

### 2.1. Inspecting Logs in Docker Compose
```bash
# Follow logs across all services in real time
docker compose logs -f --tail=100

# View only API error logs
docker compose logs api --tail=200 | grep -E "Error|Fatal|fail"

# View Nginx access & security blocks
docker compose logs web --tail=100
```

### 2.2. Structured JSON Log Format
GroceryHub outputs compact Serilog JSON logs. Each event contains:
- `@t`: UTC Timestamp
- `@l`: Log Level (`Information`, `Warning`, `Error`, `Fatal`)
- `@mt`: Message Template
- `SourceContext`: Emitting class or module namespace
- `traceId`: W3C distributed trace identifier for end-to-end correlation

Example error search using `jq`:
```bash
docker compose logs api --no-log-prefix | jq 'select(.@l == "Error" or .@l == "Fatal")'
```

---

## 3. Database Maintenance & Performance Tuning

### 3.1. Rebuilding Indexes & Updating Statistics
Run monthly maintenance during off-peak hours (02:00–04:00 AM UTC+7):
```sql
USE [GroceryHub];
GO

-- Reorganize indexes with fragmentation > 10% and rebuild > 30%
EXEC sp_MSforeachtable @command1="ALTER INDEX ALL ON ? REBUILD WITH (ONLINE = ON);";
GO

-- Update database statistics with full scan
EXEC sp_updatestats;
GO
```

### 3.2. Monitoring Connection Pool Utilization
If API logs indicate connection pool timeouts (`System.InvalidOperationException: Timeout expired`):
1. Check active connections in SQL Server:
   ```sql
   SELECT DB_NAME(dbid) AS DbName, COUNT(dbid) AS NumberOfConnections, loginame 
   FROM sys.sysprocesses 
   WHERE dbid = DB_ID('GroceryHub') 
   GROUP BY dbid, loginame;
   ```
2. Adjust `Max Pool Size` in connection string if necessary (default is 100).

---

## 4. SePay Payment Operations & Daily Reconciliation

1. **Daily Bank Reconciliation:**
   - The background worker executes daily at 00:30 UTC+7 to compare all bank inflows against `payments.PaymentTransactions`.
   - Any transaction marked `NeedsReview` must be resolved by 10:00 AM by Operations Staff using the Review Queue (`/admin/payments?tab=review`).
2. **Review Queue Threshold Alert:**
   - Alert fires if `NeedsReview` transaction count exceeds **10** items.
   - Refer to `docs/ops/payments-runbook.md` §4 for the resolution protocol (Attach to Order / Refund / Dismiss).
3. **Webhook Latency / Error Rate:**
   - Alert fires if webhook processing duration exceeds 500 ms or error rate exceeds 1%.

---

## 5. Storage & Image Upload Management

Uploaded product images reside in `Storage__ImagesPath` (`/app/var/product-images` or cloud mounted volume).
- Disk space alert threshold: **80% capacity**.
- Verify volume disk utilization:
  ```bash
  docker compose exec api df -h /app/var/product-images
  ```
- Orphaned image cleanup job:
  ```bash
  # Delete temporary uploaded files older than 7 days that were never attached to a product
  find /var/lib/docker/volumes/groceryhub_product_images/_data -type f -mtime +7 -delete
  ```

---

## 6. Restart & Graceful Shutdown Procedures

1. **Graceful Rolling Restart:**
   ```bash
   docker compose restart api
   ```
   ASP.NET Core allows a 30-second graceful shutdown window (`HostOptions.ShutdownTimeout`), allowing in-flight HTTP transactions and database writes to complete before process termination.

2. **Full Stack Cold Restart:**
   ```bash
   docker compose down
   docker compose up -d
   ```
