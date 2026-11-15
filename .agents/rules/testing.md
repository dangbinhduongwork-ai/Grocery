---
trigger: model_decision
description: "Apply when writing, running or reviewing tests, or when claiming a task is complete."
---

# Testing rules

- Test pyramid: many unit tests (domain rules), integration tests against real SQL Server in Testcontainers (EF Core queries, endpoints via `WebApplicationFactory`, authorization, webhook idempotency), a thin set of Playwright E2E flows (guest browse -> register -> checkout COD; checkout QR with simulated signed webhook; admin order processing).
- Libraries: xUnit, Shouldly (or built-in asserts), Testcontainers, Playwright, Vitest + Testing Library + MSW (MSW only for frontend unit tests, never for E2E).
- Every acceptance criterion in a phase prompt maps to at least one automated test or a documented manual check with evidence.
- Tests must be deterministic: inject `TimeProvider`, no real network except the SQL Server container, no sleeps for synchronization.
- Do not mark tests skipped/ignored; do not catch assertion exceptions; do not assert on mocks of the unit under test.
- Concurrency tests are mandatory for: last-unit stock race, double webhook delivery, double checkout with the same Idempotency-Key, coupon usage limit.
- Report format for evidence: command, exit code, summary line (e.g. `Passed! - Failed: 0, Passed: 142`) saved under `docs/evidence/phase-XX/`.
- Coverage is a signal, not a goal: aim >= 80% line coverage on Domain + Application; do not add meaningless tests to reach numbers.
