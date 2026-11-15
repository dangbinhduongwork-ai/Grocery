# Phase 02 Plan — Agents, Skills, Rules (Install and Prove They Work)

- **Phase:** 02
- **Date:** 2026-11-15
- **Lead Agent:** `project-manager`
- **Supporting Agents:** `backend-developer` (Smoke Test 1 & Worktree Test), `qa-engineer` (Smoke Test 2, `verify-reality` Self-Test & Worktree Test), `devops-engineer` (Tooling & Subagent Configuration Audit)
- **Integration Branch:** `integration/phase-02` → merged to `main` after gate `GO`
- **Dependencies:** Phase 01 `GO` (`phase-01-done`)

## 1. Tasks, Ownership, Branches, and Execution Order

| ID | Task | Owner | Order | Branch | Owned Paths | Evidence Expected |
|---|---|---|---|---|---|---|
| P02-T1 | Rules, Skills & Agents Frontmatter Audit + Active Context Verification + `/wf-progress-review` Dry Run | `project-manager` | 1 (Seq) | `integration/phase-02` | `docs/evidence/phase-02/01-frontmatter-audit.txt`, `02-rules-skills-active.txt`, `.agents/**` | Verified YAML frontmatter across 7 rules, 14 skills, 9 agents; proof of active rules/skills in `<user_rules>`/`<skills>` |
| P02-T2 | Subagents Tooling Fix & Smoke Tests 1, 2, 3 (`backend-developer`, `qa-engineer`, and parallel `branch` mode isolation) | `project-manager` → `backend-developer` & `qa-engineer` | 2 (Seq) | `integration/phase-02` | `.agents/agents/*.md`, `docs/evidence/phase-02/hello-backend.txt`, `docs/evidence/phase-02/03-subagent-smoke-tests.txt` | Fix `.agents/agents/*.md` tool bindings if needed; real `hello-backend.txt` (`10.0.404`); `qa-engineer` `verify-reality` run; parallel `branch` subagent isolation test |
| P02-T3 | `verify-reality.ps1` Self-Test (`src/Tmp/Fake.cs` with `NotImplementedException` + `web/src/tmp.ts` with `localStorage.setItem('token', t)`) | `qa-engineer` / `project-manager` | 3 (Seq) | `integration/phase-02` | `scripts/verify-reality.ps1`, `docs/evidence/phase-02/04-verify-reality-selftest.txt` | Both planted violations detected with non-zero exit code (`ExitCode: 1`); temp files removed; clean run exits `0` |
| P02-T4 | Git Worktrees Parallel Creation, Commit, Merge & Cleanup Test (`agent/backend-developer/phase-02-test` & `agent/qa-engineer/phase-02-test`) | `project-manager` | 4 (Seq) | `integration/phase-02` | `docs/evidence/phase-02/05-git-worktrees-test.txt`, `docs/evidence/phase-02/worktree-backend.txt`, `docs/evidence/phase-02/worktree-qa.txt` | Real `git worktree add`, commits on both branches, clean merge into `integration/phase-02`, and `git worktree remove` |
| P02-T5 | Execution Mode Decision (`DEC-012`), Phase 02 Report & Gate Review (`/wf-progress-review`) | `project-manager` | 5 (Seq) | `integration/phase-02` → `main` | `docs/progress/DECISIONS.md`, `docs/progress/PROGRESS.md`, `docs/progress/phase-02-report.md` | `DEC-012` recorded, complete mechanism verification table, Vietnamese user guide |

## 2. Definition of Done (Phase 02)
1. Table with observed behavior for rules, skills, slash invocation, subagents, and worktrees in `docs/progress/phase-02-report.md`.
2. Execution mode recorded in `docs/progress/DECISIONS.md` (`DEC-012`).
3. Successful end-to-end subagent delegations (`hello-backend.txt`, `qa-engineer` reality check, parallel `branch` isolation) verified with real artifacts.
4. All 7 rules, 14 skills, and 9 agents verified to have valid YAML frontmatter (and `.agents/agents/*.md` updated with valid tool names so custom subagents can execute commands and write files).
5. `scripts/verify-reality.ps1` self-test detects both planted violations (`ExitCode: 1`) and passes clean (`ExitCode: 0`) after cleanup.
