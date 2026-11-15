# 09 — Agent Orchestration for Google Antigravity

## 1. What Antigravity actually supports (verified in official docs, 14 Nov 2026)
Source: https://antigravity.google/docs (pages: Rules, Skills, Custom subagents, Teamwork, Workflows→Skills migration). Re-verify before relying on any item; do not invent features.

| Mechanism | Verified facts | Where it is documented as available |
|---|---|---|
| **Rules** | `AGENTS.md` / `GEMINI.md` (plain Markdown, always active) in workspace folder or parents up to repo root; `.agents/rules/*.md` (legacy `.agent/rules/` still read) — **flat directory only**, each file needs YAML frontmatter with `trigger: always_on \| model_decision \| glob \| manual`; `description` required for `model_decision`; `globs` required for `glob`. Invalid/missing frontmatter ⇒ rule silently ignored. Limits: 24 KB per file; 20,000-token budget for all active global/always-on rules. Global: `~/.gemini/GEMINI.md`, `~/.gemini/config/rules/*.md`. `@[label](path)` inlines a file into a rule. | App, CLI, IDE |
| **Skills** | Folder `.agents/skills/<name>/SKILL.md` (workspace) or `~/.gemini/config/skills/<name>/` (global); frontmatter `name` (optional, defaults to folder), `description` (required; drives automatic activation); progressive disclosure (name+description first, body on demand); optional `scripts/`, `references/`, `examples/`, `resources/`. Invocable as `/<skill-name>`. | App, CLI, IDE |
| **Workflows** | Markdown files `.agents/workflows/<name>.md` invoked `/<name>`, 12,000-char limit — **officially deprecated**; a new workflow system is announced; skills take precedence over same-named workflows. | IDE (legacy) |
| **Custom subagents** | `.agents/agents/<name>.md` or `.agents/agents/<name>/agent.md` (global `~/.gemini/config/agents/`). Frontmatter: `name`*, `description`*, `tools`, `mainAgent`, `subagent`, `model` (`inherit/flash/pro`), `commandExecutionPolicy` (`off/auto/eager/sandbox`), `mcpServers`, `skills`/`plugins`. Body = system prompt. Parent calls `invoke_subagent` (workspace mode `inherit`, `branch` = isolated Git worktree, or `share`); subagents start with a clean context, run asynchronously, can message each other, nesting depth ≤ 10, inherit parent's permissions/sandbox. `/agents` panel lists/monitors. **Known issue: a misspelled/unmapped tool name in `tools` can hang the subagent.** | **Antigravity App and CLI** (the page does not list the IDE) |
| Built-in subagents | `research`, `browser`, `self` | App/CLI |
| **/teamwork** | Two-phase: scoping interview → prompt artifact → autonomous team (Sentinel, Project Orchestrator, Explorers, Workers with **exclusive file ownership**, Critic/Challenger/Auditor/Success Auditor verification gates), integrity modes `development/demo/benchmark`, artifacts for request/plan/progress. | App, CLI; **paid plans** |
| **/swarm**, **/boost** | Swarm: coordinator + named workers on a shared message board; Boost: 3-tier reasoning hierarchy. | App/CLI; Pro/Ultra/Enterprise tiers |
| Plans/artifacts | `/plan` implementation-plan artifact; walkthrough artifact; screenshots; browser recordings (IDE). | App/CLI/IDE |

### Consequences for this pack
1. **Rules** are shipped as flat `.agents/rules/*.md` with valid `trigger` frontmatter, plus `AGENTS.md` for the always-on charter (also readable by other tools).
2. **Skills** are the primary reusable mechanism. The five requested "workflows" are shipped as **skills named `wf-*`** (invoke `/wf-code-review` etc.) because workflows are deprecated.
3. **Agents** are shipped as `.agents/agents/*.md` (valid custom-subagent format). Because the IDE is not documented as supporting custom subagents, the pack defines **two execution modes** that use the *same files*:
   - **Mode A — Subagent mode (App/CLI):** the PM (main agent) delegates with `invoke_subagent`, using `branch` workspaces for parallel work. Monitor with `/agents`.
   - **Mode B — Role-adoption mode (IDE or any surface without subagents):** you start a **separate conversation per role** (or one conversation per task) and begin with: "Adopt the role defined in `.agents/agents/<role>.md`. Obey its scope, file ownership and forbidden list. Load the listed skills." Parallelism = several conversations/windows on **separate git worktrees**.
   Phase 02 includes a smoke test that detects which mode works on the user's installation and records it in `docs/progress/DECISIONS.md`.
