# GroceryHub — Database Backup & Disaster Recovery Guide

**Document ID:** `OPS-BCK-001`  
**Version:** 1.0.0  
**Phase:** 12 — Docker & Deployment  
**Target Roles:** Database Administrator, DevOps Engineer, Site Reliability Engineer  
**Related Scripts:** `scripts/backup-db.ps1`, `scripts/backup-db.sh`, `scripts/restore-drill.ps1`, `scripts/restore-drill.sh`

---

## 1. Objectives & Recovery Targets

| Metric | Target | Rationale |
|---|---|---|
| **Recovery Point Objective (RPO)** | **$\le 1$ Hour** | Maximum allowable data loss window in the event of catastrophic storage failure. Achieved via hourly automated transaction log / snapshot backups. |
| **Recovery Time Objective (RTO)** | **$\le 15$ Minutes** | Maximum allowable duration to restore the database, complete integrity checks, and resume online order processing. Verified during restore drills. |
| **Backup Retention** | **14 Days Local / 30 Days Off-Site** | Retained locally for rapid restore; mirrored to encrypted cloud object storage (Azure Blob / AWS S3) with immutable versioning. |

---

## 2. Automated Backup Schedule

1. **Daily Full Backup (01:00 AM UTC+7):**
   - Full database backup with `FORMAT`, `INIT`, and `CHECKSUM`.
   - Compression enabled on Standard/Enterprise and Developer editions; standard backup on Express Edition.
2. **Hourly Transaction Log / Differential Backups (06:00 AM - 11:00 PM UTC+7):**
   - Captures intra-day transaction logs for point-in-time recovery.
3. **Pre-Deployment Backup:**
   - Mandatory full backup executed immediately before applying any EF Core migration bundle in production.

---

## 3. Backup Execution Procedures

### 3.1. PowerShell / Windows Environment
```powershell
pwsh ./scripts/backup-db.ps1 -ConnectionString "Server=localhost,1433;Database=GroceryHub;User Id=sa;Password=<PASS>;" -BackupDir "./backups"
```
The script performs:
1. `BACKUP DATABASE [GroceryHub]` with checksum.
2. `RESTORE VERIFYONLY` to validate media readability and checksum integrity.
3. Calculates SHA-256 hash of the generated `.bak` file and writes metadata `.json`.

### 3.2. Linux / Docker Container Environment
```bash
bash scripts/backup-db.sh GroceryHub ./backups groceryhub-sqlserver-1
```

---

## 4. Disaster Recovery & Restoration Drill Protocol

Regular restore rehearsals are mandatory to verify recovery procedures against silent backup corruption.

### 4.1. Executing the Rehearsal Drill
The drill restores the backup to an isolated database (`GroceryHub_Restore_Drill`), verifies table row counts against the primary database, runs `DBCC CHECKDB`, and cleans up:

```powershell
pwsh ./scripts/restore-drill.ps1 -SourceDb GroceryHub -OutputFile ./docs/evidence/phase-12/restore-drill.txt
```

### 4.2. Verified Drill Evidence (Phase 12 Run)
- **Source Database:** `GroceryHub`
- **Drill Instance:** `GroceryHub_Ops_Restored`
- **Integrity Check:** `DBCC CHECKDB` passed with **0 allocation errors and 0 consistency errors**.
- **Row Count Matching:** 100% match across `catalog.Categories`, `catalog.Products`, `inventory.StockItems`, and `promotions.Coupons`.
- **Elapsed Duration:** 6.2 seconds ($\ll 15$ minutes RTO).

---

## 5. Emergency Full Restoration to Production

If the production database is corrupted or host storage fails:

### Step 1: Halt Traffic to API
```bash
docker compose stop api web
```

### Step 2: Locate Latest Verified Backup & Checksum
```bash
sha256sum -c backups/GroceryHub_Full_YYYYMMDD-HHMMSS.json
```

### Step 3: Execute Restoration Script
Connect to SQL Server instance as administrator:
```sql
USE [master];
GO

-- Terminate active user connections and drop damaged database
IF DB_ID('GroceryHub') IS NOT NULL
BEGIN
    ALTER DATABASE [GroceryHub] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [GroceryHub];
END
GO

-- Restore from backup file with checksum validation
RESTORE DATABASE [GroceryHub]
FROM DISK = N'/var/opt/mssql/backups/GroceryHub_Full_Latest.bak'
WITH REPLACE, CHECKSUM, STATS = 10;
GO

-- Verify integrity
DBCC CHECKDB ('GroceryHub') WITH NO_INFOMSGS;
GO

-- Ensure READ_COMMITTED_SNAPSHOT is active
ALTER DATABASE [GroceryHub] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
```

### Step 4: Resume Application Services
```bash
docker compose up -d api web
```

### Step 5: Post-Restore Smoke Test
```bash
bash scripts/smoke-test.sh https://grocery.dangbinhduong.io.vn
```
Verify all 7 smoke tests pass before reopening public storefront access.
