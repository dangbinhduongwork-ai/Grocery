# GroceryHub — Security Review & Container Hardening Report (Phase 12)

**Phase:** Phase 12 — Docker & Deployment Hardening  
**Date:** 2026-11-16  
**Auditor:** `security-reviewer` (GroceyHub Architecture & Security)  
**Status:** **PASSED — ZERO OPEN FINDINGS (0 Critical, 0 High, 0 Medium)**

---

## 1. Executive Summary

This security review audits the containerization, reverse proxy configuration, operational scripts, CI/CD pipeline, and deployment hardening of the GroceryHub platform against OWASP ASVS Level 2 and the CIS Docker Benchmark.

All container images use pinned upstream versions, run as unprivileged non-root users (`app:1654`, `nginx:101`), enforce `no-new-privileges`, set explicit memory limits, and avoid baking secrets into layers. The production Docker Compose exposes **only** the unprivileged Nginx web gateway port (8080), leaving SQL Server and the backend API on an isolated internal bridge network (`groceryhub_net`).

---

## 2. Container & Host Hardening Matrix

| Security Control | Implementation | Verification / Status |
|---|---|---|
| **Non-Root Execution** | `Dockerfile.api`: `USER app` (UID 1654)<br>`web/Dockerfile`: `nginxinc/nginx-unprivileged:1.27-alpine` (`USER nginx`, UID 101) | **PASS**: Neither container runs as `root`. |
| **No New Privileges** | `security_opt: [no-new-privileges:true]` on all services (`sqlserver`, `migrator`, `api`, `web`) | **PASS**: Prevents privilege escalation via setuid binaries. |
| **Pinned Image Tags** | All base images pinned to specific LTS releases (`.NET 10 noble`, `Node 22 alpine`, `Nginx 1.27 alpine`, `MSSQL 2022-CU16`) | **PASS**: Zero `latest` tags used in production configurations. |
| **Resource Limits** | Explicit memory limits (`sqlserver`: 2g, `api`: 1g, `web`: 512m) | **PASS**: Prevents container memory exhaustion attacks (DoS). |
| **Port Isolation** | `docker-compose.yml` publishes only `web` port. Database (1433) and API (8080) are strictly internal. | **PASS**: Verified via `scripts/verify-ports.ps1`. |
| **Read-Only / Isolation** | Only specific named volumes are mounted (`sqlserver_data`, `product_images`). | **PASS**: No host filesystem mounts in production compose. |

---

## 3. Reverse Proxy & Edge Defense Verification

The Nginx gateway configuration (`web/nginx/default.conf`) was reviewed and verified for defense-in-depth:

### 3.1 Content-Security-Policy (CSP)
```nginx
add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https://qr.sepay.vn; font-src 'self' data:; connect-src 'self'; frame-ancestors 'none';" always;
```
- **Image Sources:** Strictly limited to `'self'`, `data:`, and `https://qr.sepay.vn` (SePay VietQR dynamic payment codes). Any external image injection is blocked by the browser.
- **Framing:** `frame-ancestors 'none'` and `X-Frame-Options: DENY` prevent clickjacking.
- **MIME Sniffing:** `X-Content-Type-Options: nosniff` active across all routes and assets.

### 3.2 SePay Webhook Path Hardening (`/api/v1/payments/sepay/webhook`)
- **Body Size Limit:** `client_max_body_size 64k;` (mitigates memory exhaustion and large payload DoS).
- **Rate Limiting:** Dedicated zone `zone=webhook_limit:10m rate=10r/m;` with `burst=5 nodelay;`.
- **IP Allowlist:** Configured with SePay's official dispatcher IP ranges (`https://developer.sepay.vn/vi/dia-chi-ip`):
  - IPv4: `172.236.138.20/32`, `172.233.83.68/32`, `171.244.35.2/32`, `151.158.108.68/32`, `151.158.109.79/32`, `103.255.238.139/32`, `45.57.137.67/32`.
  - IPv6: `2400:8905::2000:8cff:fe98:45cd/128`, `2600:3c15::2000:8aff:fedd:874b/128`.
  - Private CIDRs allowed for internal gateway routing and local development simulations.

---

## 4. Automated Security Scans & Compliance

```
1. NuGet Vulnerability Audit:
   Command: dotnet list package --vulnerable --include-transitive
   Result:  0 vulnerable packages across all 18 projects.

2. npm Vulnerability Audit:
   Command: npm audit --omit=dev
   Result:  0 vulnerabilities found (web frontend).

3. Gitleaks Secret Scan:
   Command: gitleaks detect --no-banner
   Result:  0 leaks detected. All secrets use .env.example templates.

4. Reality Verification:
   Command: scripts/verify-reality.ps1
   Result:  0 violations.
```

---

## 5. Security Sign-Off

The container configuration, deployment architecture, and operational scripts satisfy all security requirements (`NFR-003`, `NFR-004`, `NFR-009`, `BR-14`, `BR-15`).

**Recommendation:** **GO** for Phase 12 Deployment completion.
