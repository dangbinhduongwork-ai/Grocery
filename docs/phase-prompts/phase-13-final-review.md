# Phase 13 — Final Review and Handover

**Goal:** Independently verify the entire product against the requirements, produce the final report and handover documentation. Nothing new is built.
**Agents:** project-manager (lead), qa-engineer (clean-checkout run), security-reviewer (final), ui-ux-designer (final UI review), devops-engineer (deployment rehearsal).
**Inputs:** everything (FR-CUS-018 wishlist is a 'Could' item: report it as Deferred unless the user approved it); TRACEABILITY.md; all phase reports.
**Outputs:** `docs/progress/FINAL-REPORT.md`, final TRACEABILITY with evidence, `docs/security/review-final.md`, `docs/ops/handover.md`, `README.md` (project), release tag `v1.0.0`, known-issues list.
**Dependencies:** Phase 12 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 13 — FINAL REVIEW. Assume nothing is done until you see it run. Load skill wf-progress-review, wf-code-review, wf-security-review, wf-ui-review.

1. Requirements audit: for every FR/NFR/BR in docs/spec/02 produce a row: ID | implemented in (file path) | test (name) | evidence file | verdict (Verified/Partial/Missing). Do not copy from earlier reports: re-run the relevant test now or re-observe the behaviour. List every Partial/Missing item with owner and effort.
2. Clean-checkout run (qa-engineer, different folder/worktree): docker compose up -d --build; migrations; smoke test; dotnet test (all projects); npm ci + lint + typecheck + unit tests + build; Playwright J1..J7; a11y; record exit codes and summaries.
3. Reality audit: scripts/verify-reality; grep for NotImplementedException, TODO, FIXME, skipped tests, hard-coded data, client-side paid paths, tokens in storage, emoji in UI, banned packages; inspect every hit.
4. Independent code review of the highest-risk areas by someone other than the author: ordering/inventory transactions, payments webhook, auth, authorization policies, uploads, reports (money math).
5. Final security review (security-reviewer): full /wf-security-review, scans, RBAC matrix for every endpoint, payments negatives, config review of the production compose; retest all earlier findings.
6. Final UI review (ui-ux-designer): /wf-ui-review for all pages at 4 widths; anti-AI-look checklist; accessibility.
7. Business-flow rehearsal on the compose stack, recorded step by step (screenshots + HTTP logs): guest -> register -> COD order -> staff fulfilment -> delivered -> review -> manager approves; QR order with signed webhook; underpaid anomaly + refund with two users; stock receipt; reports match fixtures.
8. Deployment rehearsal (devops-engineer): follow docs/ops/deployment-guide.md literally on a fresh environment (or the user's staging), fix the guide where it was wrong, and write docs/ops/handover.md.
9. Write docs/progress/FINAL-REPORT.md: executive summary (Vietnamese), scope delivered vs out-of-scope, verification table, test totals, security status, known issues and risks, operational notes, what the user must do (secrets, domain, SePay production switch, SQL licence), and next-step recommendations. Tag v1.0.0 ONLY if every Must requirement is Verified and there are no open Critical/High findings; otherwise write a NO-GO with a punch list.
```

## Acceptance criteria
- Every Must requirement `Verified` with fresh evidence from this phase, or the report is NO-GO.
- Clean-checkout and rehearsal evidence present; handover documentation tested by following it literally.
- No open Critical/High security findings; all earlier findings retested.
- Final report is truthful: it lists failures and limitations plainly.

## How to verify
Read FINAL-REPORT.md; randomly select 10 Verified rows and re-run their tests; follow handover.md on a fresh machine/folder.

## Gate
GO = tag `v1.0.0` and handover to the user. NO-GO = punch list with owners; return to the relevant phase prompt.
