---
name: wf-progress-review
description: Reusable procedure (invoke with /wf-progress-review) for the Project Manager to audit real progress and decide the phase gate for GroceryHub - verify claims against code and test output, update progress and traceability files, list blockers, GO/NO-GO. Use at the end of every phase and whenever someone says something is "done".
---

# /wf-progress-review

Inputs: phase id, `docs/progress/phase-XX-plan.md`, agent reports.

1. **Trust nothing**: for each task in the plan, locate the commit(s) and files. Check owners edited only owned paths (`git log --name-only`).
2. **Run from clean checkout**: build + all tests + lint (backend and frontend as applicable). Save to `docs/evidence/phase-XX/gate-run.md` with exit codes.
3. **Reality scan**: run `scripts/verify-reality`; any hit must be fixed or justified in writing.
4. **Acceptance criteria**: copy the phase's criteria into the report; mark each PASS/FAIL with a link to evidence. No evidence = FAIL.
5. **Traceability**: update `docs/progress/TRACEABILITY.md` (FR/NFR -> code -> test -> evidence). Count requirements implemented vs planned.
6. **Open items**: `docs/progress/QUESTIONS.md`, CR notes not closed, security findings, technical debt.
7. **Decision**: GO only if every criterion is PASS, no open Critical/High findings, and the phase prompt's gate conditions hold. Otherwise NO-GO with the exact list of what to fix and who owns it.
8. **Update** `docs/progress/PROGRESS.md` (phase status, date, next phase) and write `docs/progress/phase-XX-report.md`.
9. Tell the user in plain Vietnamese: what works (verified), what does not, what is needed from them (keys, decisions).
