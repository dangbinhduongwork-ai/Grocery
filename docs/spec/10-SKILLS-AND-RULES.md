# 10 — Skills, Rules and Workflows (what ships and how it loads)

## 1. Inventory
| Type | Files (under `repo-template/.agents/`) | Loading mode |
|---|---|---|
| Always-on charter | `../AGENTS.md` | always active |
| Rule | `rules/security.md` | `always_on` |
| Rules (scoped) | `rules/backend.md`, `database.md`, `frontend.md`, `payments.md` | `glob` (activates when matching files are touched) |
| Rules (on demand) | `rules/ui-ux.md`, `rules/testing.md` | `model_decision` (agent reads when the task matches the description) |
| Domain skills (9) | `skills/dotnet-api`, `efcore-sqlserver`, `react-frontend`, `ui-ux-design`, `ecommerce-business`, `sepay-integration`, `security-review`, `testing-qa`, `docker-devops` | automatic by description, or `/skill-name` |
| Workflow-skills (5) | `skills/wf-code-review`, `wf-bug-fix`, `wf-ui-review`, `wf-security-review`, `wf-progress-review` | `/wf-...` slash commands |
| Agents (9) | `agents/*.md` | subagent definitions + role charters |
All rule files are < 12,000 characters (safe under both the older 12k limit and the current 24 KB limit).

## 2. Why this shape
- Project-wide rules = `AGENTS.md` (+ `security.md`). Backend/Frontend/Database/UI-UX/Security/Testing rules = scoped rule files (user requirement) with `glob`/`model_decision` triggers so they do not bloat every prompt.
- Skills hold multi-step procedures, checklists and official-doc pointers (user requirement) and are token-efficient through progressive disclosure.
- Workflows are deprecated by Google in favour of skills → shipped as `wf-*` skills (still slash-invocable, and portable to other agent tools following the Agent Skills open standard).

## 3. Rules authoring constraints (verified)
1. File must be directly inside `.agents/rules/` (subfolders ignored unless registered in `.agents/rules.json`).
2. Frontmatter required: `trigger` ∈ {`always_on`,`model_decision`,`glob`,`manual`} (snake_case exactly; `alwaysOn`/`modelDecision` are silently dropped).
3. `glob` rules need `globs` (comma-separated; quote patterns starting with `*`). `model_decision` rules need `description`.
4. Keep each file short; split by purpose.
5. Rules are cumulative with `AGENTS.md`; parent folders up to the repo root are scanned, **subfolders below the workspace are not**. Open Antigravity on the repository root.

## 4. Skills authoring constraints (verified)
- Directory `.agents/skills/<name>/SKILL.md`; `description` must say what the skill does **and when** to use it (third person).
- Keep SKILL.md focused (< 500 lines); put big references under `references/` and tell the agent when to read them.
- Skill names are lowercase with hyphens; slash command = name.

## 5. Agent ↔ skill mapping
| Agent | Skills |
|---|---|
| project-manager | ecommerce-business, wf-progress-review, wf-code-review |
| backend-developer | dotnet-api, efcore-sqlserver, ecommerce-business, security-review |
| frontend-developer | react-frontend, ui-ux-design, ecommerce-business |
| ui-ux-designer | ui-ux-design, react-frontend, ecommerce-business, wf-ui-review |
| database-engineer | efcore-sqlserver, ecommerce-business, dotnet-api |
| payment-specialist | sepay-integration, dotnet-api, efcore-sqlserver, security-review, ecommerce-business |
| security-reviewer | security-review, wf-security-review, dotnet-api, react-frontend, docker-devops |
| qa-engineer | testing-qa, wf-code-review, wf-bug-fix, ecommerce-business |
| devops-engineer | docker-devops, security-review |

## 6. Installing and verifying
1. Run `install.ps1` (Windows) or `install.sh` from the pack root with the target repo path; it copies `repo-template/*` to the repo root and docs `02`–`13` to `docs/spec/`.
2. Open the **repo root** in Antigravity.
3. Verify (Phase 02 does this and records evidence): Customizations → Rules shows the 7 rule files with correct triggers; Skills lists 14 skills; typing `/wf-` autocompletes the workflow-skills; `/agents` (App/CLI) lists the 9 agents; ask "Which rules and skills are currently active and why?" and compare to the files.
4. If a rule/skill is missing: check frontmatter spelling, folder (`.agents` with `s`), flat location, file name, and that the workspace root is the repo root.

## 7. Maintenance
PM owns `.agents/**`. Any change goes through a CR + `/wf-code-review`. Re-check Antigravity docs at the start of Phase 2 and Phase 12 for changed behavior (the product evolves quickly: IDE 2.x, App 2.x, CLI 1.x at research time).
