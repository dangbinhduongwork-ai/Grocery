# GroceryHub — Component Specifications (`components.md`)

- **Design Concept:** **"Sổ chợ"** (The Market Ledger)
- **Requirements Covered:** `FR-CUS-001`..`FR-CUS-004`, `FR-CUS-017` (Responsive & Accessible UI), `NFR-006` (WCAG 2.2 AA Accessibility), `NFR-007` (Vietnamese UI localization).
- **Approved Decisions:** `DEC-004` (Radix UI shadcn/ui primitives), `DEC-005` (Self-hosted fonts: Be Vietnam Pro & Fraunces), `DEC-009` (WCAG 2.2 AA Contrast Guardrails: `--tomato-ink` `#B8321F`, `--warn-ink` `#8A560D`).
- **Design Tokens:** [tokens.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer-Phase-06-Design-Docs-ui-ux-designer-8568a0c4/docs/design/tokens.md)
- **Contrast Calculations:** [contrast.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer-Phase-06-Design-Docs-ui-ux-designer-8568a0c4/docs/design/contrast.md)

---

## 1. Component System Principles

All GroceryHub components reside in `web/src/shared/ui/**` (to be implemented in Phase 08) and adhere strictly to these engineering and design rules:
1. **Radix UI Primitives Only (`DEC-004`):** Built on `@radix-ui/react-*` primitive components. No mixing with `@base-ui/react`.
2. **Zero Generic Defaults:** No default shadcn zinc/slate palette, no 9999px pills on cards or standard buttons, no heavy dropshadows. Hairline rules (`1px solid var(--rule)`) and paper contrast provide structure.
3. **Vietnamese Input Engine (IME) Protection:** Input and combobox components strictly respect `compositionstart` / `compositionend` so Vietnamese Telex/VNI diacritic input does not prematurely trigger form submission or modal close.
4. **Touch Target Accessibility ($\ge 44\text{ px}$):** Mobile touch targets for all primary buttons, steppers, and interactive pills meet the minimum $44 \times 44\text{ px}$ boundary (`FR-CUS-017`, WCAG 2.2 SC 2.5.8).
5. **Visible Focus Ring:** Focus-visible states use a prominent 2px outline in brand leaf (`var(--leaf)` `#1F3A2D`) with a 2px offset (`ring-2 ring-[var(--leaf)] ring-offset-2`).

---

## 2. Themed `shadcn/ui` Primitives

### 2.1 Button (`web/src/shared/ui/button.tsx`)

The Button component provides tactile feedback with honest, grounded weight.

#### Variants & Visual Mapping
- **`default` (Primary Action):**
  - Background: `var(--leaf)` (`#1F3A2D`)
  - Text: `var(--paper)` (`#FAF7F0`) or `#FFFFFF` (Contrast: `11.54:1` / `12.34:1` AAA)
  - Hover / Active: `var(--leaf-2)` (`#2F5A44`)
  - Usage: Primary CTAs ("Thêm vào giỏ", "Thanh toán", "Xác nhận đặt hàng").
- **`secondary` (Alternative Action):**
  - Background: `var(--paper-2)` (`#F2EDE0`)
  - Border: `1px solid var(--rule)` (`#D9D1BD`)
  - Text: `var(--ink)` (`#1B2B22`) (Contrast: `12.69:1` AAA)
  - Hover: Background `#EBE5D6`
  - Usage: Secondary actions ("Tiếp tục mua sắm", "Xem chi tiết", "Mua lại").
- **`destructive` (Danger Action — `DEC-009`):**
  - Background: `var(--tomato-ink)` (`#B8321F`) (**Mandatory per DEC-009; raw `--tomato` `#D9432F` fails 4.5:1 with white text**)
  - Text: `#FFFFFF` (Contrast: `5.98:1` AA)
  - Hover: Background `#9B2B1B`
  - Usage: Order cancellation, item removal confirmation, irreversible admin deletion.
- **`outline` (Subtle Action):**
  - Background: Transparent
  - Border: `1px solid var(--rule)` (`#D9D1BD`)
  - Text: `var(--ink)` (`#1B2B22`)
  - Hover: Background `var(--paper-2)`
  - Usage: Filter toggles, modal dismissal, secondary navigation.
- **`ghost` (Toolbar & Micro-actions):**
  - Background: Transparent
  - Text: `var(--ink)` (`#1B2B22`)
  - Hover: Background `var(--paper-2)`
  - Usage: Pagination controls, stepper buttons in cards, table row actions.
