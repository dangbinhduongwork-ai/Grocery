# GroceryHub — WCAG 2.2 Relative Luminance & Color Contrast Verification (`contrast.md`)

- **Verification Date:** `2026-11-15T03:11:55Z`
- **Requirements Covered:** `NFR-006` (WCAG 2.2 AA Accessibility), `08-UI-DESIGN-SYSTEM.md` §3 & §9
- **Raw Calculation Evidence:** [ui-ux-font-contrast.txt](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer--Phase-01-Research--self-aca95d94/docs/evidence/phase-01/ui-ux-font-contrast.txt)

---

## 1. Methodology (WCAG 2.2 Relative Luminance Formula)

All contrast ratios were computed programmatically in Node.js (`verify-phase01.mjs`) using the exact W3C WCAG 2.2 relative luminance specification:

1. For each 8-bit sRGB channel $C_8 \in [0, 255]$, normalize $c = C_8 / 255$ and linearize:
   $$C_{\text{lin}} = \begin{cases} \dfrac{c}{12.92} & \text{if } c \le 0.04045 \\[6pt] \left(\dfrac{c + 0.055}{1.055}\right)^{2.4} & \text{if } c > 0.04045 \end{cases}$$
2. Compute relative luminance $L$:
   $$L = 0.2126 \cdot R_{\text{lin}} + 0.7152 \cdot G_{\text{lin}} + 0.0722 \cdot B_{\text{lin}}$$
3. For lighter luminance $L_1 = \max(L_{\text{fg}}, L_{\text{bg}})$ and darker luminance $L_2 = \min(L_{\text{fg}}, L_{\text{bg}})$, compute the contrast ratio:
   $$\text{CR} = \frac{L_1 + 0.05}{L_2 + 0.05}$$

### WCAG 2.2 Thresholds Applied
- **Spec 08 §3 Primary Body Target (`--ink` on `--paper`):** $\ge 12.0:1$ (exceeds WCAG AAA $7.0:1$).
- **WCAG 2.2 AA Normal Text (`< 18.66 px` regular or `< 14 px` bold):** $\ge 4.5:1$.
- **WCAG 2.2 AA Large Text (`>= 24 px` regular or `>= 18.66 px` bold — e.g. scale steps `19 px` bold, `23 px`, `28 px`, `34 px`, `42 px`) & Non-Text UI Components/Icons (SC 1.4.11):** $\ge 3.0:1$.
- **Decorative Hairlines / Accents Not Conveying Text (`--rule`, `--sprout`):** Never used for text.

---

## 2. Token Relative Luminance Table ($L$)

