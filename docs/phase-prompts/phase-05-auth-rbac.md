# Phase 05 — Authentication and RBAC

**Goal:** Secure identity: register/verify/login/refresh/logout/reset, JWT, rotating refresh tokens, lockout, permission-based authorization with Staff/Manager/Admin management endpoints and seeded roles/permissions.
**Agents:** backend-developer ‖ database-engineer → qa-engineer → security-reviewer (mandatory); PM.
**Inputs:** specs 05 §7–8, 07 §2/§4 (auth), 06 identity schema, ADR-003, FRs FR-CUS-006, FR-CUS-007, FR-ADM-009, FR-ADM-010, NFR-003.
**Outputs:** Identity module, identity migrations/seeders, policies, rate limits, tests, `docs/security/rbac-test-phase-05.md`, `docs/security/review-phase-05.md`.
**Dependencies:** Phase 04 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 05 — AUTH & RBAC. Requirements: FR-CUS-006, FR-CUS-007, FR-ADM-009, FR-ADM-010, NFR-003, BR-14. Follow docs/spec/05 §7-8, docs/spec/07 §2, §4(Auth), §5(Staff, Roles) and ADR-003. Load skills dotnet-api, efcore-sqlserver, security-review.

Delegate:
1. database-engineer: identity schema (ASP.NET Core Identity tables customised per doc 06: Users with FullName/Status/DeletedAt, Roles, UserRoles, Permissions, RolePermissions, UserPermissions, RefreshTokens (hashed), StaffProfiles), migration, idempotent seeders for roles (Customer, Staff, Manager, Admin), permissions (all codes in doc 07 §2) and role-permission matrix (docs/spec/permissions-seed.md). Admin bootstrap from env Seed__AdminEmail / Seed__AdminPassword: created only if no Admin exists; password never logged; force-change flag.
2. backend-developer: Identity module: register (email normalisation, password policy: min 10 chars, no common passwords check optional), email verification (token link via SMTP; Mailpit in dev), login (lockout after 5 failures/15 min, generic errors), JWT access token 15 min (sub, roles, jti), refresh token rotation with family + reuse detection, httpOnly Secure SameSite=Strict cookie scoped to /api/v1/auth, `X-Requested-With` check on refresh, logout, forgot/reset (non-enumerating), change password (revokes other sessions), GET /auth/me (roles + effective permissions), dynamic permission policy provider + cached IPermissionService (TTL <= 60 s, invalidate on change), CurrentUser abstraction, owner-check helpers; staff endpoints (CRUD staff, assign roles, per-user permissions), role/permission endpoints; audit hooks for role/permission changes (Audit module minimal AuditLog writer now). Rate limiter policies applied. Update OpenAPI.
3. qa-engineer: integration tests: full register->verify->login->refresh->logout; wrong password lockout; refresh reuse detection revokes family; expired/invalid JWT; password reset flow with Mailpit; permission cache invalidation; an endpoint x role matrix test generator that reads OpenAPI + a policy catalogue and asserts allowed/denied statuses (anon, Customer A, Customer B, Staff, Manager, Admin) for all implemented endpoints so far; default-deny architecture test (endpoint without policy fails build).
4. security-reviewer: run /wf-security-review for this phase (RBAC matrix with real requests, brute-force, refresh theft scenario, cookie flags, enumeration checks, secrets in logs).
5. You: integrate, clean-checkout run, verify-reality, /wf-progress-review, report.
```

## Acceptance criteria
- Tests prove: lockout; refresh rotation + reuse revokes the family; logout invalidates; reset non-enumeration (same response for existing/non-existing email); verification required before login.
- Authorization matrix test passes for all current endpoints; default-deny test passes and fails when a policy is removed (mutation evidence).
- Cookies have `HttpOnly; Secure; SameSite=Strict` (Secure honoured behind HTTPS; test via forwarded proto) and correct path.
- Admin seed works once; second start does nothing; password not in logs.
- Permission change takes effect within 60 s (test with fake time provider).
- Security review: no open Critical/High findings.

## How to verify
Use curl/HTTP client: login, inspect `Set-Cookie`; replay an old refresh cookie; try `GET /admin/staff` with each role; read `docs/security/rbac-test-phase-05.md`.

## Gate to Phase 06
All criteria PASS with evidence; security sign-off recorded; PM GO.
