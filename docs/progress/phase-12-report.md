# GroceryHub — Phase 12 Completion Report: Docker & Deployment

**Document ID:** `PHASE-12-REPORT`  
**Phase:** 12 — Docker & Deployment  
**Author:** `project-manager` (in coordination with `devops-engineer`, `security-reviewer`, `database-engineer`, and `qa-engineer`)  
**Date:** 2026-11-16  
**Status:** **GO (Passed all Phase 12 Acceptance Criteria)**

---

## 1. Executive Summary

Phase 12 delivers a reproducible, hardened, and verified production container deployment stack for GroceryHub. The implementation satisfies requirements `NFR-004` (graceful shutdown, health monitoring, restart policies, restore drill), `NFR-005` (structured logging), `NFR-009` (CI/CD pipeline, dependency auditing), `BR-14` (RBAC security), and `BR-15` (payment webhook integrity).

All container images run as non-root unprivileged users, enforce `no-new-privileges`, set memory ceilings, and leave zero database ports exposed to the public internet. The database backup and restoration drills were executed with 100% data integrity verified. Operational runbooks in Vietnamese and English provide end-to-end instructions for the user's chosen hosting environment.

---

## 2. Confirmed Project Decisions (`DEC-018`)

In consultation with the user, all key production parameters were established and logged in `docs/progress/DECISIONS.md`:
1. **Hosting Target:** **Managed Container Platform** (Azure Container Apps / AWS ECS / GCP Cloud Run) for production, with Docker Compose maintained for local development, staging, and portable on-premise deployments.
2. **Production Domain:** `grocery.dangbinhduong.io.vn`. Nginx reverse proxy configured with SPA fallback, `/api` proxy, strict CSP (`img-src 'self' data: https://qr.sepay.vn`), and HSTS.
3. **Database Engine:** **Azure SQL Database / AWS RDS for SQL Server** (Cloud Managed Database) for production deployment, ensuring strict compliance with Microsoft licensing (`Developer` edition strictly non-production; `Express` for local development/test fallback).
4. **SMTP Service:** Mailpit for local Dev/Staging/Demo + standard production SMTP configuration (SendGrid, Resend, Brevo, AWS SES) for transactional customer notifications.

---

## 3. Deliverables Inventory

### 3.1. Containerization & Reverse Proxy
- `Dockerfile.api`: Multi-stage build (.NET 10 noble SDK $\rightarrow$ aspnet:10.0 noble runtime). Non-root user `app` (UID 1654), health check `/health/live`, separate `migrator` target for one-off bundle execution.
- `web/Dockerfile` & `Dockerfile.web`: Multi-stage build (Node 22 alpine $\rightarrow$ `nginxinc/nginx-unprivileged:1.27-alpine`). Non-root user `nginx` (UID 101), SPA fallback, security headers, unprivileged port 8080.
- `web/nginx/default.conf`: Hardened reverse proxy configuration:
  - Strict Content-Security-Policy with `img-src 'self' data: https://qr.sepay.vn`.
  - Rate-limited `/api/` (60r/s) and `/api/v1/payments/sepay/webhook` (10r/m, burst 5).
  - SePay official dispatcher IP allowlist (`172.236.138.20/32`, `172.233.83.68/32`, `171.244.35.2/32`, etc.).
  - Immutable asset caching (1 year) for `/assets/`, `no-cache` for `index.html`.

### 3.2. Docker Compose
- `docker-compose.yml`: Production base configuration. Internal bridge network `groceryhub_net`. **Only `web` publishes host port 8080**. SQL Server (1433) and API (8080) are strictly internal. Migrator executes to completion (`service_completed_successfully`) before API boots.
- `docker-compose.override.yml`: Local development overrides exposing port 1433, port 5000 (direct API swagger), and Mailpit (1025/8025).
- `.env.example`: Production-ready template with all required keys, placeholders, and security guidance.

### 3.3. CI/CD & Automation
- `.github/workflows/ci.yml`: Comprehensive pull request & push verification:
  - Secret scan (`gitleaks`) and `verify-reality`.
  - Backend .NET 10 build (`-warnaserror`), vulnerable package check, and test suite.
  - Frontend Node 22 clean install, Vitest unit tests, Vite build, and npm audit.
  - Docker image build validation for API and Web images.
- `.github/workflows/release.yml`: Automated build, tagging, GHCR publication (`ghcr.io/.../api`, `web`), and Aqua Security Trivy vulnerability scan upon tag push (`v*.*.*`).
- `.github/dependabot.yml`: Weekly automated security dependency tracking for NuGet, npm, Docker, and GitHub Actions.

