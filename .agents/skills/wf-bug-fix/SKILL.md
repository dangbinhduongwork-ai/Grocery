---
name: wf-bug-fix
description: Reusable procedure (invoke with /wf-bug-fix) to handle a GroceryHub bug - reproduce first, write a failing test, find the root cause, fix at the owner's scope, verify with real runs, add regression protection. Use for any defect report or failing test.
---

# /wf-bug-fix

Inputs: bug description, expected vs actual, environment, requirement ID if known.

1. **Reproduce** with the real system (running stack or integration test). Record exact steps, request/response, logs in `docs/evidence/bugs/BUG-<nnn>.md`. If you cannot reproduce, say so and ask for more data; do not guess a fix.
2. **Failing test**: add the smallest automated test that fails for the right reason (QA owns integration tests; developers own unit tests). Run it and save the failing output.
3. **Root cause**: trace to the actual defect (not the symptom). Write a 3-line explanation: where, why, since when.
4. **Owner**: identify the file owner via the ownership map. If you are not the owner, send a CR note with the failing test name and root cause; the owner fixes.
5. **Fix** minimally; no unrelated refactors. Keep the stack/library choices unchanged.
6. **Verify**: the new test passes; the full suite passes; re-run the original reproduction steps; save outputs.
7. **Regression guard**: check for the same pattern elsewhere (`rg`) and note results.
8. **Close**: update BUG file with commit id, test names, evidence; notify PM; if security-relevant, ask security-reviewer to retest.
