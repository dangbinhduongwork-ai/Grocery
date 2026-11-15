# Phase 01 Report — Research and Requirements

- **Phase:** `01` (Research and Requirements)
- **Date:** `2026-11-15`
- **Lead Agent:** `project-manager`
- **Role Agents Executed in Parallel (`share` worktrees):**
  - `ui-ux-designer` — branch `agent/ui-ux-designer/01-references-and-fonts` (commit `32015d1`)
  - `payment-specialist` — branch `agent/payment-specialist/01-sepay-diffs` (commit `5388660`)
  - `security-reviewer` — branch `agent/security-reviewer/01-threat-model` (commit `5cae719`)
- **Integration Branch:** `integration/phase-01`
- **Gate Status:** **READY FOR GO** (All technical deliverables and verification evidence are complete and passing; awaiting user confirmation of `Q-001`..`Q-011` or acceptance of proposed defaults in `docs/progress/DECISIONS.md` to finalize `GO` and tag `phase-01-done`).

---

## 1. Deliverables & File Ownership Audit (`/wf-progress-review`)

| Task | Owner | Branch & Commit | Files Modified (Verified via `git diff --name-status`) | Independent Review |
|---|---|---|---|---|
| **P01-T1: UI/UX Reference Repos, Grocery UX Study, Font & Contrast Math** | `ui-ux-designer` | `agent/ui-ux-designer/01-references-and-fonts` (`32015d1`) | `docs/design/references.md`<br>`docs/design/font-check.md`<br>`docs/design/contrast.md`<br>`docs/evidence/phase-01/ui-ux-github-api.txt`<br>`docs/evidence/phase-01/ui-ux-font-contrast.txt` | Reviewed by `project-manager` (spot-checked GitHub API commits, npm packages, and WCAG luminance math — **PASS**) |
| **P01-T2: Official SePay Documentation Re-verification** | `payment-specialist` | `agent/payment-specialist/01-sepay-diffs` (`5388660`) | `docs/spec/11-SEPAY-DIFFS.md`<br>`docs/evidence/phase-01/sepay-docs-check.txt` | Reviewed by `project-manager` & mapped to `security-reviewer` threat model (`TM-PAY-01..08` — **PASS**) |
| **P01-T3: STRIDE Threat Model Across All Modules** | `security-reviewer` | `agent/security-reviewer/01-threat-model` (`5cae719`) | `docs/security/threat-model.md`<br>`docs/evidence/phase-01/security-threat-model-check.txt` | Reviewed by `project-manager` (`31` Threat IDs across 5 modules, `58` mapped requirement IDs — **PASS**) |
| **P01-T4 & T5: Requirements Audit, Coverage Matrix, Decisions & Integration** | `project-manager` | `integration/phase-01` | `docs/progress/phase-01-plan.md`<br>`docs/progress/QUESTIONS.md`<br>`docs/progress/DECISIONS.md`<br>`docs/progress/phase-01-report.md`<br>`docs/spec/02-PROJECT-REQUIREMENTS.md`<br>`docs/spec/03-TECH-STACK-RESEARCH.md`<br>`docs/spec/04-GITHUB-UI-RESEARCH.md`<br>`docs/evidence/phase-01/pm-requirements-coverage.txt` | Verified by `scripts/verify-reality.ps1` (**PASS**) |

---

## 2. Key Research Findings Verified by Evidence

1. **GitHub Reference Repositories (`docs/design/references.md`, `ui-ux-github-api.txt`):**
   - Queried live GitHub REST API for all 5 repos:
     - `Giogfr/aura-storefront`: `7b36904949f7` (`2026-10-17`), `MIT`, React 19 + Vite 8 + Tailwind v4.
     - `Kiranism/next-shadcn-dashboard-starter`: `47da494b89e3` (`2026-11-02`), `MIT`, 7,275 ★ (`v2.0.0` moved to `@base-ui/react`, `v1.0.0` Radix UI).
     - `masondevx/orbynadmin`: `6c2796f7baf7` (`2026-07-17`), `MIT`, 19 ★, Next.js 16 + Radix `shadcn/ui`.
     - `shadcnstore/shadcn-dashboard-landing-template` (redirected from `silicondeck/...`): `866423741fa4` (`2026-10-22`), `MIT` (`License.md`), 1,264 ★.
     - `satnaing/shadcn-admin`: `e16c87f213a5` (`2026-06-11`, tag `v2.2.1`), `MIT`, 16,711 ★, Vite + React 19 + `@tanstack/react-router` + `@tanstack/react-query` + `@tanstack/react-table` + Tailwind v4 + 18 `@radix-ui/react-*` primitives.
   - **`shadcn/ui` Variant Decision (`DEC-004`):** Standardized on **Radix UI** (`@radix-ui/react-*` / `radix-ui`) to match `satnaing/shadcn-admin` and ensure WCAG 2.2 AA + Vietnamese IME (`compositionstart`/`compositionend`) stability.

