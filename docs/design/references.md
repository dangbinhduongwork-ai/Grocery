# GroceryHub — Phase 01 UI/UX Reference Research & Pattern Study

- **Verification Date:** `2026-11-15T03:09:59Z`
- **Requirements Covered:** `FR-CUS-001`..`FR-CUS-004`, `FR-CUS-017`, `NFR-002`, `NFR-006`, `NFR-007`
- **Raw Evidence:** [ui-ux-github-api.txt](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer--Phase-01-Research--self-aca95d94/docs/evidence/phase-01/ui-ux-github-api.txt)
- **Design System Spec:** [08-UI-DESIGN-SYSTEM.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer--Phase-01-Research--self-aca95d94/docs/spec/08-UI-DESIGN-SYSTEM.md)

---

## 1. Verified GitHub Reference Repositories

All 5 repositories specified in `docs/spec/04-GITHUB-UI-RESEARCH.md` (plus the redirected alias `silicondeck/shadcn-dashboard-landing-template`) were queried live against the GitHub REST API (`https://api.github.com/repos/<owner>/<repo>`, `/commits?per_page=1`, `/license`, `/tags`, and `package.json`).

### 1.1 Summary Verification Table

| Repository | Canonical URL | Default Branch | Latest Commit SHA & Date (UTC) | Latest Tag / Release | License (SPDX & File) | Stars / Forks | Core Stack (`package.json`) | GroceryHub Verdict |
|---|---|---|---|---|---|---|---|---|
| **Giogfr/aura-storefront** | https://github.com/Giogfr/aura-storefront | `main` | `7b36904949f74727207f84a8f2ba12edb4ad282a`<br>`2026-10-17T00:46:05Z` | None | `MIT` (`LICENSE`) | 0 ★ / 0 ⑂ | React `^19.0.1`, Vite `^8.3.0`, TS `^7.0.2`, Tailwind `^4.3.3` (`@tailwindcss/vite`), `lucide-react` `^0.546.0`, Vitest `^3.2.7` | **Read-only UX flow reference** for storefront interactions (focus trap in drawers, free-shipping progress bar, sticky add-to-cart). Do not copy state/routing or fashion look. |
| **Kiranism/next-shadcn-dashboard-starter** | https://github.com/Kiranism/next-shadcn-dashboard-starter | `main` | `47da494b89e330d884a0f9b5c37a605fbabb3987`<br>`2026-11-02T11:02:26Z`<br>(`pushed_at: 2026-11-08T16:08:10Z`) | `v2.0.0` (`06e83c0`, Base UI)<br>`v1.0.0` (`21848bd`, Radix UI) | `MIT` (`LICENSE`) | 7,275 ★ / 1,625 ⑂ | Next.js `16.4.2`, React `19.3.0`, Tailwind `^4.3.3`, `@base-ui/react` `^1.9.0`, `@tanstack/react-table` `^9.3.1`, `@tanstack/react-query` `^5.104.2`, `nuqs` `^2.10.2`, `@clerk/nextjs`, `cmdk`, `kbar`, `recharts` `3.10.1`, `zod` `^4.6.7`, `zustand` `^5.0.15` | **Pattern reference only** for URL-synced server data tables and feature folder organization. Next.js App Router + Clerk + Base UI (`v2.0.0`) are not drop-in compatible with our Vite + TanStack Router stack. |
| **masondevx/orbynadmin** | https://github.com/masondevx/orbynadmin | `main` | `6c2796f7baf7330ca9f4e8260a92af59cb97070d`<br>`2026-07-17T14:45:12Z` | None | `MIT` (`LICENSE`) | 19 ★ / 10 ⑂ | Next.js `16.2.10`, React `19.2.4`, Tailwind `^4`, `radix-ui` `^1.6.2`, `shadcn` `^4.13.0`, `@tanstack/react-table` `^8.21.3`, `recharts` `^3.9.2`, `cmdk` `^1.1.1`, `sonner` `^2.0.7`, `date-fns` `^4.4.0` | **Low-priority visual layout reference** (specifically logistics/products table composition). All data is hardcoded mock data on Next.js. |
| **shadcnstore/shadcn-dashboard-landing-template**<br>*(redirected from `silicondeck/...`, repo ID `1057889634`)* | https://github.com/shadcnstore/shadcn-dashboard-landing-template | `main` | `866423741fa4b62d5e9427603cb2174aedf00f80`<br>`2026-10-22T12:22:47Z`<br>(`pushed_at: 2026-10-28T07:28:37Z`) | `v2.0.0` (`f12bbec`)<br>`v1.1.0` (`390daf5`)<br>`v1.0.0` (`7c4e3f3`) | `MIT` (`License.md`) | 1,264 ★ / 266 ⑂ | `vite-version/`: Vite `^8.3.3`, React `^19.3.0`, `react-router-dom` `^7.18.4`, Tailwind `^4.3.3`, `@base-ui/react` `^1.8.0`, `@tanstack/react-table` `^9.2.6`, `react-hook-form` `^7.89.0`, `zod` `^4.6.5`, `zustand` `^5.0.15`, `recharts` `3.10.1`, `cmdk` `^1.1.1` | **Secondary Vite reference** (`vite-version/`). Useful for RHF + Zod form compositions and chart wrappers, but uses `react-router-dom` and `@base-ui/react` (`v2.0.0`). |
| **satnaing/shadcn-admin** | https://github.com/satnaing/shadcn-admin | `main` | `e16c87f213a5ba5e45964e9b67c792105ec74d26`<br>`2026-06-11T10:08:35Z`<br>(`pushed_at: 2026-09-10T22:30:37Z`) | `v2.2.1` (`0217f8c`)<br>`v2.2.0` (`934561d`)<br>`v2.1.0` (`bae8eca`)<br>`v2.0.0` (`64d67d4`) | `MIT` (`LICENSE`) | 16,711 ★ / 2,508 ⑂ | Vite `^8.0.8`, React `^19.2.5`, TS `~6.0.3`, Tailwind `^4.2.2` (`@tailwindcss/vite`), `@tanstack/react-router` `^1.168.22`, `@tanstack/react-query` `^5.99.0`, `@tanstack/react-table` `^8.21.3`, `@radix-ui/react-*` (18 primitives), `react-hook-form` `^7.72.1`, `zod` `^4.3.6`, `cmdk` `1.1.1`, `sonner` `^2.0.7`, `recharts` `^3.8.1`, `lucide-react` `^1.8.0`, Vitest `^4.1.4` | **Primary structural reference for `web/src/admin`.** Exact match for our fixed stack (Vite + React 19 + TanStack Router + TanStack Query + TanStack Table + Tailwind v4 + RHF + Zod + Radix shadcn/ui). |

