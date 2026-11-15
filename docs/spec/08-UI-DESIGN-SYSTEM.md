# 08 — GroceryHub UI Design System

Goal: a Vietnamese online grocer that feels **deliberate, warm and trustworthy** — not a generic SaaS template. Working name for the visual concept: **"Sổ chợ"** (the market ledger): kraft/paper surfaces, ink-green type, hand-written-price-tag energy, aisle signage clarity.

## 1. Principles
1. **Clarity over decoration**: price, freshness, availability and delivery promise are always legible at a glance.
2. **Aisles, not feeds**: navigation mirrors how people shop a store (aisle → shelf → item).
3. **Few boxes**: hairline rules, whitespace and typography create structure; cards only for selectable units.
4. **Local**: Vietnamese-first typography, VND formatting, local units (chai, gói, hộp, vỉ, kg), local address model.
5. **Honest imagery**: real product photos, consistent background; no stock "AI" art; no emoji.
6. **Distinct page recipes**: each page type has its own composition (below).

## 2. Anti-pattern blacklist (reviewers reject on sight)
Purple/blue-violet or rainbow gradients; glassmorphism/blur panels; blob/mesh backgrounds; centered "headline + 2 buttons" gradient hero; every section wrapped in identical rounded shadow cards; emoji as icons/bullets; default shadcn zinc palette untouched; identical layouts across home/category/product; stock illustrations of floating dashboards; fake testimonials/lorem ipsum; "Welcome to our amazing platform" copy.

## 3. Tokens
### Color (CSS variables; Tailwind v4 `@theme`)
| Token | Hex | Use |
|---|---|---|
| `--paper` | #FAF7F0 | storefront background |
| `--paper-2` | #F2EDE0 | alt bands, input backgrounds |
| `--ink` | #1B2B22 | body text (contrast ≥ 12:1 on paper) |
| `--ink-2` | #4A5A50 | secondary text (verify ≥ 4.5:1) |
| `--leaf` | #1F3A2D | brand, primary buttons, links |
| `--leaf-2` | #2F5A44 | hover/pressed |
| `--sprout` | #7FA38A | tags/subtle accents (not for text) |
| `--tomato` | #D9432F | price emphasis, discount, destructive (text on paper must pass 4.5:1 — use `--tomato-ink` #B8321F for small text) |
| `--turmeric` | #E3A21A | highlight chips (text on it is `--ink`) |
| `--rule` | #D9D1BD | hairlines |
| `--ok / --warn / --err / --info` | #2E7D4F / #B7791F / #B8321F / #2B6C8F | status (always paired with icon + text) |
Admin theme `--surface`: #F6F7F5, panels #FFFFFF, borders #DDE2DC, same leaf accent; dark mode optional (S) with own verified palette.
**Agents must verify every text/background pair with a real contrast calculation and save the table to `docs/design/contrast.md`.**

### Typography
- UI/body: **Be Vietnam Pro** (OFL), weights 400/500/600/700. Display (editorial headings, home/category hero titles): **Fraunces** candidate — verify the `vietnamese` subset exists and renders; if not, fall back to Be Vietnam Pro 700 with tighter tracking. Self-host (Fontsource), subset with `unicode-range`, `font-display: swap`.
- Scale (px, 1.2 ratio from 16): 12/14/16/19/23/28/34/42. Line height 1.5 body, 1.2 headings.
- Numerals: `font-variant-numeric: tabular-nums` for prices, quantities, tables.
- Test string for every font weight: `Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ ữ ế 25.000 ₫`.

### Spacing, shape, elevation
4-px base scale (4/8/12/16/24/32/48/64/96). Radius: 4 (inputs/tags), 8 (buttons), 12 (sheets/drawers); no pill-everything. Elevation: almost none; use 1 px rules; drawers/menus get one soft shadow token. Max content width 1240 px; 12-column grid; 16 px mobile gutters.

### Motion
120–200 ms ease-out; add-to-cart: item "tag" flies to cart count (≤ 400 ms) only if `prefers-reduced-motion: no-preference`; skeleton shimmer subtle; no parallax.

## 4. Signature components
- **PriceTag**: selling price in `--tomato-ink` when discounted (else `--ink`), list price struck through in `--ink-2`, unit price (`/kg`, `/chai`) below in small type; discount shown as a turmeric notch label "-15%". Tabular numerals, `₫` after number with thin space.
- **AisleNav**: left list (desktop) / full-height sheet (mobile) of categories with counts, hairline separators, current aisle marked by a leaf bar — not tiles.
- **ReceiptSummary**: cart/checkout totals styled as a receipt (dotted leader lines, monospace-adjacent tabular figures), sticky on desktop, bottom bar on mobile.
- **FreshnessNote**: tiny label on perishables (e.g. "Hạn dùng còn ≥ 5 ngày" when data exists) — only when real data.
- **StockBadge**: "Còn hàng", "Sắp hết (còn N)", "Hết hàng" with icon + text.
- **DeliveryStrip**: ETA/free-shipping progress line near cart/checkout ("Còn 35.000 ₫ để miễn phí giao hàng").
- **ProductRow** (list density) and **ProductTile** (grid): two layouts, chosen by context.
- **StatusChip** (admin): icon + label + color for order/payment/stock statuses.
- **Admin**: DataTable (TanStack Table) with sticky header, column visibility, saved filters in URL, keyboard navigation; **CommandPalette** (cmdk) for jump-to-order/product.

