# Phase 01 — Research and Requirements

**Goal:** Confirm research (repos, stack, SePay, design references), close open questions with the user, freeze requirements v1 and threat model.
**Agents:** project-manager (lead), ui-ux-designer, payment-specialist, security-reviewer.
**Inputs:** `docs/spec/02`, `03`, `04`, `08`, `11`.
**Outputs:** updated specs, `docs/progress/DECISIONS.md` (answers to open questions), `docs/design/references.md`, `docs/security/threat-model.md`, `docs/spec/11` diff notes, `docs/progress/phase-01-report.md`.
**Dependencies:** Phase 00 GO.

## Prompt to paste into Antigravity (main agent = project-manager)
```text
You are the Project Manager. Read AGENTS.md and docs/spec/02-PROJECT-REQUIREMENTS.md, 03, 04, 08, 11.

PHASE 01 — RESEARCH AND REQUIREMENTS. No application code.

A. Delegate in parallel (separate branches agent/<role>/phase-01-*; if subagents are unavailable run the roles one conversation at a time using each role file):
 1. ui-ux-designer: re-open the four reference repos in docs/spec/04 (aura-storefront, next-shadcn-dashboard-starter, orbynadmin, shadcn-dashboard-landing-template) plus satnaing/shadcn-admin; record for each: last commit date, licence file, stack, what to reuse / avoid. Study 4 public grocery sites (Vietnamese and international) and write observations (patterns only; no assets copied). Verify fonts: Be Vietnam Pro and Fraunces contain Vietnamese glyphs; render the test string and keep evidence. Output docs/design/references.md and docs/design/font-check.md. Do not copy repo code.
 2. payment-specialist: re-read ALL official SePay pages listed in docs/spec/11 §10 (webhook quick start, integrate, create, authentication, security, error handling, reconciliation, QR, test mode, transactions API). Produce docs/spec/11-SEPAY-DIFFS.md listing any difference vs doc 11 (header names, retry schedule, response contract, QR params, API endpoints). Quote only short phrases, cite URLs. Do not guess; if a page cannot be opened, say so.
 3. security-reviewer: write docs/security/threat-model.md (STRIDE per module: Identity, Cart/Order, Payments webhook, uploads, admin) with assets, actors, abuse cases, required controls mapped to requirement IDs.
B. Requirements review (you): read docs/spec/02 line by line. List gaps, contradictions, ambiguous items. Check every item from the user's original feature list is represented by at least one FR ID (customer, admin, staff, roles). Write the gap list to docs/progress/QUESTIONS.md.
C. Ask the user the five open questions in docs/spec/02 §8 plus anything from B, in Vietnamese, in ONE message with numbered questions and suggested defaults. Wait for answers; record them verbatim in docs/progress/DECISIONS.md (DEC-001...). If the user is unavailable, record the proposed defaults as "PROPOSED - not confirmed" and flag them in the report.
D. Update docs/spec files affected by decisions (via owners' CR notes). Decide Radix vs Base UI variant of shadcn/ui and record it.
E. Run /wf-progress-review and write docs/progress/phase-01-report.md. Finish with a Vietnamese summary.
```

## Acceptance criteria
- `references.md` has per-repo licence/stack/last-commit facts that were actually read (links included); font check evidence exists.
- `11-SEPAY-DIFFS.md` exists (even if "no differences found", with the list of pages opened).
- Threat model covers all modules listed; each threat links to a control and a requirement ID.
- Requirement coverage table (user's original feature list → FR IDs) shows no uncovered item.
- Decisions DEC-001+ recorded with status Confirmed/Proposed.

## How to verify
Open each output file; spot-check two repo facts on GitHub; spot-check two SePay facts against the official page; check the coverage table against the original brief.

## Gate to Phase 02
User answered (or explicitly accepted defaults for) the five open questions; requirements frozen as v1.0 with change-control note; PM GO.
