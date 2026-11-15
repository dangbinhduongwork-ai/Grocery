# GroceryHub — Vietnamese Typography & Self-Hosted Font Verification (`font-check.md`)

- **Verification Date:** `2026-11-15T03:11:55Z`
- **Requirements Covered:** `NFR-002` (Performance/Self-hosted assets), `NFR-006` (Accessibility), `NFR-007` (Vietnamese UI localization)
- **Design System Spec:** [08-UI-DESIGN-SYSTEM.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer--Phase-01-Research--self-aca95d94/docs/spec/08-UI-DESIGN-SYSTEM.md) §3 (Typography)
- **Raw Verification Output:** [ui-ux-font-contrast.txt](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer--Phase-01-Research--self-aca95d94/docs/evidence/phase-01/ui-ux-font-contrast.txt)

---

## 1. Executive Summary

Both typefaces specified in `docs/spec/08-UI-DESIGN-SYSTEM.md` §3 — **Be Vietnam Pro** (UI & body text, weights `400`, `500`, `600`, `700`) and **Fraunces** (editorial display serif, optical size `9..144`, weights `600`, `700`) — were verified against the live **Google Fonts CSS2 API**, the **Fontsource Metadata API**, and the **npm registry**:

1. **Vietnamese Subset Exists & Is Complete:** Both **Be Vietnam Pro** and **Fraunces** ship an explicit `vietnamese` subset alongside `latin-ext` and `latin`.
2. **100% Test String Codepoint Coverage:** Every character and combined vowel+tone mark in the mandatory test string:
   ```text
   Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ ữ ế 25.000 ₫
   ```
   plus supplementary Vietnamese letters (`ă`, `Ă`, `đ`, `Ơ`) is covered within the verified `unicode-range` blocks of both **Be Vietnam Pro** and **Fraunces** across all required weights (`400`, `500`, `600`, `700` for Be Vietnam Pro; `600`, `700` for Fraunces).
3. **Verdict on Display Serif (`Fraunces`):** **APPROVED.** Because `Fraunces` natively includes the complete `vietnamese` subset (`U+0102-0103, U+0110-0111, U+0128-0129, U+0168-0169, U+01A0-01A1, U+01AF-01B0, U+1EA0-1EF9, U+20AB`), no fallback to Be Vietnam Pro 700 is needed for editorial headlines.
4. **Permissive License & Self-Hosting (`Fontsource`):** Both fonts are licensed under **SIL Open Font License 1.1 (`OFL-1.1`)** and available as self-hosted `.woff2` packages on npm via `@fontsource/be-vietnam-pro@5.3.0` and `@fontsource-variable/fraunces@5.3.0` (or `@fontsource/fraunces@5.3.0`).

---

## 2. Live API & Registry Verification

### 2.1 Google Fonts CSS2 API Verification
- **Endpoint Queried:**
  `https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700&family=Fraunces:opsz,wght@9..144,600;9..144,700&display=swap`
- **HTTP Status:** `200 OK`

| Font Family | Verified Weights Returned | Subsets Returned | `vietnamese` Subset `unicode-range` |
|---|---|---|---|
| **Be Vietnam Pro** | `400`, `500`, `600`, `700` | `vietnamese`, `latin-ext`, `latin` | `U+0102-0103, U+0110-0111, U+0128-0129, U+0168-0169, U+01A0-01A1, U+01AF-01B0, U+0300-0301, U+0303-0304, U+0308-0309, U+0323, U+0329, U+1EA0-1EF9, U+20AB` |
| **Fraunces** | `600`, `700` (`opsz 9..144`) | `vietnamese`, `latin-ext`, `latin` | `U+0102-0103, U+0110-0111, U+0128-0129, U+0168-0169, U+01A0-01A1, U+01AF-01B0, U+0300-0301, U+0303-0304, U+0308-0309, U+0323, U+0329, U+1EA0-1EF9, U+20AB` |

Full `unicode-range` definitions per subset returned by Google Fonts CSS2 and Fontsource API:
- **`vietnamese`**: `U+0102-0103, U+0110-0111, U+0128-0129, U+0168-0169, U+01A0-01A1, U+01AF-01B0, U+0300-0301, U+0303-0304, U+0308-0309, U+0323, U+0329, U+1EA0-1EF9, U+20AB`
- **`latin-ext`**: `U+0100-02BA, U+02BD-02C5, U+02C7-02CC, U+02CE-02D7, U+02DD-02FF, U+0304, U+0308, U+0329, U+1D00-1DBF, U+1E00-1E9F, U+1EF2-1EFF, U+2020, U+20A0-20AB, U+20AD-20C4, U+2113, U+2C60-2C7F, U+A720-A7FF`
- **`latin`**: `U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD`

