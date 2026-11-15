---
name: wf-security-review
description: Reusable procedure (invoke with /wf-security-review) for a phase-gate security review of GroceryHub - RBAC matrix tests with real requests, webhook negative tests, scans, config review, findings with severity and retest. Use at the end of phases 5, 7, 10, 11, 12 and before any release.
---

# /wf-security-review

Inputs: phase id, running stack, endpoint list from OpenAPI.

1. **Inventory**: export endpoints from `docs/contracts/openapi.json` with their auth requirements. Any endpoint without an explicit policy = finding.
2. **RBAC matrix**: for each endpoint call with (no token, Customer A, Customer B, Staff, Manager, Admin). Save table of expected vs actual status codes in `docs/security/rbac-test-<phase>.md`. Include IDOR attempts (Customer B on Customer A's resources).
3. **Auth hardening**: login brute-force lockout/rate limit; refresh rotation + reuse detection; logout revocation; password reset non-enumeration.
4. **Payments** (when present): send webhooks with valid signature, tampered body, wrong secret, old timestamp, missing headers, duplicate id (x2 concurrent), wrong amount, wrong account, unknown code, `out` type, late arrival after expiry. Record DB state before/after.
5. **Uploads/inputs**: oversized files, wrong MIME, polyglot images, script in review text, SQL-ish strings, very long strings, negative quantities/amounts.
6. **Scans**: vulnerable packages, `npm audit`, gitleaks, image scan; attach output.
7. **Config review**: headers/CSP/CORS, cookies, `.env.example` vs real secrets, compose network exposure, non-root containers.
8. **Report** `docs/security/review-<phase>.md` with findings (id, severity, evidence, fix, status). Gate rule: no open Critical/High.
9. **Retest** fixes from a clean build and update status with evidence.