- **`link` (Inline Navigation):**
  - Text: `var(--leaf)` (`#1F3A2D`)
  - Hover: Text decoration underline
  - Usage: Breadcrumbs, "Xem tất cả", footer navigation.

#### Sizes
- `sm`: Height `32px` (`h-8`), padding `px-3`, text `12px` (`text-caption`). Desktop compact mode only.
- `default`: Height `40px` (`h-10`), padding `px-4`, text `14px` (`text-body-sm`), radius `8px` (`rounded-md`).
- `lg`: Height `48px` (`h-12`), padding `px-6`, text `16px` (`text-body-lg`), radius `8px` (`rounded-md`). Primary storefront checkout & PDP CTA.
- `icon`: Height `40px` (`h-10 w-10`), centered icon, min touch target $44\text{ px}$ on mobile via transparent padding.

#### Code Signature Example
```tsx
export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'ghost' | 'link';
  size?: 'sm' | 'default' | 'lg' | 'icon';
  isLoading?: boolean;
}
```

---

### 2.2 Badge (`web/src/shared/ui/badge.tsx`)

Badges convey concise metadata (discounts, product attributes, stock status).

#### Variants
- **`turmeric` (Discount Notch Tag):**
  - Background: `var(--turmeric)` (`#E3A21A`)
  - Text: `var(--ink)` (`#1B2B22`) (Contrast: `6.68:1` AA)
  - Geometry: `rounded-sm` (`4px`), bold tabular text (e.g. `"-15%"`).
- **`sprout` (Attribute & Freshness Tag):**
  - Background: `var(--sprout)` (`#7FA38A`)
  - Text: `var(--ink)` (`#1B2B22`) (Contrast: `5.31:1` AA)
  - Geometry: `rounded-sm` (`4px`), text `12px` (e.g. `"VietGAP"`, `"Hữu cơ"`).
- **`leaf` (Brand Badge):**
  - Background: `var(--leaf)` (`#1F3A2D`)
  - Text: `var(--paper)` (`#FAF7F0`)
- **`ok` (Success Tag):**
  - Background: `#EAF4EE`, Border: `1px solid #B8E0C5`, Text: `var(--ok)` (`#2E7D4F`)
- **`warn` (Low Stock / Warning Tag — `DEC-009`):**
  - Background: `#FBF4E6`, Border: `1px solid #F3D9A0`, Text: `var(--warn-ink)` (`#8A560D`, `5.74:1`)
- **`err` (Out of Stock / Critical Tag):**
  - Background: `#FDF0EE`, Border: `1px solid #F7C6BE`, Text: `var(--tomato-ink)` (`#B8321F`, `5.59:1`)
- **`outline` (Neutral Tag):**
  - Background: Transparent, Border: `1px solid var(--rule)`, Text: `var(--ink-2)`

---

### 2.3 Card (`web/src/shared/ui/card.tsx`)

#### Strict Usage Guardrails (Anti-"Card Soup" Rule)
In GroceryHub, **cards are reserved exclusively for selectable or actionable objects** (a `ProductTile` in a browse grid, a saved address card in checkout, or a voucher ticket chip). Non-interactive sections (heroes, descriptions, feature strips, specifications) use hairline rules (`--rule`), background bands (`--paper-2`), and whitespace.

#### Styling Specification
- **Storefront Card:**
  - Background: `var(--paper)` (`#FAF7F0`) or `#FFFFFF`
  - Border: `1px solid var(--rule)` (`#D9D1BD`)
  - Radius: `8px` (`rounded-md`)
  - Shadow: None by default (`shadow-none`); subtle lift on hover for interactive cards:
    `hover:border-[var(--leaf)] hover:shadow-sm hover:-translate-y-[1px] transition-all duration-120`
- **Admin Panel Card:**
  - Background: `var(--panel)` (`#FFFFFF`)
  - Border: `1px solid var(--border)` (`#DDE2DC`)
  - Radius: `8px` (`rounded-md`)

---

### 2.4 Dialog (`web/src/shared/ui/dialog.tsx`)

Built on `@radix-ui/react-dialog`. Used for modal flows: address editor (`FR-CUS-008`), order cancellation prompt (`FR-CUS-014`), and quick-view image lightbox.