2. **Vietnamese Font & WCAG 2.2 Contrast Verification (`docs/design/font-check.md`, `docs/design/contrast.md`, `ui-ux-font-contrast.txt`):**
   - Both **Be Vietnam Pro** (`400, 500, 600, 700`) and **Fraunces** (`opsz 9..144`, `600, 700`) ship complete `vietnamese` subsets (`U+0102-0103, U+0110-0111, U+0128-0129, U+0168-0169, U+01A0-01A1, U+01AF-01B0, U+1EA0-1EF9, U+20AB`) covering 100% of `Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ ữ ế 25.000 ₫`.
   - Verified npm packages: `@fontsource/be-vietnam-pro@5.3.0` (`OFL-1.1`, static) and `@fontsource-variable/fraunces@5.3.0` (`OFL-1.1`, variable). Note that `@fontsource-variable/be-vietnam-pro` returns HTTP 404 on npm because Be Vietnam Pro is static upstream.
   - Contrast math surfaced two concrete token guardrails:
     - Solid destructive buttons with white text must use `--tomato-ink` (`#B8321F`, `5.98:1` vs `#FFFFFF`) rather than `--tomato` (`#D9432F`, `4.38:1`).
     - Added companion token `--warn-ink` (`#8A560D`, `5.74:1` on `--paper`, `6.14:1` on `#FFFFFF`) for small warning text labels alongside `--warn` (`#B7791F`, `3.40:1` icon/border).

3. **Official SePay Documentation Diffs (`docs/spec/11-SEPAY-DIFFS.md`, `sepay-docs-check.txt`):**
   - Checked 41 official SePay URLs (`developer.sepay.vn` HTML & `.md`, `docs.sepay.vn`, `qr.sepay.vn/banks.json`).
   - Confirmed **HMAC-SHA256** (`X-SePay-Signature: sha256={hex}` + `X-SePay-Timestamp` over `{timestamp}.{raw_body}`) is **natively supported and recommended by SePay**, alongside `Authorization: Apikey <key>` and OAuth 2.0.
   - **Critical Discovery 1 (Payment Code Suffix Length):** SePay's payment code structure defaults to suffix length `6–8` digits. Because GroceryHub uses `GH` + **10 digits** (`GH1000000001`), the user must set Min=`10`, Max=`10` in SePay Dashboard (**Cấu hình Công ty → Cấu hình chung → Cấu trúc mã thanh toán**).
   - **Critical Discovery 2 (Reconciliation API v2 UUID vs Webhook Numeric ID):** SePay API v2 (`https://userapi.sepay.vn/v2/transactions` & Sandbox `https://userapi-sandbox.sepay.vn/v2/transactions`, rate limit `3 req/s`) returns a **UUID** `id`, whereas webhooks send a **numeric integer** `id`. Therefore, `ReconciliationService` must deduplicate not only by `WebhookEvents.ProviderTransactionId == tx.Id` but also by `PaymentTransactions.ReferenceCode == tx.ReferenceNumber` (plus `AccountNumber` + `TransferAmount`) or already-`Paid` `PaymentCode` so webhook-confirmed orders are not falsely flagged as `DuplicatePayment`.
   - **Critical Discovery 3 (Sandbox / Test Mode):** `my.dev.sepay.vn` is defunct; SePay now provides a built-in **Test mode** toggle directly inside the left sidebar of `https://my.sepay.vn`.

4. **STRIDE Threat Model (`docs/security/threat-model.md`, `security-threat-model-check.txt`):**
   - Defines 4 trust boundaries (`TB-01`..`TB-04`), 8 critical/high assets (`AST-01`..`AST-08`), 5 threat actors (`TA-01`..`TA-05`), and **31 Threat IDs** (`TM-ID-01..06`, `TM-ORD-01..07`, `TM-PAY-01..08`, `TM-UPL-01..04`, `TM-ADM-01..06`) mapped to `58` requirement IDs and phase-gate tests (`Phases 04..12`).

---

## 3. Acceptance Criteria Verification

| # | Acceptance Criterion | Result | Evidence Link |
|---|---|---|---|
| 1 | `references.md` has per-repo licence/stack/last-commit facts that were actually read (links included); font check evidence exists. | **PASS** | `docs/design/references.md`, `docs/design/font-check.md`, `docs/design/contrast.md`, `docs/evidence/phase-01/ui-ux-github-api.txt`, `ui-ux-font-contrast.txt` |
| 2 | `11-SEPAY-DIFFS.md` exists (with the list of pages opened, short quotes, URLs, and diffs). | **PASS** | `docs/spec/11-SEPAY-DIFFS.md`, `docs/evidence/phase-01/sepay-docs-check.txt` |
| 3 | Threat model covers all modules listed (`Identity`, `Cart/Order`, `Payments webhook`, `Uploads`, `Admin`); each threat links to a control and a requirement ID. | **PASS** | `docs/security/threat-model.md`, `docs/evidence/phase-01/security-threat-model-check.txt` |
| 4 | Requirement coverage table (user's original feature list → FR IDs) shows no uncovered item (`0` uncovered across `68` IDs). | **PASS** | `docs/progress/QUESTIONS.md` §3, `docs/evidence/phase-01/pm-requirements-coverage.txt` |
| 5 | Decisions `DEC-001`..`DEC-011` recorded with status `CONFIRMED` / `PROPOSED - awaiting user confirmation`. | **PASS** | `docs/progress/DECISIONS.md` |
