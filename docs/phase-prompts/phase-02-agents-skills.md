# Phase 02 — Agents, Skills, Rules (install and prove they work)

**Goal:** Prove, on the user's actual Antigravity installation, that rules load, skills are discoverable, custom subagents (if supported) can be invoked, and the role-adoption fallback works. Record the execution mode.
**Agents:** project-manager (lead), devops-engineer (tool test), qa-engineer (verify-reality test).
**Inputs:** `.agents/**`, `AGENTS.md`, spec 09 and 10.
**Outputs:** `docs/progress/DECISIONS.md` entry "Execution mode", `docs/evidence/phase-02/*`, fixes to `.agents/**` if needed, `docs/progress/phase-02-report.md`.
**Dependencies:** Phase 01 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. Read AGENTS.md, docs/spec/09-AGENT-ORCHESTRATION.md and 10-SKILLS-AND-RULES.md. Phase 02 proves the agent system works. Be honest about what does not work; do not claim features you did not observe.

1. RULES: Ask yourself (without opening files) which rules are active for this conversation, then list .agents/rules and verify each file has valid frontmatter (trigger spelled exactly always_on|model_decision|glob|manual; globs/description present where required). Open the Customizations > Rules panel (or CLI equivalent) and report whether all 7 rules appear. Test: ask a backend question touching a .cs file path and report whether backend rules were applied; test the security always_on rule.
2. SKILLS: List .agents/skills; verify 14 folders each with SKILL.md having name+description. Report which skills Antigravity lists. Invoke /wf-progress-review in a dry-run (no changes) to prove slash invocation. Report the exact behavior.
3. SUBAGENTS: Check the Antigravity docs page "Custom subagents" and your surface. If custom subagents are available: run /agents and report whether the 9 agents are listed. Smoke test 1: invoke_subagent backend-developer with the task "Create file docs/evidence/phase-02/hello-backend.txt containing the output of `dotnet --version` and nothing else; do not touch other files." Verify the file content yourself. Smoke test 2: invoke qa-engineer to run scripts/verify-reality and report. Smoke test 3 (isolation): invoke two subagents with workspace mode `branch` to create different files in different branches; verify no overlap; merge. If a subagent hangs or errors, check the `tools` frontmatter note in the agent file, report the exact error, and do not add tool names that are not documented.
   If custom subagents are NOT available on this surface: switch to ROLE-ADOPTION MODE: demonstrate it by starting (or simulating via a new conversation instructed by the user) a role conversation with the prompt "Adopt the role defined in .agents/agents/backend-developer.md ..." and run Smoke test 1 in that conversation.
3b. VERIFY-REALITY SELF-TEST: install ripgrep if missing (tell the user the winget command if you cannot). Create a temporary file src/Tmp/Fake.cs containing `throw new NotImplementedException();` and a temporary web/src/tmp.ts containing `localStorage.setItem('token', t)`, run scripts/verify-reality.ps1 (Windows) or .sh and confirm BOTH are detected and the exit code is non-zero; delete the temp files and confirm the script then reports no violations. Fix the script if it misses them (PM owns it).
4. WORKTREES: create two git worktrees (agent/backend-developer/phase-02-test, agent/qa-engineer/phase-02-test), commit a different file in each, merge both into integration/phase-02, remove worktrees. Save command output.
5. DECISION: write in docs/progress/DECISIONS.md: "Execution mode = A (subagents) | B (role adoption)", the Antigravity surface/version, which of /teamwork, /swarm are available on the user's plan (do not call them unless the user agrees), and any limitation found.
6. FIX anything wrong in .agents/** (you own it) via small commits; re-run the failing test to prove the fix.
7. Write docs/progress/phase-02-report.md with a table: mechanism | expected | observed | evidence | PASS/FAIL. Finish with a Vietnamese how-to for the user: "how to start a role conversation / invoke a subagent / run a review".
```

## Acceptance criteria
- Table with observed behavior for rules, skills, slash invocation, subagents (or documented fallback), worktrees.
- Execution mode recorded.
- At least one successful end-to-end delegation (subagent or role conversation) with a verified file artifact.
- No rule/skill/agent file has invalid frontmatter.
- `verify-reality` self-test detected the two planted violations and then passed clean.

## How to verify
Read the report; open the evidence files; run `/agents` yourself; ask "What rules are active?" and compare.

## Gate to Phase 03
Execution mode chosen and proven; any broken customization fixed; PM GO.