#### Specifications
- **Backdrop Overlay:** `fixed inset-0 bg-[#1B2B22]/45 backdrop-blur-[1px] transition-opacity duration-200`
- **Content Panel:**
  - Desktop: Centered, max-width `540px` (`w-full`), background `var(--paper)` (storefront) / `#FFFFFF` (admin), border `1px solid var(--rule)`, radius `12px` (`rounded-lg`), shadow `var(--shadow-lg)`, padding `24px`.
  - Mobile (`< 768px`): Automatically transitions to a bottom sheet layout anchored to viewport bottom with `rounded-t-xl`.
- **Accessibility:**
  - Roving focus trap automatically engages upon mount; returns focus to trigger upon dismiss.
  - Dismissible via `Escape` key, backdrop tap, or prominent close button (top-right, min $44 \times 44\text{ px}$ hit target).
  - ARIA: `aria-labelledby` binds to `DialogTitle`; `aria-describedby` binds to `DialogDescription`.

---

### 2.5 DropdownMenu (`web/src/shared/ui/dropdown-menu.tsx`)

Built on `@radix-ui/react-dropdown-menu`. Used for sorting selections (Catalog/Aisle sorting), account menus, and admin row action menus.

#### Specifications
- **Container:**
  - Background: `#FFFFFF` / `var(--paper)`
  - Border: `1px solid var(--rule)` (`#D9D1BD`)
  - Radius: `8px` (`rounded-md`), shadow `var(--shadow-md)`
  - Padding: `4px`
  - Min-width: `180px`
- **Menu Items:**
  - Height: `36px`, padding `px-3 py-2`, text `14px` (`text-body-sm`), radius `4px` (`rounded-sm`).
  - Hover / Focus: Background `var(--paper-2)` (`#F2EDE0`), text `var(--ink)`.
  - Active / Checked: Marked with a leading checkmark icon (`Check` in `var(--leaf)`).
  - Keyboard Navigation: Arrow navigation, `Enter` / `Space` to select, `Escape` to close.

---

### 2.6 Input (`web/src/shared/ui/input.tsx`)

Used for search bars, checkout address entry, voucher inputs, and admin forms.

#### Specifications
- **Visual Styling:**
  - Background: `var(--paper-2)` (`#F2EDE0`)
  - Border: `1px solid var(--rule)` (`#D9D1BD`)
  - Text: `var(--ink)` (`#1B2B22`), font-size `16px` on mobile (`text-base` to prevent iOS viewport auto-zoom) / `14px` on desktop.
  - Placeholder: `var(--ink-2)` (`#4A5A50`)
  - Radius: `4px` (`rounded-sm`)
  - Height: `44px` (`h-11` on mobile) / `40px` (`h-10` on desktop)
- **Interactive States:**
  - Focus: `border-[var(--leaf)] ring-2 ring-[var(--leaf)] ring-offset-1`
  - Error State:
    - Border: `border-[var(--tomato-ink)]`
    - Focus Ring: `ring-2 ring-[var(--tomato-ink)]`
    - Feedback: Paired with `AlertCircle` line icon and validation message in `var(--tomato-ink)` (`text-caption`).
- **Vietnamese IME Safety:**
  - Handles `compositionstart` and `compositionend` natively. Search debouncing runs on normalized strings only after composition finishes.

---

### 2.7 Sheet (`web/src/shared/ui/sheet.tsx`)

Built on `@radix-ui/react-dialog`. Serves as the primary slide-over panel for the Cart Drawer (`FR-CUS-005`), Mobile Aisle Navigation (`AisleNav`), and Mobile Filter Sheet (`FR-CUS-003`).

#### Specifications
- **Slide Directions:**
  - Right (`side="right"`): Desktop & Tablet Cart Drawer (`max-w-[420px]`).
  - Bottom (`side="bottom"`): Mobile Filter Sheet and Mobile Cart (`max-h-[85vh]`, rounded top corners `rounded-t-xl`).
- **Motion:** 200ms ease-out entrance (`transition-transform duration-200 ease-out`).
- **Structure:**
  - Header: Fixed top bar with title in Be Vietnam Pro SemiBold and close icon.
  - Body: Scrollable container (`flex-1 overflow-y-auto px-4 py-3`).
  - Footer: Sticky bottom CTA bar with subtotal and primary action button ("Tiến hành thanh toán" / "Áp dụng bộ lọc").

---

### 2.8 Tabs (`web/src/shared/ui/tabs.tsx`)

Built on `@radix-ui/react-tabs`. Used for category filtering, order status queues, and account settings.

#### Variants
- **Underline Style (Storefront Catalog & Account):**
  - Active Tab: Bottom border `2px solid var(--leaf)`, text `var(--leaf)` SemiBold.
  - Inactive Tab: Border transparent, text `var(--ink-2)`, hover text `var(--ink)`.