---

### 1.2 Detailed Breakdown: What to Reuse vs. What to Avoid

#### 1. `Giogfr/aura-storefront`
- **Verified Commit:** `7b36904949f74727207f84a8f2ba12edb4ad282a` (`feat: trap and restore focus in dialogs and drawers`, `2026-10-17T00:46:05Z`).
- **What to Reuse (Conceptual & Accessibility Patterns Only):**
  - Focus trapping and focus restoration patterns when opening/closing slide-over drawers (cart drawer, mobile filter sheet).
  - Free-shipping threshold calculation and progressive status bar concept (`DeliveryStrip` in `08-UI-DESIGN-SYSTEM.md`).
  - Reduced-motion media query handling (`prefers-reduced-motion: reduce`) on transitions.
  - Pure helper unit-testing discipline with Vitest.
- **What to Avoid (Strictly Forbidden):**
  - **Client-side simulated commerce:** The repo simulates cart, checkout, accounts, orders, and reviews in React Context/localStorage. GroceryHub enforces server-authoritative totals, stock reservations, and SePay webhook verification (`BR-*`).
  - **Query-string pseudo-routing:** It lacks a router; GroceryHub uses `@tanstack/react-router` with Zod-validated search params.
  - **Visual styling & assets:** Fashion-boutique aesthetic and external Unsplash URLs violate GroceryHub's **"Sổ chợ"** design system and self-hosted imagery rules.

#### 2. `Kiranism/next-shadcn-dashboard-starter`
- **Verified Commit:** `47da494b89e330d884a0f9b5c37a605fbabb3987` (`chore: replace deprecated APIs and guard against new ones`, `2026-11-02T11:02:26Z`).
- **What to Reuse (Architecture & Table UX Patterns):**
  - Data-table toolbar design: faceted multi-select status filters, debounced search input, column visibility toggle, and active filter reset button.
  - Synchronizing pagination (`page`, `pageSize`), sorting (`sortBy`, `sortDir`), and filter state directly into URL query parameters (adapted from `nuqs` to TanStack Router `validateSearch`).