### 2.2 Fontsource & npm Registry Verification (Self-Hosting)

Per `AGENTS.md` §2.6 (verify package existence before recommending), we queried the npm registry (`https://registry.npmjs.org/<pkg>/latest`) and Fontsource API (`https://api.fontsource.org/v1/fonts/<id>`):

| Package Name | npm HTTP Status | Latest Version | License | Font Type (`api.fontsource.org`) | Subsets Available | Notes for `web/package.json` |
|---|---|---|---|---|---|---|
| `@fontsource/be-vietnam-pro` | `200 OK` | `5.3.0` | `OFL-1.1` | Static (`variable: false`), weights `100..900` | `["latin", "latin-ext", "vietnamese"]` | **Use this package** for Be Vietnam Pro (`400.css`, `500.css`, `600.css`, `700.css`). |
| `@fontsource-variable/be-vietnam-pro` | `404 Not Found` | — | — | Not a variable font upstream | — | **Do NOT use** (does not exist on npm because Be Vietnam Pro is static upstream). |
| `@fontsource-variable/fraunces` | `200 OK` | `5.3.0` | `OFL-1.1` | Variable (`variable: true`), `opsz` + `wght` (`100..900`) | `["latin", "latin-ext", "vietnamese"]` | **Preferred for Fraunces** (single variable `.woff2` per subset covering `600` & `700` + `opsz`). |
| `@fontsource/fraunces` | `200 OK` | `5.3.0` | `OFL-1.1` | Static fallback (`100..900`) | `["latin", "latin-ext", "vietnamese"]` | Available if static weight files (`600.css`, `700.css`) are preferred. |

---

## 3. Codepoint-by-Codepoint Verification of the Vietnamese Test String

**Test String (`08-UI-DESIGN-SYSTEM.md` §3):**
`Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ ữ ế 25.000 ₫`

Every unique character in the test string (plus supplementary Vietnamese characters `ă`, `đ`, `Ă`, `Ơ`) was decomposed to its exact Unicode codepoint and matched against the `unicode-range` intervals of `Be Vietnam Pro` and `Fraunces`:

| Character | Unicode Codepoint | Decimal (UTF-16) | Character Name / Role | Matched `unicode-range` Interval | `Be Vietnam Pro` Subset(s) | `Fraunces` Subset(s) | Status |
|---|---|---|---|---|---|---|---|
| `R` | `U+0052` | 82 | Latin Capital Letter R | `U+0000-00FF` | `latin` | `latin` | PASS |
| `a` | `U+0061` | 97 | Latin Small Letter A | `U+0000-00FF` | `latin` | `latin` | PASS |
| `u` | `U+0075` | 117 | Latin Small Letter U | `U+0000-00FF` | `latin` | `latin` | PASS |
| `[SPACE]` | `U+0020` | 32 | Space | `U+0000-00FF` | `latin` | `latin` | PASS |
| `c` | `U+0063` | 99 | Latin Small Letter C | `U+0000-00FF` | `latin` | `latin` | PASS |
| **`ủ`** | **`U+1EE7`** | 7911 | Latin Small Letter U with Hook Above (*dấu hỏi*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| `q` | `U+0071` | 113 | Latin Small Letter Q | `U+0000-00FF` | `latin` | `latin` | PASS |
| **`ả`** | **`U+1EA3`** | 7843 | Latin Small Letter A with Hook Above (*dấu hỏi*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| `t` | `U+0074` | 116 | Latin Small Letter T | `U+0000-00FF` | `latin` | `latin` | PASS |
| **`ư`** | **`U+01B0`** | 432 | Latin Small Letter U with Horn | `U+01AF-01B0`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| **`ơ`** | **`U+01A1`** | 417 | Latin Small Letter O with Horn | `U+01A0-01A1`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| `i` | `U+0069` | 105 | Latin Small Letter I | `U+0000-00FF` | `latin` | `latin` | PASS |
| **`—`** | **`U+2014`** | 8212 | Em Dash | `U+2000-206F` | `latin` | `latin` | **PASS** |
| **`Đ`** | **`U+0110`** | 272 | Latin Capital Letter D with Stroke | `U+0110-0111`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| **`ờ`** | **`U+1EDD`** | 7901 | Latin Small Letter O with Horn and Grave (*ơ + huyền*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| `n` | `U+006E` | 110 | Latin Small Letter N | `U+0000-00FF` | `latin` | `latin` | PASS |
| `g` | `U+0067` | 103 | Latin Small Letter G | `U+0000-00FF` | `latin` | `latin` | PASS |
| **`Ư`** | **`U+01AF`** | 431 | Latin Capital Letter U with Horn | `U+01AF-01B0`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| **`ã`** | **`U+00E3`** | 227 | Latin Small Letter A with Tilde (*dấu ngã*) | `U+0000-00FF` | `latin` | `latin` | **PASS** |
| **`ẳ`** | **`U+1EB3`** | 7859 | Latin Small Letter A with Breve and Hook Above (*ă + hỏi*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| **`ỡ`** | **`U+1EE1`** | 7905 | Latin Small Letter O with Horn and Tilde (*ơ + ngã*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| **`ợ`** | **`U+1EE3`** | 7907 | Latin Small Letter O with Horn and Dot Below (*ơ + nặng*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| **`ữ`** | **`U+1EEF`** | 7919 | Latin Small Letter U with Horn and Tilde (*ư + ngã*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| **`ế`** | **`U+1EBF`** | 7871 | Latin Small Letter E with Circumflex and Acute (*ê + sắc*) | `U+1EA0-1EF9` | **`vietnamese`** | **`vietnamese`** | **PASS** |
| `2` | `U+0032` | 50 | Digit Two (`tabular-nums`) | `U+0000-00FF` | `latin` | `latin` | PASS |
| `5` | `U+0035` | 53 | Digit Five (`tabular-nums`) | `U+0000-00FF` | `latin` | `latin` | PASS |
| `.` | `U+002E` | 46 | Full Stop (VND thousands separator) | `U+0000-00FF` | `latin` | `latin` | PASS |
| `0` | `U+0030` | 48 | Digit Zero (`tabular-nums`) | `U+0000-00FF` | `latin` | `latin` | PASS |
| **`₫`** | **`U+20AB`** | 8363 | **Dong Sign (VND currency symbol)** | `U+20AB`, `U+20A0-20AB` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| **`ă`** *(supp.)* | **`U+0103`** | 259 | Latin Small Letter A with Breve | `U+0102-0103`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| **`đ`** *(supp.)* | **`U+0111`** | 273 | Latin Small Letter D with Stroke | `U+0110-0111`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| **`Ă`** *(supp.)* | **`U+0102`** | 258 | Latin Capital Letter A with Breve | `U+0102-0103`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |
| **`Ơ`** *(supp.)* | **`U+01A0`** | 416 | Latin Capital Letter O with Horn | `U+01A0-01A1`, `U+0100-02BA` | **`vietnamese`**, `latin-ext` | **`vietnamese`**, `latin-ext` | **PASS** |

---

## 4. Implementation Rules for Phase 02+ (`web/src/styles/`)

1. **Self-Hosting via Fontsource (No Runtime Google Fonts CDN Requests):**
   - Install `@fontsource/be-vietnam-pro` (`5.3.0`, `OFL-1.1`) and `@fontsource-variable/fraunces` (`5.3.0`, `OFL-1.1`), and record both in `docs/progress/DEPENDENCIES.md`.
   - Import the default CSS entrypoints for weights `400`, `500`, `600`, `700` of `@fontsource/be-vietnam-pro` and `@fontsource-variable/fraunces/opsz.css`. Because Fontsource uses CSS `unicode-range` subsetting (identical to the verified ranges above), browsers automatically download only the `vietnamese` and `latin` `.woff2` files needed by the page, with `font-display: swap`.
2. **Font Stack Tokens (`Tailwind v4 @theme`):**
   ```css
   @theme {
     --font-sans: 'Be Vietnam Pro', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
     --font-display: 'Fraunces Variable', 'Fraunces', 'Be Vietnam Pro', Georgia, serif;
     --font-mono: ui-monospace, 'Cascadia Mono', 'SFMono-Regular', Menlo, monospace;
   }
   ```
3. **Tabular Numerals & Vietnamese Diacritic Line-Height Guardrails:**
   - Apply `font-variant-numeric: tabular-nums` to `PriceTag`, `ReceiptSummary`, cart steppers, quantities, countdown timers, and all admin `DataTable` numeric columns.
   - Maintain `line-height: 1.5` on body copy and at least `line-height: 1.2` on `Fraunces` headings so stacked Vietnamese diacritics (`ẳ`, `ỡ`, `ợ`, `ữ`, `ế`, `Đ`, `Ư`) never clip inside `overflow: hidden` containers.