- **Segmented / Pill Style (Admin Status Queue):**
  - Container: Background `var(--paper-2)`, padding `3px`, radius `6px`.
  - Active Tab: Background `#FFFFFF`, text `var(--ink)`, shadow `0 1px 2px rgba(0,0,0,0.05)`.
  - Count Badges: Embedded inside tab label (e.g. `"Chờ xử lý (12)"`).

---

### 2.9 Tooltip (`web/src/shared/ui/tooltip.tsx`)

Built on `@radix-ui/react-tooltip`. Used for helpful microcopy (e.g. freshness batch explanation, delivery policy details).

#### Specifications
- **Surface:** Background `var(--ink)` (`#1B2B22`), text `var(--paper)` (`#FAF7F0`), radius `4px` (`rounded-sm`), padding `6px 10px`, font-size `12px` (`text-caption`).
- **Timing:** Delay `300ms` on hover, instant dismiss on mouse leave or `Escape`.
- **Contrast:** Exceeds `13.8:1` (AAA).

---

## 3. GroceryHub Custom Signature Patterns

### 3.1 MarketTagPrice (`web/src/shared/ui/market-tag-price.tsx`)

The central signature element of GroceryHub's "Sổ chợ" design system. It renders grocery prices with absolute typographic clarity, instant comparison metrics, and zero floating-point ambiguities (`BR-01`).

#### Component Anatomy
```text
┌─────────────────────────────────────────────────────────────┐
│  [-15%]  25.000 ₫                                           │  <-- Discount Notch + Selling Price
│          30.000 ₫                                           │  <-- Strikethrough List Price
│          50.000 ₫/kg • Khay 500g                            │  <-- Unit Reference Price & Pack Size
└─────────────────────────────────────────────────────────────┘
```

#### Visual Rules & Color Logic
1. **Selling Price (`sellingPrice`):**
   - If discounted (`sellingPrice < listPrice`): Formatted in **`--tomato-ink`** (`#B8321F`, `5.59:1` on `--paper`).
   - If regular price: Formatted in **`--ink`** (`#1B2B22`, `13.87:1` on `--paper`).
   - Tabular numerals (`tabular-nums`) with Vietnamese Dong symbol `₫` separated by a thin space (`\u202F`) or non-breaking space.
2. **List Price (`listPrice`):**
   - Rendered only when `listPrice > sellingPrice`.
   - Struck through (`line-through`) in muted **`--ink-2`** (`#4A5A50`, `6.84:1`).
3. **Discount Notch Badge (`discountPercent`):**
   - Calculated as `Math.round((listPrice - sellingPrice) / listPrice * 100)`.
   - Turmeric background (`var(--turmeric)` `#E3A21A`) with bold **`--ink`** text (`#1B2B22`, `6.68:1` AA).
4. **Unit Reference Price (`unitPriceText`):**
   - e.g. `"50.000 ₫/kg"`, `"12.000 ₫/quả"`, `"28.000 ₫/lít"`.
   - Displayed in `12px` (`text-caption`) in `var(--ink-2)` underneath the price block.

#### Display Modes
- **`compact` (ProductTile / ProductRow):**
  - Selling price: `16px` SemiBold (`text-body-lg font-semibold`).
  - Strikethrough list price: `12px` (`text-caption`).
  - Discount badge: Small inline notch `-15%`.
- **`detailed` (Product Detail Page - PDP):**
  - Selling price: `28px` Bold (`text-h2 font-bold`) in `Fraunces` or `Be Vietnam Pro 700`.
  - Strikethrough list price: `16px` (`text-body-lg`).
  - Full unit price calculation and package size details.
- **`receipt` (ReceiptSummary & Cart Drawer):**
  - Selling price: `16px` Medium in `tabular-nums`, aligned to right margin with dotted leader lines (`. . . . . . . . 25.000 ₫`).

#### TypeScript Props Definition
```tsx
export interface MarketTagPriceProps {
  sellingPrice: number; // Integer VND (long)
  listPrice?: number | null;
  unit?: string;        // e.g. "kg", "khay", "hộp", "chai"
  packWeightGrams?: number | null; // e.g. 500
  variant?: 'compact' | 'detailed' | 'receipt';
  className?: string;
}
```

---

### 3.2 QuantityStepper (`web/src/shared/ui/quantity-stepper.tsx`)

Used on Product Tiles, the PDP purchase block, Cart rows, and the Mobile Sticky Bar.