- **What to Avoid:**
  - Next.js 16 App Router conventions (`page.tsx`, `layout.tsx`, React Server Components) and `@clerk/nextjs` authentication.
  - The `v2.0.0` migration to `@base-ui/react` (`^1.9.0`) and `@shadcn/react` (`^0.3.1`), which conflicts with the Radix-based primitives used in `satnaing/shadcn-admin` (see §3 below).
  - Generic SaaS card-grid dashboard layout and `@faker-js/faker` mock data generators in production code.

#### 3. `masondevx/orbynadmin`
- **Verified Commit:** `6c2796f7baf7330ca9f4e8260a92af59cb97070d` (`Add crypto and industry dashboards, storefront/products data table, and UI polish`, `2026-07-17T14:45:12Z`).
- **What to Reuse (Layout Ideas Only):**
  - Logistics & order fulfillment status breakdown visualization ideas (adapted to our `OrderQueue` and `Dashboard` work queues).
  - Compact product inventory table row density (thumbnail + SKU + stock level badge + price + quick actions).
- **What to Avoid:**
  - Next.js 16 framework coupling, 70+ unrelated demo pages (crypto, healthcare, HR, real estate), live runtime theme customizer bloat, and static mock arrays.

#### 4. `shadcnstore/shadcn-dashboard-landing-template` (`silicondeck/shadcn-dashboard-landing-template`)
- **Verified Commit:** `866423741fa4b62d5e9427603cb2174aedf00f80` (`chore: pin Node 24 in .nvmrc to match CI and the 20.19+ minimum`, `2026-10-22T12:22:47Z`).
- **Repository Redirect Note:** Querying `https://api.github.com/repos/silicondeck/shadcn-dashboard-landing-template` returns HTTP 200 via GitHub repository ID `1057889634` with canonical `full_name: "shadcnstore/shadcn-dashboard-landing-template"`.
- **What to Reuse (Selective Form & Layout Patterns from `vite-version/`):**
  - Split-screen authentication layout (`≥ 1024 px` editorial photo panel on one side, minimal form on the other, matching `08-UI-DESIGN-SYSTEM.md` §5 Auth recipe).
  - Sectioned form layout with React Hook Form + Zod validation error display.
- **What to Avoid:**
  - `react-router-dom` (`^7.18.4`) routing setup — GroceryHub uses `@tanstack/react-router`.
  - SaaS marketing landing page sections (gradient hero banners, pricing tier cards, fake testimonials, chat/mail/calendar demo apps).

#### 5. `satnaing/shadcn-admin`
- **Verified Commit:** `e16c87f213a5ba5e45964e9b67c792105ec74d26` (`build(deps): bump @clerk/react from 6.4.2 to 6.4.3 (#303)`, `2026-06-11T10:08:35Z`, release tag `v2.2.1`).
- **What to Reuse (Structural Foundation for `web/src/admin`):**
  - Collapsible keyboard-accessible sidebar shell (`SidebarProvider`, grouped navigation items for *Bán hàng, Kho, Danh mục, Khuyến mãi, Báo cáo, Hệ thống*).
  - Global command palette (`cmdk` `1.1.1`) wired to `⌘K` / `Ctrl+K` for instant jump-to-order and jump-to-product.
  - TanStack Table (`@tanstack/react-table` `^8.21.3`) + TanStack Router (`^1.168.22`) search-param synchronization for server-paginated admin queues.
  - Responsive drawer/dialog patterns and keyboard navigation shortcuts.
- **What to Avoid:**
  - `@clerk/react` dependency (GroceryHub uses its own ASP.NET Core Identity + JWT in memory + HttpOnly refresh cookie + `/auth/me` permission guards).
  - Default shadcn zinc/slate palette and card-heavy dashboard KPI grid (must be re-themed to Admin `--surface: #F6F7F5`, `--panel: #FFFFFF`, `--border: #DDE2DC`, `--leaf: #1F3A2D`, and a single bordered KPI band per `08-UI-DESIGN-SYSTEM.md` §3 & §6).

