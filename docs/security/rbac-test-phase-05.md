# GroceryHub — RBAC Matrix Integration Test Report (Phase 05)

- **Phase:** 05 — Identity, Authentication & Permissions Matrix
- **Task ID:** `T05-03`
- **Owner / Author:** `qa-engineer`
- **Date:** 2026-11-15
- **Branch:** `agent/qa-engineer/phase-05-auth-tests`
- **Reference Specs:**
  - `docs/spec/permissions-seed.md` (Permissions catalog & system role matrix)
  - `docs/spec/07-API-SPECIFICATION.md` §2, §4.1, §5.5
  - `docs/spec/adr/ADR-003-auth-tokens-cookies.md`
  - `docs/spec/05-SYSTEM-ARCHITECTURE.md` §7–§8, §10
  - `docs/evidence/phase-05/03-default-deny-mutation-proof.txt`

---

## 1. Executive Summary

All integration tests and architectural default-deny proofs for Phase 05 Identity & RBAC have been implemented and verified against the real ASP.NET Core Minimal API pipeline, real ASP.NET Core Identity authentication handlers, real EF Core SQL Server database (migrated with all 12 module schemas), and real `IMemoryCache` server-side permission cache.

Key verification outcomes:
1. **Full Authentication Lifecycle:** Registration, email verification, login with JWT access token + secure `gh_refresh` cookie (`HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth`), refresh rotation, authenticated `/api/v1/auth/me`, and logout family revocation operate in complete accordance with `ADR-003`.
2. **Account Lockout:** Exactly 5 failed password attempts trigger 15-minute lockout (`LockoutEnd = now + 15m`). Subsequent attempts during lockout—even with the correct password—are rejected with HTTP 423 `ACCOUNT_LOCKED`.
3. **Refresh Token Reuse Detection & Family Revocation:** Replaying an already-rotated refresh token immediately detects token theft, invalidates the entire session family (`FamilyId`), and prevents any further refresh by both attacker and legitimate holder.
4. **Anti-Enumeration Protections:** Both `POST /api/v1/auth/forgot-password` and `POST /api/v1/auth/resend-verification` return identical HTTP 202 `Accepted` responses (`{ "accepted": true }`) regardless of whether the submitted email address exists.
5. **Mandatory Email Verification:** Unverified accounts cannot authenticate; login attempts return HTTP 403 `EMAIL_NOT_VERIFIED`.
6. **Instantaneous Permission Cache Invalidation:** When an administrator grants/revokes user permissions or updates assigned roles, the server-side permission cache entry is evicted immediately, taking effect on the very next request without requiring application restart or waiting for cache TTL.
7. **Default-Deny Enforcement & Mutation Sanity:** The architecture test `Default_Deny_Endpoints_Require_Authorization_Or_AllowAnonymous` passed. A deliberate mutation (adding an unannotated endpoint) was caught and confirmed by a failing test execution before restoration.

---

## 2. Systematic RBAC Matrix Table

The table below catalogs every endpoint in the Auth (`/api/v1/auth/*`) and Admin Staff/Roles (`/api/v1/admin/*`) modules against 6 personas:
1. **Anonymous (`Anon`):** Unauthenticated guest (no `Authorization` header).
2. **Customer (`Cust`):** Authenticated retail customer (storefront shopper, zero admin permissions).
3. **Staff (`Stf`):** Operations associate (seeded with operational permissions `catalog.read`, `inventory.read`, `purchasing.read`, `orders.read`, `orders.process`, `orders.ship`, `customers.read`; lacks `staff.read`, `staff.manage`, and `roles.manage`).
4. **Manager (`Mgr`):** Store manager (includes `staff.read`, but lacks Admin-only `staff.manage` and `roles.manage`).
5. **Admin (`Adm`):** System administrator (all 29 permissions).

Format: `Expected / Actual Status Code`.