4. `tools` is omitted from agent frontmatter (to avoid the documented hang). Phase 02 inspects the default tool set; add only exact documented names (`view_file`, `replace_file_content`, `grep_search`, `run_command` are documented) after testing.
5. `/teamwork` is **optional** (paid plan) and recommended only for Phases 8–9 (large parallel frontend work) once Phases 4–7 are stable; it already enforces exclusive file ownership and independent verification, which matches this pack's rules. Teamwork's Phase-1 interview should be answered from the pack's docs ("specify what, not how"); integrity mode `development`.

## 2. Roles (9) and responsibilities
`project-manager` (orchestrator/integration owner), `backend-developer`, `frontend-developer`, `ui-ux-designer`, `database-engineer`, `payment-specialist`, `security-reviewer`, `qa-engineer`, `devops-engineer`. Full prompts: `.agents/agents/*.md`.

## 3. File ownership map (one owner per path)
| Path | Owner |
|---|---|
| `AGENTS.md`, `.agents/**`, `docs/progress/**`, `docs/agent-notes/` index, `README.md`, `.gitignore`, `scripts/verify-reality.*`, `docs/spec/{02,03,04,09,10}-*`, `docs/spec/permissions-seed.md`, `docs/phase-prompts/**` | project-manager |
| `src/GroceryHub.Api/**`, `src/GroceryHub.SharedKernel/**`, `src/GroceryHub.Modules.*/{Domain,Application,Api,Contracts}/**` (except Payments), `tests/GroceryHub.UnitTests/**` (non-payment), `docs/contracts/openapi.json`, `*.sln`, `Directory.*.props`, `docs/spec/{05,07}-*`, `docs/spec/{module-contracts,events,state-machines}.md`, `docs/spec/adr/**` | backend-developer |
| `src/GroceryHub.Persistence/**`, `src/GroceryHub.Modules.*/Infrastructure/Persistence/**`, `**/Migrations/**`, `deploy/db/**`, `docs/spec/06-*` | database-engineer |
| `src/GroceryHub.Modules.Payments/{Domain,Application,Api,Contracts}/**`, `tests/GroceryHub.UnitTests/Payments/**`, `docs/spec/11*` (incl. SEPAY-DIFFS), `docs/ops/payments-runbook.md` | payment-specialist |
| `web/src/{app,storefront,admin}/**`, `web/src/shared/{api,i18n,utils,hooks,icons}/**`, `web/package.json`, `web/vite.config.ts`, `web/tsconfig*`, `web/eslint.config.*` | frontend-developer |
| `docs/design/**`, `web/src/shared/{tokens,ui,design-demo}/**`, `web/src/styles/**`, `web/public/fonts/**`, `docs/spec/08-*` | ui-ux-designer |
| `tests/GroceryHub.IntegrationTests/**`, `tests/GroceryHub.ArchitectureTests/**`, `web/e2e/**`, `docs/qa/**`, `docs/spec/12-*` | qa-engineer |
| `docker-compose*.yml`, `**/Dockerfile`, `.dockerignore`, `deploy/{nginx,*}` (except db/security), `.github/workflows/**`, `.env.example`, `docs/ops/**` (except `payments-runbook.md`), `docs/spec/13-*`, `scripts/**` (except verify-reality) | devops-engineer |
| `docs/security/**`, `.gitleaks.toml`, `.github/dependabot.yml`, `deploy/security/**` | security-reviewer |
| `docs/evidence/**` | author of each evidence file; PM aggregates |
Shared files rule: nobody edits a file they do not own. Needed change → **Change Request** note `docs/agent-notes/CR-<nnn>-<slug>.md` (template §8) addressed to the owner; owner implements or rejects with reason within the same phase.

## 4. Parallel vs sequential
Legend: ‖ can run in parallel (separate branches/worktrees), → must follow.
| Phase | Order |
|---|---|
| 0 Audit | PM (single) |
| 1 Research & Requirements | PM → (ui-ux-designer ‖ security-reviewer threat model ‖ payment-specialist SePay re-verification) → PM consolidates |
| 2 Agents/Skills/Rules | PM installs → devops (tool smoke test) ‖ QA (verify-reality) → PM records mode |
| 3 Architecture & DB design | backend (arch) → database-engineer (ERD detail) ‖ payment-specialist (payment design) → security-reviewer reviews → PM |
| 4 Backend foundation | devops (repo/CI/compose skeleton) ‖ backend (solution, SharedKernel, Api host) → database-engineer (DbContext, outbox, seeds) → QA (arch tests, integration harness) → security review |
| 5 Auth & RBAC | backend (Identity module) ‖ database-engineer (identity schema) → QA tests → security-reviewer |
| 6 Products/Category/Inventory | database-engineer (schema) → backend (catalog, inventory, purchasing) ‖ ui-ux-designer (tokens/recipes) → QA |
| 7 Cart & Order | backend (cart, ordering, promotions, shipping) ‖ database-engineer → QA concurrency tests → security review |
| 8 Customer frontend | ui-ux-designer (tokens/primitives) → frontend (storefront) ‖ backend fixes via CR → QA E2E → UI review |
| 9 Admin dashboard | ui-ux-designer (admin recipes) → frontend (admin) ‖ backend (reports/admin endpoints) → QA |
| 10 SePay | payment-specialist → backend (order contracts) → frontend (payment page) → QA → security-reviewer (mandatory) |
| 11 Testing & Security | QA ‖ security-reviewer → owners fix → re-test |
| 12 Docker & Deployment | devops → security-reviewer ‖ QA smoke → PM |
| 13 Final review | PM runs full gate; QA clean-checkout run; security final; traceability complete |