## 5. Page recipes — storefront
| Page | Composition (not interchangeable) |
|---|---|
| **Home** | Asymmetric editorial top: left 7/12 headline in display serif with delivery promise + search field (not CTA buttons), right 5/12 photo mosaic of real products. Below: "Hôm nay nên mua" horizontal rail (ProductTile), aisle strip (text links with counts), promo band (turmeric hairline box) , two editorial category features (image + short copy), best sellers (ProductRow list), trust strip (giao nhanh, đổi trả, thanh toán) in plain text with line icons. |
| **Category (aisle)** | Left AisleNav + filter column (collapsible groups), top: breadcrumb + title + result count + sort; results as dense ProductRow/Tile toggle (default Tile ≥ 1024 px, Row on mobile); sticky "filters" bottom sheet on mobile; URL-synced. |
| **Search** | Query echoed, spelling-tolerant (accent-insensitive); grouped result: matching aisles first, then products; empty state suggests aisles. |
| **Product** | Two-column: gallery (large image + thumbnails, zoom) / purchase panel with PriceTag, pack size, quantity stepper, sticky add-to-cart bar on mobile; below: description, specs table (rules, not cards), review summary + list, related as rail. |
| **Cart** | Single-column list with ProductRow + stepper; right ReceiptSummary + coupon field + DeliveryStrip; mobile: bottom bar total + "Thanh toán". |
| **Checkout** | One page, three numbered sections (Giao hàng → Thanh toán → Xác nhận) using accordion-like steps; address picker with Province/Ward selects; ReceiptSummary sticky; no marketing distractions. |
| **Payment (QR)** | Centered QR with large amount, copyable content/payment code, bank details table, live countdown, status line updated from server; clear "Tôi đã chuyển khoản" is **not** a confirmation action (it only triggers an immediate status refresh); success/expired/anomaly states. |
| **Order tracking** | Vertical timeline with timestamps (rule line + dots), items list, address, payment status, actions (cancel/reorder/review). |
| **Account** | Left nav list (Hồ sơ, Địa chỉ, Đơn hàng, Đánh giá), forms with inline validation. |
| **Auth** | Split layout with product photo on one side only on ≥ 1024 px; minimal form. |

## 6. Page recipes — admin/staff
| Page | Composition |
|---|---|
| Shell | Collapsible left sidebar grouped (Bán hàng, Kho, Danh mục, Khuyến mãi, Báo cáo, Hệ thống), top bar with command palette (⌘/Ctrl+K), role-aware menu. |
| Dashboard | KPI strip (numbers with deltas, no cards soup: a single bordered band), revenue line, orders-by-status bar, two work queues (Cần xử lý: orders to confirm, payment anomalies, low stock, reviews) as tables. |
| Order queue | Table with status tabs + saved filters; bulk actions; row expands to side panel. |
| Order detail | Split: left items + timeline, right customer/payment/actions; sticky action bar with only valid transitions. |
| Picking mode (S) | Tablet layout, large tap targets, grouped by aisle, checkbox per item, barcode field. |
| Product form | Sectioned form with left anchor nav; image uploader with ordering; price block shows margin preview (if cost permission). |
| Inventory | Table with inline adjust (reason required), movement drawer. |
| Purchasing | PO editor as spreadsheet-like line grid; receipt screen with qty/cost/expiry per line. |
| Reports | Filter bar, chart + table, CSV export. |

## 7. States and content rules
Every screen implements loading (skeleton shaped like content), empty (typographic + line icon + one action), error (what happened + retry), partial (e.g. some items out of stock), offline banner. Copy: Vietnamese, concrete, ≤ 2 sentences. Examples: "Hết hàng — nhận thông báo khi có lại", "Phí giao hàng sẽ được tính sau khi chọn địa chỉ".

## 8. Imagery system
Product photos: uploaded original (min 1200 px) → derivatives 320/640/960/1440 in WebP/AVIF with 4:5 and 1:1 crops, neutral off-white ground guideline for photographers, alt text required (accessibility), focal-point optional. Dev seed uses generated neutral placeholders labelled "DEV". Banner images are editorial photography; no gradient overlays — use solid paper blocks for text.

## 9. Accessibility
WCAG 2.2 AA: focus-visible 2 px leaf outline offset 2; targets ≥ 44 px mobile; labels not placeholders; error summaries linked to fields; `aria-live` polite for cart/toasts; reduced motion; zoom to 200% without loss; colour never sole signal.

## 10. Implementation notes
- Tokens in `web/src/shared/tokens/tokens.css` + Tailwind `@theme`; shadcn components copied into `web/src/shared/ui` and re-themed (radius, colors, focus, density) — no default zinc.
- Storybook is optional; a `/__design` dev route (dev flag) renders every token/component/state for review screenshots.
- Deliver `docs/design/` : `tokens.md`, `contrast.md`, `components.md`, `recipes.md`, `content-guidelines.md`, `references.md`.