| # | Endpoint Route | Method | Required Policy | Anon | Cust | Stf | Mgr | Adm | Integration Test Name |
|---|---|:---:|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `/api/v1/auth/register` | POST | `AllowAnonymous()` | `201/201` | `201/201` | `201/201` | `201/201` | `201/201` | `AuthLifecycleIntegrationTests.CompleteAuthLifecycle_Register_Verify_Login_Refresh_GetMe_Logout_Succeeds` |
| 2 | `/api/v1/auth/verify-email` | POST | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `AuthLifecycleIntegrationTests.CompleteAuthLifecycle_Register_Verify_Login_Refresh_GetMe_Logout_Succeeds` |
| 3 | `/api/v1/auth/resend-verification` | POST | `AllowAnonymous()` | `202/202` | `202/202` | `202/202` | `202/202` | `202/202` | `NonEnumerationIntegrationTests.ResendVerification_ForExistingAndNonExistingEmail_BothReturn202AcceptedWithIdenticalPayload` |
| 4 | `/api/v1/auth/login` | POST | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `AuthLifecycleIntegrationTests.CompleteAuthLifecycle_Register_Verify_Login_Refresh_GetMe_Logout_Succeeds` |
| 5 | `/api/v1/auth/refresh` | POST | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `AuthLifecycleIntegrationTests.CompleteAuthLifecycle_Register_Verify_Login_Refresh_GetMe_Logout_Succeeds` |
| 6 | `/api/v1/auth/forgot-password` | POST | `AllowAnonymous()` | `202/202` | `202/202` | `202/202` | `202/202` | `202/202` | `NonEnumerationIntegrationTests.ForgotPassword_ForExistingAndNonExistingEmail_BothReturn202AcceptedWithIdenticalPayload` |
| 7 | `/api/v1/auth/reset-password` | POST | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `AuthLifecycleIntegrationTests` / Unit tests |
| 8 | `/api/v1/auth/me` | GET | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | `RbacMatrixIntegrationTests.Auth_GetMe_PolicyMatrix` |
| 9 | `/api/v1/auth/logout` | POST | `auth` | `401/401` | `204/204` | `204/204` | `204/204` | `204/204` | `RbacMatrixIntegrationTests.Auth_Logout_PolicyMatrix` |
| 10 | `/api/v1/auth/change-password` | POST | `auth` | `401/401` | `200/200` | `200/200` | `200/200` | `200/200` | Unit & API Contract tests |
| 11 | `/api/v1/admin/staff` | GET | `staff.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `RbacMatrixIntegrationTests.Admin_GetStaffList_PolicyMatrix` |
| 12 | `/api/v1/admin/staff/{id}` | GET | `staff.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `RbacMatrixIntegrationTests.Admin_GetStaffById_PolicyMatrix` |
| 13 | `/api/v1/admin/staff` | POST | `staff.create.or.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `RbacMatrixIntegrationTests.Admin_CreateStaff_PolicyMatrix` |
| 14 | `/api/v1/admin/staff/{id}` | PUT | `staff.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `RbacMatrixIntegrationTests.Admin_UpdateStaffProfile_PolicyMatrix` |
| 15 | `/api/v1/admin/staff/{id}/roles` | PUT | `staff.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `RbacMatrixIntegrationTests.Admin_UpdateStaffRoles_PolicyMatrix` |
| 16 | `/api/v1/admin/staff/{id}/permissions` | PUT | `staff.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `RbacMatrixIntegrationTests.Admin_UpdateStaffPermissions_PolicyMatrix` |
| 17 | `/api/v1/admin/roles` | GET | `roles.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `RbacMatrixIntegrationTests.Admin_GetRolesMatrix_PolicyMatrix` |
| 18 | `/api/v1/admin/permissions` | GET | `roles.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `RbacMatrixIntegrationTests.Admin_GetPermissionsCatalog_PolicyMatrix` |
| 19 | `/api/v1/admin/roles/{id}/permissions` | PUT | `roles.manage` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `RbacMatrixIntegrationTests.Admin_UpdateRolePermissions_PolicyMatrix` |

*Note on Staff / Manager permissions:* Per `docs/spec/permissions-seed.md` §2 row 16 and `docs/spec/07-API-SPECIFICATION.md` §2 table row 61, `staff.read` is restricted to `Manager` and `Admin`. The `Staff` role lacks `staff.read` and receives HTTP 403 on `/api/v1/admin/staff` until promoted to `Manager`. Manager lacks `staff.create` by default unless selectively granted via `UserPermissions` (which is verified in `PermissionCacheInvalidationIntegrationTests`).

---

## 3. Test Suite Structure & Execution Evidence

The test suite is organized under `tests/GroceryHub.IntegrationTests/Identity/`:

| Test Suite File | Test Methods | Target Requirements |
|---|:---:|---|
| `AuthLifecycleIntegrationTests.cs` | 3 | Full authentication, cookie attributes, token rotation, `/auth/me`, logout, CSRF defense |
| `LockoutIntegrationTests.cs` | 1 | 5 failed logins lockout, HTTP 423 `ACCOUNT_LOCKED`, correct password rejection during lockout window |
| `RefreshTokenReuseIntegrationTests.cs` | 1 | Token rotation, token replay detection, full family revocation (`FamilyId`), active token invalidation |
| `NonEnumerationIntegrationTests.cs` | 2 | Anti-enumeration on `/forgot-password` and `/resend-verification` (HTTP 202 Accepted) |
| `EmailVerificationRequirementIntegrationTests.cs` | 1 | Login denial for unverified users (HTTP 403 `EMAIL_NOT_VERIFIED`) |
| `PermissionCacheInvalidationIntegrationTests.cs` | 1 | Immediate cache eviction on role update and permission grant/revoke without TTL delay |
| `RbacMatrixIntegrationTests.cs` | 66 (11 theories x 6 personas) | Complete 6-persona systematic matrix across all Auth and Admin Staff/Roles endpoints |

### Integration Test Run Summary
- **Command:** `dotnet test tests/GroceryHub.IntegrationTests --filter "FullyQualifiedName~Identity"`
- **Result:** `Passed! - Failed: 0, Passed: 75, Skipped: 0, Total: 75, Duration: 53 s`
- **Database Engine:** Real Microsoft SQL Server 2022 instance (using managed networking on Windows)
- **Mocks Used:** Only external boundary `IEmailService` (recorded dispatched emails to verify tokens). Database, EF Core queries, ASP.NET Core Identity, password hasher, and authorization policies ran 100% real.