| Token | Hex | RGB `(R, G, B)` | Relative Luminance $L$ | Primary Role (`08-UI-DESIGN-SYSTEM.md` §3) |
|---|---|---|---|---|
| `--paper` | `#FAF7F0` | `(250, 247, 240)` | `0.931368` | Storefront primary background |
| `--paper-2` | `#F2EDE0` | `(242, 237, 224)` | `0.848274` | Storefront alternate bands, input backgrounds |
| `--ink` | `#1B2B22` | `(27, 43, 34)` | `0.020763` | Primary body text |
| `--ink-2` | `#4A5A50` | `(74, 90, 80)` | `0.093474` | Secondary / muted text, struck-through list price |
| `--leaf` | `#1F3A2D` | `(31, 58, 45)` | `0.035069` | Brand primary, primary button fill, links, active aisle bar |
| `--leaf-2` | `#2F5A44` | `(47, 90, 68)` | `0.083340` | Primary button / link hover & pressed state |
| `--sprout` | `#7FA38A` | `(127, 163, 138)` | `0.325414` | Subtle tag/chip fill or decorative accent (**never for text on paper**) |
| `--tomato` | `#D9432F` | `(217, 67, 47)` | `0.189713` | Large display price (`>= 19 px` bold) / badge accent |
| `--tomato-ink` | `#B8321F` | `(184, 50, 31)` | `0.125705` | Small/normal discounted price text, destructive button fill, error text |
| `--turmeric` | `#E3A21A` | `(227, 162, 26)` | `0.422461` | Highlight & discount chip background (`-15%`) — paired with `--ink` text |
| `--rule` | `#D9D1BD` | `(217, 209, 189)` | `0.640268` | 1 px hairline dividers |
| `--ok` | `#2E7D4F` | `(46, 125, 79)` | `0.158126` | Success status icon + text (on `--paper`, `--surface`, `--panel`) |
| `--warn` | `#B7791F` | `(183, 121, 31)` | `0.238410` | Warning status icon / border / large badge ($\ge 3.0:1$) |
| `--warn-ink` *(added)* | `#8A560D` | `(138, 86, 13)` | `0.120879` | **Text-safe warning token** ($\ge 4.5:1$ on `--paper`, `--paper-2`, `--surface`, `--panel`) |
| `--err` | `#B8321F` | `(184, 50, 31)` | `0.125705` | Error status icon + text |
| `--info` | `#2B6C8F` | `(43, 108, 143)` | `0.132219` | Info status icon + text |
| `--surface` *(admin)* | `#F6F7F5` | `(246, 247, 245)` | `0.927069` | Admin app background surface |
| `--panel` *(admin)* | `#FFFFFF` | `(255, 255, 255)` | `1.000000` | Admin elevated panel / table surface |
| `--border` *(admin)* | `#DDE2DC` | `(221, 226, 220)` | `0.749322` | Admin 1 px table/panel hairlines |

---

## 3. Complete Foreground / Background Contrast Matrix

### 3.1 Storefront Primary Surface (`--paper` `#FAF7F0`, $L = 0.931368$)

| Foreground Token | Hex | Background Token | Hex | Contrast Ratio | Normal Text ($\ge 4.5:1$) | Large Text / UI ($\ge 3.0:1$) | WCAG 2.2 Level & Usage Rule |
|---|---|---|---|---|---|---|---|
| `--ink` | `#1B2B22` | `--paper` | `#FAF7F0` | **13.87 : 1** | **PASS** | **PASS** | **AAA** (Exceeds Spec §3 $\ge 12:1$ target). All body copy & headings. |
| `--ink-2` | `#4A5A50` | `--paper` | `#FAF7F0` | **6.84 : 1** | **PASS** | **PASS** | **AA** (Almost AAA). Secondary text, unit price (`/kg`), struck-through list price. |
| `--leaf` | `#1F3A2D` | `--paper` | `#FAF7F0` | **11.54 : 1** | **PASS** | **PASS** | **AAA**. Brand links, section headers, focus-visible 2 px outline. |
| `--leaf-2` | `#2F5A44` | `--paper` | `#FAF7F0` | **7.36 : 1** | **PASS** | **PASS** | **AAA**. Hovered/active links and interactive icons on paper. |
| `--tomato-ink` | `#B8321F` | `--paper` | `#FAF7F0` | **5.59 : 1** | **PASS** | **PASS** | **AA**. Required by Spec §3 for all discounted `PriceTag` text & inline error messages. |
| `--tomato` | `#D9432F` | `--paper` | `#FAF7F0` | **4.09 : 1** | **FAIL** (`< 4.5`) | **PASS** | **AA Large / UI Only**. Use only for large display numerals ($\ge 19\text{ px}$ bold / $\ge 24\text{ px}$) or icons/badges; use `--tomato-ink` (`#B8321F`) for normal text. |
| `--ok` | `#2E7D4F` | `--paper` | `#FAF7F0` | **4.72 : 1** | **PASS** | **PASS** | **AA**. "Còn hàng" (`StockBadge`) and positive status text + icon. |
| `--warn` | `#B7791F` | `--paper` | `#FAF7F0` | **3.40 : 1** | **FAIL** (`< 4.5`) | **PASS** | **AA Large / Icon Only**. Valid for warning icon/border; for small warning text use `--warn-ink` (`#8A560D`) or `--ink` (`#1B2B22`). |
| `--warn-ink` | `#8A560D` | `--paper` | `#FAF7F0` | **5.74 : 1** | **PASS** | **PASS** | **AA**. Text-safe warning label ("Sắp hết (còn N)") when colored text is needed. |
| `--err` | `#B8321F` | `--paper` | `#FAF7F0` | **5.59 : 1** | **PASS** | **PASS** | **AA**. "Hết hàng" (`StockBadge`) and form validation error text + icon. |
| `--info` | `#2B6C8F` | `--paper` | `#FAF7F0` | **5.39 : 1** | **PASS** | **PASS** | **AA**. Informational status notes + icon. |
| `--sprout` | `#7FA38A` | `--paper` | `#FAF7F0` | **2.61 : 1** | **FAIL** | **FAIL** | **Decorative Only** (Spec §3 explicitly forbids `--sprout` for text; use `--ink` on `--sprout` fill at `5.31:1`). |