#### UX & Interaction Rules
1. **Bounds Enforcement:**
   - Minimum: `1` on PDP; `0` in Cart (stepping to `0` triggers item removal or trash icon).
   - Maximum: Capped at `min(availableStock, MaxQuantityPerLine)` where default `MaxQuantityPerLine = 20` (`BR-16`, `DEC-011`).
2. **Touch Targets:**
   - Mobile: Buttons are minimum $44 \times 44\text{ px}$ (`min-h-[44px] min-w-[44px]`) with centered plus/minus icons.
   - Desktop: Compact stepper height `32px` (`h-8`).
3. **Async Server Debouncing:**
   - On rapid clicking, stepper state updates optimistically in local UI while queuing a 300ms debounced API patch (`PATCH /cart/items/{id}`).
   - Displays a subtle inline spinner (`Loader2` in `var(--leaf)`) if the network roundtrip exceeds 200ms.
4. **Keyboard Accessibility:**
   - Stepper input supports `ArrowUp` (increment) and `ArrowDown` (decrement), `Home` (set 1), `End` (set max).
   - Direct manual numeric typing validates `onBlur` to clamp values within $[1, \text{max}]$.

#### TypeScript Props Definition
```tsx
export interface QuantityStepperProps {
  value: number;
  min?: number;
  max?: number;
  onChange: (quantity: number) => void;
  isLoading?: boolean;
  disabled?: boolean;
  size?: 'sm' | 'default' | 'lg';
}
```

---

### 3.3 StockBadge (`web/src/shared/ui/stock-badge.tsx`)

Replaces vague stock indicators with accurate, honest Vietnamese availability states (`BR-02`, `DEC-009`).

#### The 3 Authoritative States
1. **`InStock` ("Còn hàng"):**
   - Icon: `CheckCircle2` (`14px`) in `var(--ok)` (`#2E7D4F`)
   - Text: `"Còn hàng"` in `var(--ok)` (on white/paper) or `var(--ink)`
   - Background: `#EAF4EE` (soft green tint), Border: `1px solid #B8E0C5`
2. **`LowStock` ("Sắp hết — còn N"):**
   - Rendered when `availableStock <= lowStockThreshold` (default `5` units).
   - Icon: `AlertTriangle` (`14px`) in `var(--warn)` (`#B7791F`)
   - Text: `"Sắp hết (còn {stock})"` in **`--warn-ink`** (`#8A560D`, `5.74:1` AA on paper — `DEC-009`)
   - Background: `#FBF4E6`, Border: `1px solid #F3D9A0`
3. **`OutOfStock` ("Hết hàng"):**
   - Icon: `XCircle` (`14px`) in `var(--tomato-ink)` (`#B8321F`)
   - Text: `"Hết hàng"` in `var(--tomato-ink)`
   - Background: `#FDF0EE`, Border: `1px solid #F7C6BE`
   - Primary CTA swaps to `"Nhận thông báo khi có hàng"` (disabled add-to-cart).

#### Perishable Sub-Pattern: `FreshnessNote`
- For fresh produce, dairy, and meat where inventory batch expiry dates exist:
  - Icon: `Clock3` (`14px`) in `var(--leaf)`
  - Text: `"Hạn dùng còn ≥ {days} ngày"` (e.g. `"Hạn dùng còn ≥ 5 ngày"`)
  - **Strict Truthfulness Rule:** Rendered **only when backed by real database batch expiry data** (`BR-02`, `FR-ADM-006`). If batch data is null, the badge is not rendered (never fake freshness commitments).

---

### 3.4 DeliveryStrip (`web/src/shared/ui/delivery-strip.tsx`)

A motivational progress indicator rendered inside the Cart Drawer and Checkout summary to communicate free shipping thresholds transparently (`FR-CUS-010`, `DEC-007`).

#### UX Rules
- Uses server-configured settings: Zone threshold = `300.000 ₫` (`DEC-007`).
- If `subtotal < threshold`:
  - Text: `"Mua thêm "` + `(threshold - subtotal).toLocaleString('vi-VN') + " ₫"` + `" để được miễn phí giao hàng"`
  - Progress bar: Track `var(--paper-2)`, indicator bar in `var(--leaf)` (`#1F3A2D`), height `6px`, `rounded-full`.
- If `subtotal >= threshold`:
  - Icon: `CheckCircle2` in `var(--ok)`
  - Text: `"Đơn hàng của bạn đã đủ điều kiện Miễn phí giao hàng!"`
  - Progress bar: `100%` filled in `var(--ok)`.
