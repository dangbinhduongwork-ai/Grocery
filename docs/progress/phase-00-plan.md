# Phase 00 Plan — Audit Repository and Environment

- **Phase:** 00
- **Date:** 2026-11-15
- **Lead Agent:** `project-manager`
- **Supporting Agent:** `devops-engineer` (environment & tool check verification)
- **Branch Strategy:** Baseline setup on `main` (per Phase 00 prompt item 5: *"Create the initial commit on main and branch strategy note (`agent/<role>/<phase>-<slug>`, `integration/phase-XX`)"*). Starting Phase 01+, all agent work uses `agent/<role>/<phase>-<slug>` merged into `integration/phase-XX` and then `main`.

## 1. Tasks, Ownership, and Execution Order

| ID | Task | Owner | Order | Owned Paths | Evidence Expected |
|---|---|---|---|---|---|
| P00-T1 | Repository & Pack File Audit (`git status`, `git log`, tree depth 3, verify `AGENTS.md`, 7 rules, 14 skills, 9 agents, 2 `verify-reality` scripts, 12 spec files `02..13`) | `project-manager` | 1 (Seq) | `docs/evidence/phase-00/01-repo-audit.txt`, `docs/evidence/phase-00/02-pack-files.txt` | Real command outputs with exact file counts |
| P00-T2 | Environment & Tooling Audit (`dotnet --list-sdks`, `node`, `npm`, `git`, `docker`, `docker compose`, `rg`, `sqlcmd`, `docker info`, OS/Shell, RAM/Disk, Ports `1433, 5173, 8080, 8025`) | `project-manager` → verified by `devops-engineer` | 2 (Seq) | `docs/evidence/phase-00/03-*.txt` .. `13-*.txt`, `docs/progress/ENVIRONMENT.md` | Verbatim command outputs + Windows `winget` install commands for missing tools |
| P00-T3 | Antigravity Surface & Documentation Audit (detect IDE/App/CLI surface and version; compare official Rules, Skills, Custom Subagents docs against `docs/spec/09-AGENT-ORCHESTRATION.md`) | `project-manager` | 3 (Par with T2) | `docs/evidence/phase-00/16-antigravity-surface-version.txt`, `docs/progress/phase-00-report.md` | Verified version (`product.json`) and documented delta table |
| P00-T4 | Initialise Project Structure & Progress Tracking (`docs/{spec,contracts,design,progress,evidence,qa,security,ops,agent-notes}`, `PROGRESS.md`, `QUESTIONS.md`, `DECISIONS.md`, `DEPENDENCIES.md`, `COMMANDS.md`, `TRACEABILITY.md`, `ENVIRONMENT.md`, `.gitignore`, `.gitattributes`) | `project-manager` | 4 (Seq) | `docs/**`, `.gitignore`, `.gitattributes` | Directory listing and committed markdown files |
| P00-T5 | Reality Scan (`scripts/verify-reality.ps1`) & Phase 00 Gate Review (`/wf-progress-review`) | `project-manager` | 5 (Seq) | `scripts/verify-reality.ps1`, `docs/evidence/phase-00/14-verify-reality-initial.txt`, `docs/evidence/phase-00/17-verify-reality.txt`, `docs/progress/phase-00-report.md` | Non-silent `rg` check in `verify-reality.ps1`, real run output, and Phase 00 report with GO/NO-GO |

## 2. Definition of Done (Phase 00)
1. No application code written.
2. Every tool check has real output saved under `docs/evidence/phase-00/*.txt`; missing tools are honestly reported as `FAIL (Missing)` with exact Windows `winget` installation commands.
3. All pack files verified present with counts shown.
4. `docs/` folder structure and all `docs/progress/*.md` tracking files created; `.gitignore` and `.gitattributes` committed on `main`.
5. Differences between official Antigravity docs/runtime and `docs/spec/09-AGENT-ORCHESTRATION.md` recorded.
6. `scripts/verify-reality.ps1` executed and output saved.