### 3.2 Storefront Alternate Bands & Input Backgrounds (`--paper-2` `#F2EDE0`, $L = 0.848274$)

| Foreground Token | Hex | Background Token | Hex | Contrast Ratio | Normal Text ($\ge 4.5:1$) | Large Text / UI ($\ge 3.0:1$) | WCAG 2.2 Level & Usage Rule |
|---|---|---|---|---|---|---|---|
| `--ink` | `#1B2B22` | `--paper-2` | `#F2EDE0` | **12.69 : 1** | **PASS** | **PASS** | **AAA**. Input values, receipt summary rows, alt band body text. |
| `--ink-2` | `#4A5A50` | `--paper-2` | `#F2EDE0` | **6.26 : 1** | **PASS** | **PASS** | **AA**. Secondary labels, input helper text on `--paper-2`. |
| `--leaf` | `#1F3A2D` | `--paper-2` | `#F2EDE0` | **10.56 : 1** | **PASS** | **PASS** | **AAA**. Links, active indicators, and focus ring on `--paper-2`. |
| `--leaf-2` | `#2F5A44` | `--paper-2` | `#F2EDE0` | **6.74 : 1** | **PASS** | **PASS** | **AA**. Link hover state on `--paper-2`. |
| `--tomato-ink` | `#B8321F` | `--paper-2` | `#F2EDE0` | **5.11 : 1** | **PASS** | **PASS** | **AA**. Discounted prices & error messages on `--paper-2` bands. |
| `--tomato` | `#D9432F` | `--paper-2` | `#F2EDE0` | **3.75 : 1** | **FAIL** (`< 4.5`) | **PASS** | **AA Large / UI Only**. Never use `--tomato` for `< 19 px` text on `--paper-2`; use `--tomato-ink`. |
| `--ok` | `#2E7D4F` | `--paper-2` | `#F2EDE0` | **4.32 : 1** | **FAIL** (`4.32 < 4.5`) | **PASS** | **AA Large / Icon Only on `--paper-2`**. On `--paper-2`, render the `--ok` icon (`4.32:1` passes SC 1.4.11 $\ge 3:1$) paired with `--ink` text (`12.69:1`). |
| `--warn-ink` | `#8A560D` | `--paper-2` | `#F2EDE0` | **5.26 : 1** | **PASS** | **PASS** | **AA**. Warning status text on `--paper-2`. |
| `--info` | `#2B6C8F` | `--paper-2` | `#F2EDE0` | **4.93 : 1** | **PASS** | **PASS** | **AA**. Info status text on `--paper-2`. |

### 3.3 Buttons, Chips & Badges (Filled Component Surfaces)

