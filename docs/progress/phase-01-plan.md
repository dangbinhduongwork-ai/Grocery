# Phase 01 Plan — Research and Requirements

- **Phase:** 01
- **Date:** 2026-11-15
- **Lead Agent:** `project-manager`
- **Parallel Role Agents:** `ui-ux-designer`, `payment-specialist`, `security-reviewer`
- **Integration Branch:** `integration/phase-01` → merged to `main` after gate `GO`
- **Dependencies:** Phase 00 `GO` (`phase-00-done`)

## 1. Tasks, Ownership, Branches, and Execution Order

Legend: `‖` runs in parallel in isolated `share` git worktrees; `→` runs sequentially after dependencies.

| ID | Task | Owner | Order | Branch | Owned Paths | Evidence Expected |
|---|---|---|---|---|---|---|
| P01-T1 | Reference Repos, Grocery UX Study, Font Vietnamese Glyph Check & Contrast Verification | `ui-ux-designer` | 1A (`‖`) | `agent/ui-ux-designer/01-references-and-fonts` | `docs/design/references.md`, `docs/design/font-check.md`, `docs/design/contrast.md`, `docs/evidence/phase-01/ui-ux-*.txt` | Real GitHub API/web checks for all 5 repos (`aura-storefront`, `next-shadcn-dashboard-starter`, `orbynadmin`, `shadcn-dashboard-landing-template`, `satnaing/shadcn-admin`), 4 grocery sites analysis, Fontsource/Google Fonts `vietnamese` subset + test string verification (`Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ ữ ế 25.000 ₫`), WCAG contrast math |
| P01-T2 | Official SePay Documentation Re-verification (`docs/spec/11` §10) | `payment-specialist` | 1B (`‖`) | `agent/payment-specialist/01-sepay-diffs` | `docs/spec/11-SEPAY-DIFFS.md`, `docs/evidence/phase-01/sepay-*.txt` | Real fetches/searches of official `developer.sepay.vn` / `docs.sepay.vn` pages (webhooks overview, integrate, create, auth, security, retry/error handling, reconciliation, QR generation, test mode, transactions API) with short quotes, URLs, and explicit diff table vs `docs/spec/11-SEPAY-INTEGRATION.md` |
| P01-T3 | STRIDE Threat Model per Module (`Identity`, `Cart/Order`, `Payments Webhook`, `Uploads`, `Admin`) | `security-reviewer` | 1C (`‖`) | `agent/security-reviewer/01-threat-model` | `docs/security/threat-model.md`, `docs/evidence/phase-01/security-*.txt` | Complete STRIDE analysis per module with assets, actors, abuse cases, and required controls mapped to `FR-*`/`NFR-*`/`BR-*` IDs |
| P01-T4 | Line-by-line Requirements Review (`docs/spec/02`), Feature Coverage Table, Open Questions & `shadcn/ui` Variant Decision | `project-manager` | 2 (`→`) | `integration/phase-01` | `docs/progress/QUESTIONS.md`, `docs/progress/DECISIONS.md`, `docs/spec/02-PROJECT-REQUIREMENTS.md`, `docs/spec/04-GITHUB-UI-RESEARCH.md`, `docs/evidence/phase-01/pm-*.txt` | Line-by-line gap/ambiguity audit, original feature list → `FR-*` coverage matrix, user Q&A for the 5 open questions + gaps, Radix vs Base UI decision recorded |
| P01-T5 | Branch Integration, Cross-Review, Reality Scan & `/wf-progress-review` Gate Report | `project-manager` | 3 (`→`) | `integration/phase-01` → `main` | `docs/progress/phase-01-report.md`, `docs/progress/PROGRESS.md`, `docs/progress/TRACEABILITY.md`, `docs/evidence/phase-01/gate-run.txt` | Clean merge of all 3 role branches, spot-check verification of GitHub & SePay facts, `scripts/verify-reality.ps1` clean run, `phase-01-report.md` |

## 2. Definition of Done (Phase 01)
1. `docs/design/references.md` has per-repo license, stack, and last-commit facts verified from live GitHub API/web with URLs; `docs/design/font-check.md` (and `docs/design/contrast.md`) exist with real verification evidence.
2. `docs/spec/11-SEPAY-DIFFS.md` exists with the list of official SePay pages opened, short quotes, URLs, and every difference vs `docs/spec/11-SEPAY-INTEGRATION.md` documented.
3. `docs/security/threat-model.md` covers all 5 target areas (`Identity`, `Cart/Order`, `Payments webhook`, `Uploads`, `Admin`) using STRIDE, linking every threat to a concrete control and requirement ID.
4. Requirement coverage table (original feature list → `FR-*` IDs) shows 0 uncovered items; any gaps/ambiguities found in `docs/spec/02-PROJECT-REQUIREMENTS.md` are recorded in `docs/progress/QUESTIONS.md`.
5. The user is asked the 5 open questions from `docs/spec/02` §8 plus any Phase 01 review questions in a single numbered Vietnamese message with suggested defaults, and answers/defaults are recorded in `docs/progress/DECISIONS.md` along with the `shadcn/ui` Radix vs Base UI decision.
