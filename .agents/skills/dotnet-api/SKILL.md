---
name: dotnet-api
description: Builds and reviews the GroceryHub ASP.NET Core 10 Web API in a modular monolith - minimal API endpoints, application services, validation, ProblemDetails, JWT auth with permission policies, rate limiting, OpenAPI, health checks, logging. Use whenever writing or reviewing C# backend code, endpoints, DTOs, auth wiring or module boundaries, even if the user does not say "dotnet".
---

# dotnet-api

## Goal
Produce small, secure, testable backend code that respects module boundaries and the API spec (`docs/spec/07-API-SPECIFICATION.md`).

## Process
1. Read the requirement IDs and the endpoint rows in spec 07; read `.agents/rules/backend.md`.
2. Locate (or create) the module project. Add Domain types first (invariants + unit tests), then Application service, then Endpoint + validator, then DI registration.
3. Add the endpoint with an explicit permission policy (`.RequireAuthorization("orders.process")`) or `.AllowAnonymous()` with a comment why. Default-deny is enforced by an architecture test.
4. Return `TypedResults` and `ProblemDetails`; document with OpenAPI metadata (`WithName`, `WithTags`, `Produces<T>`, `ProducesProblem`).
5. Regenerate `docs/contracts/openapi.json` (command recorded in `docs/progress/COMMANDS.md`), notify frontend via CR if shapes changed.
6. Run `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`. Save output as evidence.

## Standards
- Layering: Endpoint -> Application service -> Domain/Persistence interface. No EF types in Api/Contracts.
- Auth: JwtBearer with short access tokens; refresh token rotation endpoint; permission policies from a dynamic `IAuthorizationPolicyProvider`; permissions resolved server-side through a cached `IPermissionService` (TTL <= 60 s) keyed by user id.
- Validation: FluentValidation per request DTO; server is the authority. Map validation failures to `ValidationProblemDetails` (HTTP 400/422 consistently - choose one in spec 07 and keep it).
- Errors: global `IExceptionHandler` -> ProblemDetails with `traceId`; domain rule violations use typed exceptions/results mapped to 409/422.
- Idempotency: `Idempotency-Key` header for `POST /checkout/orders` and payment-attempt creation; store key + request hash + response.
- Rate limiting: `AddRateLimiter` policies (`auth`, `checkout`, `public-read`), keyed by user id or IP.
- Config: options classes + `ValidateOnStart`; secrets from env.
- Observability: Serilog JSON logs with correlation id, `/health/live` and `/health/ready` (checks DB).
- Time/ids: inject `TimeProvider`; ids as `Guid`.

## Best practices
- Prefer vertical feature folders inside a module (`Orders/PlaceOrder/*`).
- Use `AsNoTracking` + projection for reads; load aggregates fully for writes.
- Keep endpoints under ~30 lines; push logic down.
- Use `CancellationToken` everywhere; never block on async.

## Common mistakes
- Forgetting object-level ownership checks (customer reads another customer's order).
- Putting permission strings inline in many places (use constants from `Permissions` class generated from spec).
- Using commercial-licensed libraries (MediatR, AutoMapper, FluentAssertions 8+, MassTransit 9+). Check license first.
- Returning 200 with an error body. Use correct status codes.
- Catching `Exception` and returning success.

## Checklist
- [ ] Policy on every endpoint; denied-role test exists
- [ ] Owner check for customer resources
- [ ] DTO validators + tests
- [ ] OpenAPI updated + contract file regenerated
- [ ] No entity leaks, no `.Result`, no `DateTime.UtcNow`
- [ ] Build/test/format evidence saved

## Official documentation
- https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis
- https://learn.microsoft.com/aspnet/core/security/authentication/
- https://learn.microsoft.com/aspnet/core/security/authorization/policies
- https://learn.microsoft.com/aspnet/core/performance/rate-limit
- https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview
- https://learn.microsoft.com/aspnet/core/fundamentals/error-handling