### 3.4. Operational Scripts & Tools
- `scripts/backup-db.ps1` & `scripts/backup-db.sh`: Database backup scripts with automatic Express/Standard edition detection, checksum verification, and SHA-256 fingerprinting.
- `scripts/restore-drill.ps1` & `scripts/restore-drill.sh`: Automated restore rehearsals verifying table matching, checksum integrity, and `DBCC CHECKDB`.
- `scripts/smoke-test.ps1` & `scripts/smoke-test.sh`: 7-point post-deployment smoke test suite.
- `scripts/verify-ports.ps1`: Automated audit verifying zero external exposure of database and API ports in production compose.
- `scripts/rotate-secrets.md`: Operational secret rotation guide (JWT key, SePay HMAC secret with zero-downtime dual-key window, database credentials).
- `scripts/sql/01-create-app-login.sql`: Least-privilege SQL application login and user creation.
- `scripts/sql/02-verify-rcsi.sql`: READ_COMMITTED_SNAPSHOT verification and enablement.

### 3.5. Documentation & Reports
- `docs/ops/deployment-guide.md`: Numbered steps in **both Vietnamese and English** for Cloud Container Platform (Azure Container Apps / AWS ECS) and Linux VPS Docker Compose.
- `docs/ops/runbook.md`: Operations and systems runbook (health monitoring, log parsing, maintenance, scaling).
- `docs/ops/backup-restore.md`: Disaster recovery runbook (RPO $\le 1$h, RTO $\le 15$m, retention policies).
- `docs/ops/incident-checklist.md`: Incident severity matrix, triage checklist, and rollback decision tree.
- `docs/ops/payments-runbook.md`: SePay payment operations, reconciliation, and refund handling.
- `docs/security/review-phase-12.md`: Independent security review (0 Critical, 0 High, 0 Medium open).

---

## 4. Acceptance Criteria Verification

| Acceptance Criterion | Verification Method | Evidence / Output | Status |
|---|---|---|---|
| **Clean Docker Compose Config** | Inspected `docker-compose.yml` and `override.yml`. Verified environment parsing and service graph. | `docker-compose.yml` | **PASS** |
| **Port Isolation** | `scripts/verify-ports.ps1` executed. Verified SQL Server port 1433 and API port 8080 have zero host exposure in production compose. | `docs/evidence/phase-12/port-exposure-audit.txt` | **PASS** |
| **Non-Root & Hardened Containers** | Verified `USER app` (1654) in API and `USER nginx` (101) in Web. `no-new-privileges` on all services. | `Dockerfile.api`, `web/Dockerfile`, `docs/security/review-phase-12.md` | **PASS** |
| **Pinned Image Tags** | Verified all base images use explicit tags (`10.0-noble`, `2022-CU16`, `node:22-alpine`, `nginx:1.27-alpine`). Zero `latest` tags. | `docker-compose.yml`, Dockerfiles | **PASS** |
| **Backup & Restore Drill** | `scripts/restore-drill.ps1` executed on active database. Verified 5 migrations, row counts match 100%, `DBCC CHECKDB` 0 errors. | `docs/evidence/phase-12/restore-drill.txt` | **PASS** |
| **CI Failure Simulation** | Created deliberate test failure. Verified test runner halts with code 1 and captures failure stack trace. | `docs/evidence/phase-12/ci-failing-test-simulation.txt` | **PASS** |
| **Zero Package Vulnerabilities** | `dotnet list package --vulnerable` and `npm audit --omit=dev` executed across all projects. | `0 vulnerable packages` | **PASS** |
| **Gitleaks & Reality Clean** | `scripts/verify-reality.ps1` executed. Zero secret leaks or commercial license violations. | `verify-reality: no violations` | **PASS** |
| **Full Test Suite Integrity** | Executed 455 .NET tests (unit, arch, integration) + 18 Web tests = 473 tests total. | **473 Passed, 0 Failed, 0 Skipped** | **PASS** |

---

## 5. Gate Recommendation

Phase 12 meets all architectural, functional, security, and operational criteria set forth in `docs/spec/13-DEPLOYMENT-STRATEGY.md` and `docs/phase-prompts/phase-12-deployment.md`.

**Phase 12 Gate Decision:** **GO**  
**Next Phase:** Phase 13 — Final Review and Handover (`docs/phase-prompts/phase-13-final-review.md`).
