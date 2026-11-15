---
name: security-review
description: Security checklist and procedure for GroceryHub - authentication, JWT/refresh tokens, RBAC and object-level access, input validation, file upload, payment webhook integrity, secrets, headers/CSP/CORS, Docker hardening, dependency and supply-chain checks. Use before merging auth/payment/upload code, at every phase gate, and when asked to review for security.
---

# security-review

## Goal
Find exploitable defects early, with evidence. Baseline: OWASP ASVS L2 and OWASP API Security Top 10 (2023).

## Process
1. Build the asset/actor map: Guest, Customer, Staff, Manager, Admin; assets: accounts, orders, addresses, payments, stock, prices/cost, audit logs.
2. For each endpoint in spec 07, test with: no token, wrong role, right role, other customer's token. Record status codes in `docs/security/rbac-test-<phase>.md` (real requests).
3. Run the automated scans (below) and attach output.
4. Review code paths listed in the checklist; open a CR note per finding with severity and reproduction.
5. Retest fixes from a fresh build.

## Automated scans
- `dotnet list package --vulnerable --include-transitive`, `npm audit --omit=dev`
- `gitleaks detect --no-banner` (or equivalent configured in `.gitleaks.toml`)
- Container image scan (Trivy or equivalent) on built images
- Optional dynamic scan against a local stack (OWASP ZAP baseline) - never against third-party systems

## Checklist
**AuthN/AuthZ**
- [ ] Every endpoint has an explicit policy or documented `AllowAnonymous`
- [ ] IDOR tests for carts, addresses, orders, payments, reviews
- [ ] Refresh token rotation + reuse detection; logout revokes; tokens hashed at rest
- [ ] Lockout + rate limits on login/register/reset/checkout; no user enumeration
- [ ] Staff/Manager/Admin privilege boundaries (e.g. cost visibility, refund approval by different person)
**Input/Output**
- [ ] Server-side validation for all DTOs; max sizes; pagination caps
- [ ] No SQL concatenation; no raw HTML rendering of user content (reviews) - sanitize/escape
- [ ] ProblemDetails hides internals; no stack traces
**Payments**
- [ ] Webhook auth enforced outside Development; raw-body HMAC; replay window; constant-time compare
- [ ] Idempotent processing; amount/account/code validated; anomalies queued
- [ ] No client-side paid path; dev simulator not reachable in Production
**Uploads**
- [ ] MIME + magic bytes + size limits; re-encoded; random names; not executable; served with safe headers
**Config/Infra**
- [ ] No secrets in repo/images/logs; `.env` ignored; separate keys per environment
- [ ] HTTPS/HSTS, CSP (img-src allows only self + qr.sepay.vn), CORS allowlist, secure cookies
- [ ] Containers non-root, pinned tags, SQL Server not exposed publicly
**Supply chain**
- [ ] Lockfiles committed; licenses checked; no abandoned/commercial surprise packages

## Common mistakes
- Trusting role claims from the frontend; forgetting object-level checks.
- Using `[AllowAnonymous]` on a controller by accident; CORS `*` with credentials.
- Logging tokens or full webhook payloads at Information level.

## Official documentation
- https://owasp.org/www-project-application-security-verification-standard/
- https://owasp.org/API-Security/
- https://cheatsheetseries.owasp.org/
- https://learn.microsoft.com/aspnet/core/security/
