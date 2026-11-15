---
trigger: always_on
description: "Security baseline for GroceryHub: secrets, auth, input validation, payments."
---

# Security rules (always on)

- Never commit secrets, keys, connection strings, SePay keys, JWT signing keys, or real customer data. Use env vars / Docker secrets. Run secret scanning (gitleaks) before every merge to `main`.
- Validate all input on the server (length, format, range). Use parameterized queries only (EF Core/LINQ or parameters). No string-concatenated SQL.
- Authorization is enforced server-side on every endpoint with a named permission policy. Default-deny: endpoints without an explicit policy or `AllowAnonymous` must fail the architecture test.
- Object-level checks: a Customer can only read/modify their own cart, addresses, orders, payments, reviews (check owner id on every query).
- Passwords: ASP.NET Core Identity hasher, lockout enabled, minimum policy per requirements. Rate-limit login/register/forgot-password/checkout. Never reveal whether an email exists.
- Tokens: short-lived access JWT, rotating refresh token stored hashed, reuse detection. Cookies `HttpOnly; Secure; SameSite=Strict`. No tokens in URLs or localStorage.
- Payment integrity: only the verified SePay webhook (HMAC + timestamp, or API key at minimum) may mark a payment as paid; amount, account number and payment code must match. Never trust the frontend, query strings or redirect URLs for payment status.
- Uploads: allowlist MIME + magic bytes, size limit, re-encode images, random file names, store outside web root, never execute.
- Output: no stack traces to clients; ProblemDetails with correlation id only. Security headers + strict CSP at the reverse proxy. CORS allowlist, no wildcard with credentials.
- Audit: record who/what/when for price changes, stock adjustments, role/permission changes, refunds, manual payment decisions, order cancellations (`audit.AuditLogs`).
- Dependencies: pin versions, run `dotnet list package --vulnerable --include-transitive` and `npm audit` in CI.
