# CR-002: Phase 05 Security Hardening — Token Logging, Reset Token Expiry, and Production JWT Key Validation

- **CR ID:** `CR-002`
- **Phase:** 05 — Identity, Authentication & Permissions Matrix
- **Author:** `security-reviewer`
- **Addressed To:** `backend-developer`
- **Status:** Proposed
- **Date:** 2026-11-15
- **Reference Findings:** `SEC-P05-001`, `SEC-P05-002`, `SEC-P05-003` in `docs/security/review-phase-05.md`

---

## 1. Summary of Required Changes

During the Phase 05 Security Review, the authentication and authorization implementation demonstrated robust enforcement of PBKDF2 hashing, account lockout, rotating refresh tokens with reuse detection, and default-deny RBAC. Three non-blocking security hardening recommendations were identified:

1. **`SEC-P05-001` (Medium): Redact verification and password reset tokens in `EmailService.cs`.**
   - *Current Behavior:* `EmailService.cs` logs the full plaintext token via `_logger.LogInformation(...)` for both verification and password reset requests.
   - *Security Risk:* Sensitive single-use tokens are exposed in plaintext to all log sinks (stdout, log files, centralized logging). Anyone with log read permissions can obtain the token and take over user accounts.
   - *Proposed Fix:* Remove `{Token}` from `LogInformation` format strings or log only in `LogLevel.Debug` under Development environment. The token should only be dispatched inside the email body.

2. **`SEC-P05-002` (Medium): Add expiration timestamp to password reset tokens.**
   - *Current Behavior:* `AuthService.GenerateOpaqueToken` computes `SHA256(userId:N : securityStamp)`. Because `user.SecurityStamp` is not rotated upon reset request, the token has no embedded expiration timestamp and is deterministic and valid indefinitely until the user completes a reset.
   - *Security Risk:* An unused password reset token remains valid indefinitely, violating OWASP ASVS v4.0.3 §2.5.2 (short expiration window, e.g., $\le 1$ hour).
   - *Proposed Fix:* Embed a timestamp into the token (e.g., Base64Url-encoded `userId:timestampUtc:HMAC(userId:timestampUtc:securityStamp)`), validate that `now - timestamp <= TimeSpan.FromHours(1)`, or store an explicit `PasswordResetToken` entity with `ExpiresAt`.

3. **`SEC-P05-003` (Low): Enforce production JWT signing key override.**
   - *Current Behavior:* `appsettings.json` contains a default key `"groceryhub-dev-jwt-super-secret-signing-key-minimum-256-bits-length!"`.
   - *Security Risk:* If an operator accidentally deploys the API to Production without providing `JWT_SIGNING_KEY` / `Jwt__SecretKey` environment variable, the application falls back to this publicly known key.
   - *Proposed Fix:* In `Program.cs` / `JwtOptions` validation, assert that if `Environment.IsProduction()`, `SecretKey` cannot equal the known development key.