## 5. Reviewer matrix (author ≠ reviewer)
| Author | Reviewer(s) |
|---|---|
| backend-developer | qa-engineer (tests), security-reviewer (auth/payments/uploads), PM (integration) |
| database-engineer | backend-developer (domain fit), qa-engineer (concurrency tests) |
| payment-specialist | security-reviewer (mandatory), qa-engineer |
| frontend-developer | ui-ux-designer (`/wf-ui-review`), qa-engineer (E2E/a11y), security-reviewer (token handling) |
| ui-ux-designer | frontend-developer (buildability), qa-engineer (a11y) |
| devops-engineer | security-reviewer, qa-engineer (smoke) |
| qa-engineer | backend/frontend owners (test realism), PM |
| security-reviewer | PM (finding triage; the user for risk acceptance) |

## 6. Integration responsibility and branching
- PM is the **only** integrator. Branch naming: `agent/<role>/<phase>-<slug>` → merged into `integration/phase-XX` → after the gate into `main` (squash or merge commit, Conventional Commits).
- Integration procedure: (1) merge in dependency order; (2) `dotnet build -warnaserror && dotnet test` and `npm ci && npm run lint && npm run typecheck && npm test && npm run build`; (3) `scripts/verify-reality`; (4) record evidence; (5) tag `phase-XX-done` only after GO.
- Parallel work uses separate git worktrees (subagent `branch` mode, or `git worktree add ../GroceryHub-<role> agent/<role>/...` in role-adoption mode).

## 7. Conflict handling
1. Prevention: ownership map + CR notes + contract-first (OpenAPI).
2. Merge conflict in a file → the **file owner** resolves; PM arbitrates ownership disputes by §3.
3. Contract conflict (API shape) → backend-developer decides; frontend adapts; recorded in the CR.
4. Requirement conflict → PM asks the user; decision recorded in `docs/progress/DECISIONS.md`.
5. Two agents claim different facts → run the real command/test; evidence wins.

## 8. Templates
**Delegation message (PM → agent):**
```
Phase: <id>   Task: <title>   Branch: agent/<role>/<phase>-<slug>
Goal: <one paragraph>
Requirements: <FR/NFR/BR ids>
Inputs: <docs, contracts, previous outputs>
Owned paths: <list>   Forbidden paths: everything else
Acceptance criteria: <checkable list>
Evidence required: <commands + files under docs/evidence/phase-XX/>
Skills to load: <names>
Report back with: summary, commits, evidence links, open questions.
```
**Change Request (`docs/agent-notes/CR-<nnn>-<slug>.md`):** From, To (owner), Date, Reason, Exact change requested (file/endpoint/schema), Impact, Blocking? (Y/N), Status (Open/Accepted/Rejected/Done), Resolution commit.

## 9. Progress management
- `docs/progress/PROGRESS.md`: table of phases (status, started, GO date, evidence link).
- `docs/progress/phase-XX-plan.md` and `phase-XX-report.md` per phase.
- `docs/progress/TRACEABILITY.md`: FR/NFR/BR → code path → test name → evidence file → status (`NotStarted/InProgress/Implemented/Verified`). Only QA/PM may set `Verified`, and only with a real test run.
- `docs/progress/QUESTIONS.md`, `DECISIONS.md`, `DEPENDENCIES.md`, `COMMANDS.md` (exact build/test/run commands as they become known).
- Weekly/each-phase `/wf-progress-review`.

## 10. Anti-fabrication controls (system-wide)
Evidence-or-it-didn't-happen; independent reviewers; clean-checkout gate runs; `verify-reality` scans; mutation sanity checks in QA; Teamwork `Auditor`-style checks when available; the PM must never accept an agent's summary without opening the code and the command output.
