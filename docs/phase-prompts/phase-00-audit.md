# Phase 00 — Audit repository and environment

**Goal:** Know the real state of the machine, the repository and Antigravity before building anything. No application code is written in this phase.
**Agents:** project-manager (lead), devops-engineer (tool checks).
**Inputs:** the empty or existing GroceryHub repository; pack installed via `install.ps1`/`install.sh`.
**Outputs:** `docs/progress/phase-00-report.md`, `docs/progress/PROGRESS.md`, `docs/progress/ENVIRONMENT.md`, `docs/evidence/phase-00/*.txt`, git baseline commit.
**Dependencies:** none.

## Prompt to paste into Antigravity (main agent)
```text
You are the Project Manager of the GroceryHub project. Read AGENTS.md, then .agents/agents/project-manager.md and adopt that role. Load the skill wf-progress-review.

PHASE 00 — AUDIT. Do NOT write application code. Do NOT assume anything is installed or done; verify by running commands and saving real output.

1. Repository audit
   - Run: git status; git log --oneline -n 20; list the tree to depth 3. Report whether this is a new or existing repo, current branch, remotes, uncommitted changes.
   - Confirm the pack files exist: AGENTS.md, .agents/rules (7 files), .agents/skills (14 folders each with SKILL.md), .agents/agents (9 files), scripts/verify-reality.*, docs/spec/ (02..13). Report any missing file.
2. Environment audit (save each command's output to docs/evidence/phase-00/):
   - dotnet --list-sdks (need a 10.x SDK), node --version, npm --version (or pnpm), git --version, docker --version, docker compose version, rg --version (ripgrep), sqlcmd -? (optional), OS and shell.
   - Docker daemon reachable: docker info (report errors verbatim).
   - Free disk space and RAM (SQL Server container needs >= 2 GB RAM).
   - Ports 1433, 5173, 8080, 8025 free.
3. Antigravity audit
   - Report which Antigravity surface/version you are running in (IDE, App or CLI) if discoverable; do not guess. Check the official docs pages Rules, Skills, Custom subagents for differences from docs/spec/09-AGENT-ORCHESTRATION.md and list differences.
4. Initialise project structure (only folders/markdown, no code): docs/{spec,contracts,design,progress,evidence,qa,security,ops,agent-notes}; create docs/progress/{PROGRESS.md,QUESTIONS.md,DECISIONS.md,DEPENDENCIES.md,COMMANDS.md,TRACEABILITY.md} with headers; add .gitignore for .NET, Node, .env, Docker volumes, IDE files; add .gitattributes (LF for scripts).
5. Create the initial commit on main and branch strategy note (agent/<role>/<phase>-<slug>, integration/phase-XX).
6. Write docs/progress/phase-00-report.md: table of checks (item, command, result, evidence file, PASS/FAIL), list of missing tools with exact install instructions for Windows (winget commands) — the user installs them, you do not pretend they exist.
7. Run scripts/verify-reality and save the output.
Finish with a plain Vietnamese summary for the user: what is ready, what must be installed, what questions remain.
```

## Acceptance criteria
- Every tool check has real output saved; missing tools listed with install commands.
- All pack files verified present (counts shown).
- Docs folder structure and progress files created; initial commit exists.
- Differences between Antigravity docs and spec 09 (if any) recorded.

## How to verify
`git log --oneline`, open `docs/progress/phase-00-report.md`, compare tool versions with the evidence files; run `scripts/verify-reality`.

## Gate to Phase 01
All tools needed for Phases 4–12 are installed or the user has been told exactly what to install; initial commit made; report GO by PM.