---

## 2. Public Grocery E-Commerce UX Pattern Study

We analyzed two leading Vietnamese online grocery platforms (**Bách Hóa Xanh**, **WinMart / Kingfoodmart**) and two international benchmarks (**Instacart**, **Ocado / Picnic**) across the six core grocery UX touchpoints required for `FR-CUS-001..004`, `FR-CUS-017`, and `08-UI-DESIGN-SYSTEM.md`. No visual assets, copy, or code were copied.

### 2.1 Vietnamese Grocers

#### A. Bách Hóa Xanh (`bachhoaxanh.com`)
- **Aisle Navigation:** Uses a persistent left-hand category tree on desktop and a quick-access category sheet on mobile, organized by everyday Vietnamese household mental models (*Thịt, cá, trứng, hải sản*; *Rau, củ, nấm, trái cây*; *Dầu ăn, nước chấm, gia vị*; *Sữa các loại*). Search bar occupies the top center with instant product thumbnail + price autocomplete.
- **Unit Pricing & Local Units:** Explicitly labels Vietnamese selling units (*khay 300g*, *túi 500g*, *chai 1L*, *lốc 4 hộp*, *vỉ 10 quả*) alongside converted reference unit prices (`₫/kg` or `₫/100g`) so home cooks can compare pack value immediately.
- **Freshness & Expiry Indicators:** Highlights batch freshness and remaining shelf-life commitments on perishables/dairy, paired with a visible quality guarantee (*"Đổi trả trong ngày nếu hàng không tươi"*).
- **Delivery Promise & Slot Clarity:** Prompts for delivery location early (Province/Ward) so stock availability and shipping fees reflect the nearest fulfillment store. Displays clear time windows (e.g., 2-hour express or scheduled morning/afternoon slots) and free-shipping thresholds.
- **Mobile Sticky Cart & Stepper:** Product tiles swap the "Mua" button into an inline `[- 1 +]` quantity stepper as soon as an item is in the cart, paired with a persistent bottom bar showing total items and subtotal.
- **What to Emulate vs. Avoid:**
  - *Emulate:* Inline quantity stepper transformation on product rows/tiles; explicit Vietnamese unit labels (`gói`, `bó`, `khay`, `vỉ`, `kg`); upfront address/serviceability check.
  - *Avoid:* Overcrowded promotional banners, flashing red/yellow flash-sale badges on every card, and visual clutter that obscures unit prices.

#### B. WinMart (`winmart.vn`) & Kingfoodmart (`kingfoodmart.com`)
- **Aisle Navigation:** Kingfoodmart groups fresh produce and ready-to-cook items into task-oriented aisles (e.g., *Đi chợ đêm — Sáng sớm giao hàng*, *Thế giới trái cây*, *Rau củ quả*) with clean sub-category filter pills at the top of the aisle view.
- **Unit Pricing & Member/Discount Hierarchy:** Separates current selling price, struck-through list price, and percentage discount tag (`-15%`).
- **Freshness & Cutoff Transparency:** Both platforms clearly communicate evening cutoff rules for fresh produce (orders placed after 18:00–18:30 are automatically scheduled for early morning delivery the next day to guarantee freshness).
- **Delivery Slot & Checkout Friction Reduction:** Checkout is consolidated into a compact 3-part flow: (1) Recipient & 2-level administrative address + driver delivery notes, (2) Delivery window / payment method (VietQR / COD), and (3) Order summary with voucher selector and free-shipping progress nudge.
- **What to Emulate vs. Avoid:**
  - *Emulate:* Transparent cutoff messaging for fresh goods; slide-out cart drawer with free-shipping progress bar (`DeliveryStrip`); driver delivery note field in checkout; 3-section single-page checkout.
  - *Avoid:* Multi-tier loyalty club price confusion (showing 3 different prices on one card) and intrusive app-install popups on mobile web.

### 2.2 International Grocers

