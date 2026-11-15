---
name: testing-qa
description: Test strategy and procedures for GroceryHub - xUnit unit tests, Testcontainers SQL Server integration tests, WebApplicationFactory API tests, Playwright E2E, axe accessibility, concurrency and idempotency tests, mutation sanity checks, evidence capture and anti-fake-test detection. Use when writing tests, verifying a feature, or deciding whether a task is truly done.
---

# testing-qa

## Goal
Prove behavior through the real system. A passing test must be impossible to achieve with a fake implementation.

## Process
1. Map requirement IDs to tests in `docs/qa/test-plan.md` (table: FR-ID | test name | level | status | evidence file).
2. Write the failing test first for new behavior or bug fix.
3. Integration tests: `WebApplicationFactory<Program>` + Testcontainers SQL Server; real EF Core, real auth pipeline (issue real JWTs from a test login), real migrations.
4. E2E: Playwright against the compose stack; seed through public APIs or the dev seeder, never by writing rows that skip business rules.
5. Concurrency: run N parallel requests (`Task.WhenAll`) for last-unit stock, double webhook, duplicate Idempotency-Key, coupon limit; assert exactly one winner.
6. Mutation sanity: deliberately break one rule in a scratch branch; at least one test must fail. Record in evidence.
7. Run everything from a clean checkout and save outputs in `docs/evidence/phase-XX/`.

## Standards
- Naming: `Method_Scenario_ExpectedResult`. One behavior per test. Arrange-Act-Assert.
- Deterministic: `FakeTimeProvider`, no sleeps, isolated database per test class (respawn/reset) or per-run containers.
- External boundaries only may be faked (SePay HTTP, SMTP). Never fake the DB, authorization, validators.
- Frontend: Testing Library by role/label, MSW for unit tests only, axe for a11y.

## Anti-fake detection (run `scripts/verify-reality`)
Search the repo for: `NotImplementedException`, `TODO`/`FIXME` without issue id, `[Skip`/`.skip(`/`xit(`, hard-coded success responses, `Thread.Sleep`, `Task.Delay` used as synchronization in tests, `Mock<` of the class under test, Faker/seed data inside production code paths, `return Ok()` handlers with no logic.

## Common mistakes
- Tests that assert on mocks instead of behavior; shared mutable state across tests.
- Testing happy path only; no authorization-denied tests.
- Flaky tests "fixed" with retries/sleeps.
- Reporting "all tests pass" without a real run log.

## Checklist
- [ ] FR/NFR mapped to tests
- [ ] Authorization allowed + denied tests for new endpoints
- [ ] Concurrency/idempotency tests where required
- [ ] Clean-checkout run log saved
- [ ] Mutation sanity recorded

## Official documentation
- https://xunit.net/docs/getting-started/v3/cmdline
- https://dotnet.testcontainers.org/
- https://learn.microsoft.com/aspnet/core/test/integration-tests
- https://playwright.dev/docs/intro
- https://vitest.dev/guide/
- https://testing-library.com/docs/react-testing-library/intro/
