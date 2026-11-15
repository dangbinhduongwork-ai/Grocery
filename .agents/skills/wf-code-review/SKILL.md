---
name: wf-code-review
description: Reusable procedure (invoke with /wf-code-review) to review a branch or PR for GroceryHub - correctness vs requirements, module boundaries, security, tests that really test, evidence, style. Use after any developer agent finishes a task and before merging; the reviewer must not be the author.
---

# /wf-code-review

Inputs: branch name or diff range, requirement IDs, author agent.

1. **Independence**: confirm you are not the author. If you are, stop and ask the PM for a reviewer.
2. **Scope**: `git diff --stat <base>...<branch>`. Flag files outside the author's owned paths (ownership map in spec 09) as violations.
3. **Build from scratch**: checkout the branch in a clean worktree; run backend `dotnet build -warnaserror && dotnet test` and/or frontend `npm ci && npm run lint && npm run typecheck && npm test && npm run build`. Save outputs.
4. **Requirements**: for each requirement ID, locate code + test. Missing = finding.
5. **Reality check**: run `scripts/verify-reality`; inspect every hit.
6. **Design review**: module boundaries (no illegal references), domain invariants in domain, thin endpoints, DTOs not entities, money as `long`, `TimeProvider`, idempotency where required.
7. **Security quick pass**: policy on endpoints, object-level checks, validation, no secrets, no sensitive logging.
8. **Test quality**: do tests fail if the rule is removed? Pick two critical rules and mentally/actually mutate them. No skipped tests; no mocks of the unit under test.
9. **Report** `docs/evidence/reviews/review-<branch>.md`: verdict APPROVE / CHANGES REQUESTED; findings table (id, severity, file:line, issue, expected fix); evidence links.
10. Send findings to the owner as CR notes. Re-review after fixes from a fresh checkout.