#### C. Instacart (`instacart.com`)
- **Aisle Navigation:** Mirrors physical store aisles (*Produce → Fresh Vegetables → Leafy Greens*) while keeping search and "Buy It Again" one tap away. Filters stay contextual to the current aisle.
- **Unit Pricing:** Strict typographic hierarchy: bold primary package price, muted standardized unit price (`per kg` / `per 100g`) directly underneath, and explicit weight approximation notes for variable-weight produce.
- **Freshness & Exception Handling:** Sets clear customer expectations before checkout on out-of-stock handling and quality guarantees.
- **Delivery Slot & Sticky Cart:** Uses a thumb-friendly floating bottom bar on mobile that doubles as a progressive threshold indicator (*"Add X more for free delivery"*) and transitions into a live vertical fulfillment timeline immediately after order placement.
- **What to Emulate vs. Avoid:**
  - *Emulate:* Dual-metric price hierarchy (`PriceTag` component); post-checkout transition into a vertical timestamped order tracking timeline (`Order tracking` recipe in Spec 08 §5).
  - *Avoid:* Pre-browsing login/zip wall (GroceryHub allows Guests to browse and build a cart freely per `FR-CUS-001..004`), tip-preset upsell modals, and subscription dark patterns.

#### D. Ocado (`ocado.com`) & Picnic
- **Aisle Navigation & Dense Browse:** Ocado supports high-density browsing for 30+ item weekly shops via scannable list/row views alongside grid tiles, plus hairline rules separating shelf groups.
- **Freshness Guarantee (`FreshnessNote`):** Displays explicit minimum shelf-life badges on perishables (e.g., *"Minimum 5 days life guaranteed"*, mapped to `"Hạn dùng còn ≥ 5 ngày"` in Spec 08 §4 only when real batch/expiry data exists in inventory).
- **Receipt-Style Basket Summary:** Basket sidebar presents a clean, receipt-inspired breakdown (subtotal, savings/coupon discount, shipping fee or free-shipping progress, integer total) with tabular numerals so digits align vertically.
- **What to Emulate vs. Avoid:**
  - *Emulate:* `ProductRow` (list density on mobile / power-shopper view) + `ProductTile` (desktop browse grid) toggle; `FreshnessNote` driven strictly by real inventory expiry data; `ReceiptSummary` with dotted leaders and `tabular-nums`.
  - *Avoid:* Mandatory delivery-slot gatekeeper before viewing catalog prices, and multi-step "Did you forget...?" upsell interstitials between cart and checkout.

### 2.3 Synthesis Matrix: GroceryHub UX Rules Derived from Study

| UX Dimension | Pattern Adopted into GroceryHub ("Sổ chợ" Design System) | Anti-Pattern Explicitly Rejected |
|---|---|---|
| **Aisle Navigation** | Left `AisleNav` list on desktop (`≥ 1024 px`) with item counts, hairline separators (`--rule`), and `--leaf` active indicator bar; full-height bottom/side sheet on mobile. | Mega-menu hover mazes; colorful cartoon/emoji category tiles; infinite uncategorized feeds. |
| **Price & Unit Clarity** | `PriceTag` component: selling price in `--tomato-ink` (`#B8321F`) when discounted (else `--ink` `#1B2B22`), struck-through list price in `--ink-2` (`#4A5A50`), `--turmeric` (`#E3A21A`) notch badge (`-15%`) with `--ink` text, and unit price (`/kg`, `/chai`, `/hộp`, `/vỉ`) below in `tabular-nums`. | Floating-point prices; missing unit weight/volume; low-contrast red text on warm paper; fake strikethrough prices. |
| **Freshness & Stock** | `StockBadge` (*"Còn hàng"*, *"Sắp hết (còn N)"*, *"Hết hàng"*) always pairs icon + text. `FreshnessNote` (*"Hạn dùng còn ≥ 5 ngày"*) rendered only when backed by real database batch expiry data. | Fabricated "Only 2 left!" urgency timers or fake expiry guarantees when batch data is null. |
| **Delivery Promise** | `DeliveryStrip` in cart/checkout showing exact VND remaining to reach free shipping (*"Còn 35.000 ₫ để miễn phí giao hàng"*) based on server-configured province zone rate (`BR-*`). | Hidden shipping fees revealed only at the final step; legacy 3-level (District) address selectors. |
| **Mobile Sticky Cart** | Single thumb-zone sticky bottom bar (`min-height: 44 px` touch targets) on product/cart views showing item count, subtotal in `tabular-nums`, and primary CTA (*"Thêm vào giỏ"* / *"Thanh toán"*). | Overlapping floating chat bubbles, back-to-top buttons, and sticky promotional banners covering the CTA bar. |
| **Checkout Flow** | Single-page 3-step accordion (*1. Giao hàng → 2. Thanh toán → 3. Xác nhận*) with Vietnam 2-level address picker (34 Provinces/Cities → Ward/Commune) and sticky `ReceiptSummary`. | Multi-page wizard reloads; client-side order total math; trusting "Tôi đã chuyển khoản" button to mark orders paid. |