| Foreground Token | Hex | Background Token | Hex | Contrast Ratio | Normal Text ($\ge 4.5:1$) | Large Text / UI ($\ge 3.0:1$) | WCAG 2.2 Level & Usage Rule |
|---|---|---|---|---|---|---|---|
| `--paper` | `#FAF7F0` | `--leaf` | `#1F3A2D` | **11.54 : 1** | **PASS** | **PASS** | **AAA**. Primary CTA button label (`--paper` text on `--leaf` button). |
| `#FFFFFF` | `#FFFFFF` | `--leaf` | `#1F3A2D` | **12.34 : 1** | **PASS** | **PASS** | **AAA**. Primary CTA button label (white text on `--leaf` button). |
| `--paper` | `#FAF7F0` | `--leaf-2` | `#2F5A44` | **7.36 : 1** | **PASS** | **PASS** | **AAA**. Primary CTA button hover/pressed state (`--paper` on `--leaf-2`). |
| `#FFFFFF` | `#FFFFFF` | `--leaf-2` | `#2F5A44` | **7.87 : 1** | **PASS** | **PASS** | **AAA**. Primary CTA button hover/pressed state (white on `--leaf-2`). |
| `--ink` | `#1B2B22` | `--turmeric` | `#E3A21A` | **6.68 : 1** | **PASS** | **PASS** | **AA** (Spec §3: text on `--turmeric` is always `--ink`). Discount notch label (`-15%`) and promo chips. |
| `--ink` | `#1B2B22` | `--sprout` | `#7FA38A` | **5.31 : 1** | **PASS** | **PASS** | **AA**. Subtle category/attribute tag when `--sprout` is used as a chip background with `--ink` text. |
| `#FFFFFF` | `#FFFFFF` | `--tomato-ink` | `#B8321F` | **5.98 : 1** | **PASS** | **PASS** | **AA**. Destructive button fill (`--tomato-ink` background with white text). |
| `#FFFFFF` | `#FFFFFF` | `--tomato` | `#D9432F` | **4.38 : 1** | **FAIL** (`4.38 < 4.5`) | **PASS** | **Important Finding**: White normal text on raw `--tomato` (`#D9432F`) is `4.38:1` (just under `4.5:1`). **Rule:** Solid destructive buttons with white text MUST use `--tomato-ink` (`#B8321F`, `5.98:1`). |

### 3.4 Admin Theme Surfaces (`--surface` `#F6F7F5`, $L = 0.927069$ & `--panel` `#FFFFFF`, $L = 1.000000$)

| Foreground Token | Hex | Background Token | Hex | Contrast Ratio | Normal Text ($\ge 4.5:1$) | Large Text / UI ($\ge 3.0:1$) | WCAG 2.2 Level & Usage Rule |
|---|---|---|---|---|---|---|---|
| `--ink` | `#1B2B22` | `--surface` | `#F6F7F5` | **13.81 : 1** | **PASS** | **PASS** | **AAA**. Admin shell primary text. |
| `--ink-2` | `#4A5A50` | `--surface` | `#F6F7F5` | **6.81 : 1** | **PASS** | **PASS** | **AA**. Admin shell muted/secondary text. |
| `--leaf` | `#1F3A2D` | `--surface` | `#F6F7F5` | **11.49 : 1** | **PASS** | **PASS** | **AAA**. Admin sidebar active item & links. |
| `--tomato-ink` / `--err` | `#B8321F` | `--surface` | `#F6F7F5` | **5.56 : 1** | **PASS** | **PASS** | **AA**. Admin error/anomaly text on `--surface`. |
| `--ok` | `#2E7D4F` | `--surface` | `#F6F7F5` | **4.69 : 1** | **PASS** | **PASS** | **AA**. Admin success status text on `--surface`. |
| `--warn-ink` | `#8A560D` | `--surface` | `#F6F7F5` | **5.72 : 1** | **PASS** | **PASS** | **AA**. Admin warning status text on `--surface`. |
| `--info` | `#2B6C8F` | `--surface` | `#F6F7F5` | **5.36 : 1** | **PASS** | **PASS** | **AA**. Admin info status text on `--surface`. |
| `--ink` | `#1B2B22` | `--panel` | `#FFFFFF` | **14.84 : 1** | **PASS** | **PASS** | **AAA**. Admin DataTable & card panel primary text. |
| `--ink-2` | `#4A5A50` | `--panel` | `#FFFFFF` | **7.32 : 1** | **PASS** | **PASS** | **AAA**. Admin DataTable secondary/header text. |
| `--leaf` | `#1F3A2D` | `--panel` | `#FFFFFF` | **12.34 : 1** | **PASS** | **PASS** | **AAA**. Admin primary action links in tables/panels. |
| `--ok` | `#2E7D4F` | `--panel` | `#FFFFFF` | **5.05 : 1** | **PASS** | **PASS** | **AA**. Admin `StatusChip` (`Paid`, `Delivered`, `InStock`). |
| `--warn` | `#B7791F` | `--panel` | `#FFFFFF` | **3.64 : 1** | **FAIL** (`< 4.5`) | **PASS** | **AA Large / Icon Only**. Use `--warn` for icon/dot/border and `--warn-ink` (`#8A560D`, `6.14:1`) or `--ink` (`#1B2B22`) for `StatusChip` label text. |
| `--warn-ink` | `#8A560D` | `--panel` | `#FFFFFF` | **6.14 : 1** | **PASS** | **PASS** | **AA**. Admin `StatusChip` warning label (`PendingPayment`, `LowStock`). |
| `--err` | `#B8321F` | `--panel` | `#FFFFFF` | **5.98 : 1** | **PASS** | **PASS** | **AA**. Admin `StatusChip` error/cancelled label (`Failed`, `Cancelled`). |
| `--info` | `#2B6C8F` | `--panel` | `#FFFFFF` | **5.76 : 1** | **PASS** | **PASS** | **AA**. Admin `StatusChip` processing label (`Confirmed`, `Preparing`, `Shipping`). |

