# ADR-006: Domain `Result<T>` Pattern & RFC 7807 `ProblemDetails` Error Model

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** `backend-developer`, `frontend-developer`, `security-reviewer`
- **References:** `docs/spec/07-API-SPECIFICATION.md` §1 & §3, `.agents/rules/backend.md`, `.agents/rules/security.md`

---

## Context

GroceryHub has rich domain invariants (`INSUFFICIENT_STOCK`, `COUPON_MIN_SUBTOTAL`, `COD_LIMIT_EXCEEDED`, `ORDER_INVALID_TRANSITION`, `REFUND_SAME_APPROVER`, etc.) that represent expected business rejections rather than unhandled system crashes.

Requirements:
- Domain and application layers should express expected business failures explicitly in method signatures without throwing exceptions for normal control flow.
- Every HTTP error returned to clients must follow a single, predictable JSON contract (RFC 7807 / RFC 9457 `application/problem+json`) containing a stable machine-readable `code`, correlation `traceId`, and field-level `errors` when applicable (`docs/spec/07-API-SPECIFICATION.md` §1).
- Stack traces, SQL query text, and internal exception details must never leak to clients (`.agents/rules/security.md`).

---

## Decision

We standardize on a two-layer error architecture: **`Result<T>` inside Domain/Application/Contracts** and **RFC 7807 `ProblemDetails` at the HTTP API boundary**:

1. **`DomainError` and `Result` / `Result<T>` in `GroceryHub.SharedKernel`:**
   ```csharp
   public enum ErrorType
   {
       Validation = 422,
       BadRequest = 400,
       Unauthorized = 401,
       Forbidden = 403,
       NotFound = 404,
       Conflict = 409,
       PreconditionFailed = 412,
       TooManyRequests = 429,
       Unexpected = 500
   }

   public sealed record DomainError(
       string Code,
       string Message,
       ErrorType Type = ErrorType.Conflict,
       IReadOnlyDictionary<string, string[]>? FieldErrors = null);
   ```
   - All domain aggregate methods that can fail business rules and all cross-module `Contracts` methods return `Result` or `Result<T>` (`Task<Result<T>>` for async calls).
   - Exceptions are reserved for truly exceptional infrastructure failures (database connectivity loss, `DbUpdateConcurrencyException`, unhandled bugs).

2. **Standardized RFC 7807 `ProblemDetails` Response Envelope:**
   - All non-2xx API responses (except `POST /payments/sepay/webhook` which returns `{"success": false}` on `401` per SePay's webhook spec `docs/spec/11-SEPAY-INTEGRATION.md` §5.2) return `Content-Type: application/problem+json`:
   ```json
   {
     "type": "https://groceryhub.vn/errors/INSUFFICIENT_STOCK",
     "title": "Insufficient stock",
     "status": 409,
     "detail": "Product 'Sữa tươi Vinamilk 1L' only has 2 units available.",
     "code": "INSUFFICIENT_STOCK",
     "traceId": "01932d8c-7c20-7a11-9b3e-00155d4a8102",
     "errors": {}
   }
   ```
   - **Status Code Mapping Rules:**
     - `400 Bad Request`: Malformed JSON payload, invalid route/query parameter syntax, or invalid webhook JSON.
     - `401 Unauthorized`: Missing/expired JWT or invalid credentials/refresh token (`UNAUTHENTICATED`, `REFRESH_TOKEN_INVALID`, `ACCOUNT_LOCKED`, `EMAIL_NOT_VERIFIED`).
     - `403 Forbidden`: Authenticated user lacks required permission policy or does not own the resource (`FORBIDDEN`).
     - `404 Not Found`: Target entity does not exist (`NOT_FOUND`).
     - `409 Conflict`: Business state/rule conflict (`INSUFFICIENT_STOCK`, `ORDER_INVALID_TRANSITION`, `ORDER_NOT_CANCELLABLE`, `IDEMPOTENCY_KEY_REUSED`, `REFUND_SAME_APPROVER`, `EMAIL_ALREADY_REGISTERED`, etc.).
     - `412 Precondition Failed`: Optimistic concurrency `If-Match` / `RowVersion` mismatch (`CONCURRENCY_CONFLICT`).
     - `422 Unprocessable Entity`: Request DTO validation failure via FluentValidation (`VALIDATION_FAILED`, with `errors: Record<string, string[]>`), or domain validation (`QUANTITY_LIMIT_EXCEEDED`, `COUPON_INVALID`, `COD_LIMIT_EXCEEDED`, etc.).
     - `429 Too Many Requests`: Rate limit exceeded (`RATE_LIMITED`, includes `Retry-After` header).
     - `500 Internal Server Error`: Unhandled exception caught by `GlobalExceptionHandler`; logs full exception to Serilog with `CorrelationId` and returns generic `detail` + `traceId` (never stack traces).

3. **Frontend Consumption:**
   - The React SPA maps `problem.code` to localized Vietnamese user messages in `web/src/shared/i18n/vi.ts` and maps `problem.errors` directly to React Hook Form field errors via `setError(field, { message })`.

---

## Alternatives Considered

1. **Throwing custom exceptions for all business rules (`throw new InsufficientStockException()`):**
   - *Rejected:* Uses exceptions as `goto` control flow, hides possible failure modes from C# method signatures, and incurs stack-unwinding overhead under concurrent checkout contention.
2. **Returning HTTP `200 OK` with `{ success: false, errorCode: "..." }`:**
   - *Rejected:* Forbidden by `.agents/skills/dotnet-api/SKILL.md` ("Returning 200 with an error body. Use correct status codes.") and breaks standard HTTP semantics, caching, and TanStack Query error handling.

---

## Consequences

- **Positive:**
  - Every failure path in `Contracts` and `Application` services is visible in the return type (`Result<T>`) and straightforward to unit-test.
  - Clients receive a uniform `ProblemDetails` structure with `code` and `traceId` for both validation errors and domain conflicts.
  - Zero risk of leaking stack traces or sensitive internal messages in production.
- **Negative / Trade-offs:**
  - Callers must check `result.IsFailure` before accessing `result.Value` (enforced by throwing `InvalidOperationException` if `Value` is read on a failed `Result<T>`).
