# ADR-003: Authentication, Short-Lived JWT Access Tokens, Rotating Refresh Cookie & Server-Side Permission Cache

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** `backend-developer`, `security-reviewer`, `project-manager`
- **References:** `DEC-001`, `docs/spec/05-SYSTEM-ARCHITECTURE.md` §7–§8, `docs/spec/permissions-seed.md`, `.agents/rules/security.md`

---

## Context

GroceryHub serves both anonymous Guests, authenticated Customers, and privileged internal operators (`Staff`, `Manager`, `Admin`) from a single Vite SPA behind an nginx reverse proxy (`docs/spec/05-SYSTEM-ARCHITECTURE.md` §1, §6).

Security requirements (`NFR-003`, `.agents/rules/security.md`, `docs/security/threat-model.md`):
- Passwords must be hashed with a modern KDF and protected by account lockout and rate limiting.
- Access tokens must never be stored in `localStorage` or `sessionStorage` where XSS could exfiltrate long-lived credentials.
- When an Admin locks a user account (`customers.lock` / `staff.manage`) or revokes a permission (`roles.manage` / `staff.manage`), the change must take effect quickly without waiting for a long-lived JWT to expire.
- Refresh tokens must resist theft via token rotation and family reuse detection.

---

## Decision

We implement a hybrid **ASP.NET Core Identity + in-memory short-lived JWT access token + `HttpOnly` rotating refresh token cookie + server-side cached permission resolution** architecture:

1. **Identity & Password Policy:**
   - `ASP.NET Core Identity` (`UserManager<ApplicationUser>`, `PasswordHasher<ApplicationUser>` using PBKDF2-HMAC-SHA512) manages user credentials, security stamps, email verification tokens, and password reset tokens.
   - Lockout policy: 5 failed login attempts lock the account for `15 minutes`.
   - Rate limiting (`auth` policy): `5 requests / minute` per IP + normalized email on `/auth/login`, `/auth/register`, `/auth/forgot-password`.
   - Anti-enumeration: `/auth/forgot-password` and `/auth/resend-verification` always return `202 Accepted` regardless of whether the email exists.

2. **Short-Lived Access JWT (In-Memory Only on SPA):**
   - Lifetime: **15 minutes** (`900 seconds`).
   - Signing: HMAC-SHA256 (`HS256`) with a 256-bit+ secret from `JWT_SIGNING_KEY` environment variable, validated for `Issuer`, `Audience`, `Lifetime` (`ClockSkew = 30s`), and signature.
   - Claims included: `sub` (`UserId`), `email`, `name`, `role` (role names), `jti` (unique token ID).
   - **No permissions array in JWT:** Fine-grained permission codes (`orders.process`, `refunds.approve`, etc.) are **never** embedded in the JWT payload.
   - The React SPA holds `accessToken` strictly in memory (JavaScript closure / state) and sends it via `Authorization: Bearer <token>`.

3. **Rotating Refresh Token in `HttpOnly` Cookie (`gh_refresh`):**
   - Stored in `identity.RefreshTokens` as a **SHA-256 hash (`TokenHash`)** — raw token bytes (32 cryptographically random bytes, Base64Url-encoded) are never persisted in the database.
   - Delivered to the browser in cookie `gh_refresh` with attributes:
     `HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Max-Age=1209600` (14 days).
   - **Rotation & Reuse Detection (`FamilyId`):**
     - Every `/auth/login` creates a new `FamilyId` (`Guid`).
     - `POST /api/v1/auth/refresh` requires header `X-Requested-With: XMLHttpRequest` (CSRF defense-in-depth alongside `SameSite=Strict`).
     - Presenting a valid unrevoked refresh token marks it `RevokedAt = now`, links `ReplacedByTokenId`, and issues a new token in the same `FamilyId`.
     - If an already-revoked token is presented again (**token reuse detected**), the server immediately revokes **all** refresh tokens sharing that `FamilyId`, logs a security warning, and returns `401 REFRESH_TOKEN_INVALID`.

4. **Server-Side Permission Resolution & Cache (`IPermissionService`):**
   - Every protected endpoint declares `.RequireAuthorization("<permission>")` (or `"auth"` for authenticated customer endpoints).
   - `PermissionAuthorizationHandler` calls `IPermissionService.GetEffectivePermissionsAsync(userId, ct)`, which resolves:
     $$\text{EffectivePermissions}(u) = \left(\bigcup_{r \in \text{Roles}(u)} \text{RolePermissions}(r)\right) \cup \text{UserPermissions}(u)$$
     while verifying `u.Status == UserStatus.Active && u.DeletedAt == null`.
   - Cached in `IMemoryCache` under key `perm:user:{userId}` with TTL **60 seconds**.
   - Immediately evicted whenever user status (`Lock`/`Unlock`/`Disable`), `UserRoles`, `RolePermissions`, or `UserPermissions` are modified.

5. **Guest Cart Cookie (`gh_cart`):**
   - Unauthenticated guests receive cookie `gh_cart` (`HttpOnly; Secure; SameSite=Lax; Path=/api/v1; Max-Age=2592000` = 30 days) containing an opaque random token mapped to `cart.Carts.GuestToken`. Merged into the authenticated user's cart upon `POST /auth/login`.

---

## Alternatives Considered

1. **Storing JWT and Refresh Token in `localStorage`:**
   - *Rejected:* Violates `.agents/rules/security.md`. Any XSS vulnerability or compromised third-party script could steal both tokens silently.
2. **Embedding all 29 permission claims inside the JWT:**
   - *Rejected:* Bloats the `Authorization` header on every request and prevents immediate revocation when a Staff/Manager role or permission is modified until the JWT expires.
3. **Stateful Cookie Session only (no Bearer JWT):**
   - *Rejected:* `AGENTS.md` §1 fixes the technology stack to `ASP.NET Core Identity + JWT access token + rotating refresh token + permission-based RBAC`.

---

## Consequences

- **Positive:**
  - Resistant to XSS token theft (refresh token is `HttpOnly` and path-restricted to `/api/v1/auth`; access token lives only in JS memory).
  - Stolen refresh tokens are detected on first reuse and invalidate the entire session family.
  - Permission and account-lock changes take effect immediately (via cache eviction) or within at most 60 seconds.
- **Negative / Trade-offs:**
  - On full page reload (`F5`), the SPA in-memory access token is cleared and the app makes one silent `POST /api/v1/auth/refresh` call before rendering authenticated routes.