---

## 4. Mandatory Design Token Guardrails Discovered by Contrast Math

Our exact luminance calculations surfaced four strict rules that must be enforced in `web/src/shared/tokens/tokens.css` and component implementations:

1. **Always use `--tomato-ink` (`#B8321F`) for small/normal red text AND solid red buttons with white text:**
   - `--tomato` (`#D9432F`) on `--paper` (`#FAF7F0`) is `4.09:1` (passes `>= 3.0:1` for large text $\ge 19\text{ px}$ bold, fails `4.5:1` for normal text).
   - `#FFFFFF` on `--tomato` (`#D9432F`) is `4.38:1` (fails `4.5:1` by `0.12`), whereas `#FFFFFF` on `--tomato-ink` (`#B8321F`) is **`5.98:1` (PASS AA)**.
   - Therefore, `--tomato-ink` (`#B8321F`) is the required token both for normal-size discounted prices/error text on paper (`5.59:1`) and for solid destructive button backgrounds with white text (`5.98:1`).
2. **Add `--warn-ink` (`#8A560D`) for warning label text:**
   - `--warn` (`#B7791F`) is `3.40:1` on `--paper` and `3.64:1` on `#FFFFFF` — passing WCAG 2.2 SC 1.4.11 ($\ge 3.0:1$) for warning icons, status dots, and borders, but below `4.5:1` for small text.
   - Pairing the `--warn` (`#B7791F`) icon with either `--ink` (`#1B2B22`) or `--warn-ink` (`#8A560D`, `5.74:1` on `--paper`, `5.26:1` on `--paper-2`, `6.14:1` on `#FFFFFF`) guarantees 100% WCAG 2.2 AA compliance across all surfaces.
3. **On `--paper-2` (`#F2EDE0`), pair `--ok` (`#2E7D4F`) icons with `--ink` text:**
   - `--ok` (`#2E7D4F`) passes normal text contrast on `--paper` (`4.72:1`), Admin `--surface` (`4.69:1`), and Admin `--panel` (`5.05:1`), and passes icon/large-text contrast on `--paper-2` (`4.32:1`). When rendering a success message inside a `--paper-2` band, use `--ok` for the icon and `--ink` (`12.69:1`) for the text.
4. **Never use `--sprout` (`#7FA38A`) as a foreground text color on light backgrounds:**
   - `--sprout` on `--paper` is `2.61:1`. When `--sprout` is used as a subtle badge background, its foreground text must be `--ink` (`#1B2B22`), which yields **`5.31:1` (PASS AA)**.