---

## 3. Recommendation: `shadcn/ui` Primitive Foundation — Radix UI vs. Base UI

### 3.1 Context from Repository Verification
Our live inspection of `package.json` across the 5 reference repositories revealed a critical ecosystem split in late 2025–2026:
1. **`satnaing/shadcn-admin` (`v2.2.1`)** — our primary Vite + `@tanstack/react-router` + `@tanstack/react-query` + `@tanstack/react-table` reference — is built on **Radix UI primitives** (`@radix-ui/react-dialog`, `@radix-ui/react-dropdown-menu`, `@radix-ui/react-select`, `@radix-ui/react-tabs`, `@radix-ui/react-popover`, `@radix-ui/react-tooltip`, etc.) with React 19 (`^19.2.5`) and Tailwind CSS v4 (`^4.2.2`).
2. **`Kiranism/next-shadcn-dashboard-starter` (`v2.0.0`)** and **`shadcnstore/shadcn-dashboard-landing-template` (`v2.0.0`)** migrated in their `v2.0.0` tags to **`@base-ui/react`** (`^1.8.0` / `^1.9.0`), whereas their `v1.0.0` tags used Radix UI.
3. **`masondevx/orbynadmin`** uses the unified **`radix-ui`** (`^1.6.2`) package with `shadcn` (`^4.13.0`).

### 3.2 Recommendation: **Adopt Radix UI Variant of `shadcn/ui` (Standardize on Radix Primitives)**

From a UI/UX, accessibility (WCAG 2.2 AA), and frontend architecture perspective, **GroceryHub should standardize on the Radix UI variant of `shadcn/ui`** (using either the unified `radix-ui` package or `@radix-ui/react-*` primitives matching `satnaing/shadcn-admin`) and **must never mix Radix UI and Base UI in the same workspace**.

#### Rationale:
1. **Direct Structural Compatibility with `satnaing/shadcn-admin` + Vite + TanStack Router:**
   - `satnaing/shadcn-admin` is the only reference repository that shares our exact frontend architecture (Vite + React 19 + TanStack Router + TanStack Query + TanStack Table + Tailwind v4). Its sidebar shell, responsive drawer/sheet menus, data-table faceted filter popovers, and `cmdk` command dialog are all wired with Radix `asChild` / `Slot` composition and Radix `data-[state=open]` / `data-[state=closed]` attributes.
2. **Mature WCAG 2.2 AA Accessibility & Vietnamese IME Compatibility:**
   - Radix UI primitives (`Dialog`, `AlertDialog`, `Select`, `DropdownMenu`, `Popover`, `Tabs`, `Tooltip`, `RadioGroup`, `Checkbox`) have battle-tested focus management, roving `tabindex`, ARIA landmark/role wiring, Escape/outside-click dismissal, and scroll locking.
   - Crucial for Vietnamese users (`NFR-006`, `NFR-007`): Radix inputs, comboboxes (`cmdk`), and dialogs handle Vietnamese IME composition events (`compositionstart` / `compositionend` in Telex/VNI input methods such as Unikey/EVKey) without prematurely triggering Enter/Escape keydown handlers when typing diacritics like `đ`, `ơ`, `ư`, `ệ`.
3. **Zero Bundle Duplication & Consistent Styling Hooks:**
   - Mixing `@base-ui/react` and `@radix-ui/react-*` would ship two competing portal, focus-trap, floating-positioning, and dismissable-layer runtimes in the bundle and split CSS selectors between `data-[state=...]` (Radix) and `data-[open]` / `data-[popup-open]` (Base UI).
   - Standardizing on Radix UI keeps `web/src/shared/ui/**` compact, deterministic, and easy to theme with GroceryHub's `--paper`, `--ink`, `--leaf`, `--tomato-ink`, and `--turmeric` tokens.
