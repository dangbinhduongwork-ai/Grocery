# GroceryHub — Design Tokens Specification (`tokens.md`)

- **Design Concept:** **"Sổ chợ"** (The Market Ledger) — warm paper surfaces, ink-green typography, hand-written price tag energy, clear aisle signage.
- **Requirements Covered:** `FR-CUS-017` (Responsive UI 360–1920 px, WCAG 2.2 AA), `NFR-002` (Web Performance & Self-hosted assets), `NFR-006` (Accessibility), `NFR-007` (Vietnamese UI localization).
- **Approved Decisions:** `DEC-004` (Radix UI shadcn/ui primitives), `DEC-005` (Self-hosted fonts: Be Vietnam Pro & Fraunces), `DEC-009` (WCAG 2.2 AA Contrast Guardrails: `--tomato-ink` `#B8321F`, `--warn-ink` `#8A560D`).
- **Contrast Verification:** [contrast.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer-Phase-06-Design-Docs-ui-ux-designer-8568a0c4/docs/design/contrast.md)
- **Vietnamese Typography Verification:** [font-check.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer-Phase-06-Design-Docs-ui-ux-designer-8568a0c4/docs/design/font-check.md)

---

## 1. Visual Theme Architecture

GroceryHub's visual language departs deliberately from generic SaaS and AI templates (no purple gradients, no glassmorphism, no card soup, no default shadcn zinc palette). The design system mirrors a high-quality Vietnamese fresh market experience:
- **Warm Paper Surface:** Natural kraft and paper undertones (`#FAF7F0`) replace sterile `#FFFFFF` or clinical cool gray, creating a human, artisanal storefront foundation.
- **Deep Market Ink:** Heavy dark green-black typography (`#1B2B22`) guarantees crisp readability and high contrast ($\ge 12:1$ on paper).
- **Market Price Accents:** Pricing adopts honest Vietnamese market-tag visual cues: tomato red for discounted prices, turmeric gold for discount notch chips (`-15%`), paired with tabular numerals and clear package/unit metrics (`₫/kg`, `₫/khay`, `₫/bó`).
- **Aisles over Feeds:** Hairlines (`--rule` `#D9D1BD`) and whitespace structure content logically into aisles, shelves, and lists rather than wrapping every item in floating drop-shadowed cards.

---

## 2. Color Tokens

### 2.1 Storefront Palette

| CSS Variable | Tailwind v4 Token | Hex | Relative Luminance ($L$) | Primary Usage & Role | Contrast vs `--paper` |
|---|---|---|---|---|---|
| `--paper` | `color-paper` | `#FAF7F0` | `0.931368` | Storefront primary page background | Surface |
| `--paper-2` | `color-paper-2` | `#F2EDE0` | `0.848274` | Alternate section bands, input backgrounds, subtle card fills | `1.10:1` |
| `--ink` | `color-ink` | `#1B2B22` | `0.020763` | Primary body text, headings, dark icons, text on turmeric | **13.87:1** (AAA) |
| `--ink-2` | `color-ink-2` | `#4A5A50` | `0.093474` | Secondary text, unit price (`/kg`), struck-through list price, helper text | **6.84:1** (AA) |
| `--leaf` | `color-leaf` | `#1F3A2D` | `0.035069` | Brand primary color, primary CTA button fill, active aisle marker, focus ring | **11.54:1** (AAA) |
| `--leaf-2` | `color-leaf-2` | `#2F5A44` | `0.083340` | Hover and pressed state for primary CTA buttons and links | **7.36:1** (AAA) |
| `--sprout` | `color-sprout` | `#7FA38A` | `0.325414` | Subtle attribute chips, fresh tags (**never for text on paper**; paired with `--ink`) | `2.61:1` (Chip bg) |
| `--tomato` | `color-tomato` | `#D9432F` | `0.189713` | Display prices ($\ge 19\text{ px}$ bold), promotional badge accent | `4.09:1` (Large only) |
| `--tomato-ink` | `color-tomato-ink` | `#B8321F` | `0.125705` | Normal discounted price text, destructive buttons (with white text), form error text | **5.59:1** (AA) |
| `--turmeric` | `color-turmeric` | `#E3A21A` | `0.422461` | Highlight tags, discount notch badges (`-15%`) (**text is always `--ink`**) | Chip bg (`6.68:1` w/ ink) |
| `--rule` | `color-rule` | `#D9D1BD` | `0.640268` | 1px hairline dividers, card/table borders, section separators | Border (`1.42:1`) |

### 2.2 Semantic Status Tokens

All semantic status indicators **must pair an icon with descriptive text** to satisfy WCAG 2.2 SC 1.4.1 (use of color):

| Status Token | Hex | Text Token (Storefront) | Admin Status Role | Icon Association |
|---|---|---|---|---|
| `--ok` | `#2E7D4F` | `--ok` (`4.72:1` on `--paper`) | Completed, Paid, Delivered, InStock ("Còn hàng") | `CheckCircle2` |
| `--warn` | `#B7791F` | `--warn-ink` (`#8A560D`, `5.74:1`) | PendingPayment, LowStock ("Sắp hết"), NearExpiry | `AlertTriangle` |
| `--warn-ink` | `#8A560D` | `--warn-ink` (`5.74:1` on `--paper`) | Mandatory text token for warnings (DEC-009) | Text label |
| `--err` | `#B8321F` | `--err` / `--tomato-ink` (`5.59:1`) | Cancelled, Failed, OutOfStock ("Hết hàng"), Form Error | `XCircle` / `AlertCircle` |
| `--info` | `#2B6C8F` | `--info` (`5.39:1` on `--paper`) | Preparing, Shipping, Batch Info, Processing | `Info` |

### 2.3 Admin Theme Tokens

The Admin interface (`web/src/admin`) uses a cooler, higher-density surface for enterprise scanning and keyboard navigation, retaining the brand `--leaf` accent:

| CSS Variable | Hex | Luminance ($L$) | Role |
|---|---|---|---|
| `--surface` | `#F6F7F5` | `0.927069` | Admin application background surface |
| `--panel` | `#FFFFFF` | `1.000000` | Elevated cards, forms, and DataTable background |
| `--border` | `#DDE2DC` | `0.749322` | Admin 1px hairlines and table cell gridlines |
| `--leaf` | `#1F3A2D` | `0.035069` | Primary action buttons, active navigation item |

---

## 3. CSS Variables & Tailwind CSS v4 Theme Integration

### 3.1 CSS Custom Properties (`web/src/styles/tokens.css`)

```css
:root {
  /* Storefront Surface & Content Tokens */
  --paper: #FAF7F0;
  --paper-2: #F2EDE0;
  --ink: #1B2B22;
  --ink-2: #4A5A50;
  --leaf: #1F3A2D;
  --leaf-2: #2F5A44;
  --sprout: #7FA38A;
  --tomato: #D9432F;
  --tomato-ink: #B8321F;
  --turmeric: #E3A21A;
  --rule: #D9D1BD;

  /* Semantic Feedback Tokens */
  --ok: #2E7D4F;
  --warn: #B7791F;
  --warn-ink: #8A560D;
  --err: #B8321F;
  --info: #2B6C8F;

  /* Spacing Base (4px grid) */
  --spacing-base: 4px;

  /* Border Radii */
  --radius-none: 0px;
  --radius-sm: 4px;     /* Inputs, tags, badges, status chips */
  --radius-md: 8px;     /* Buttons, product cards, dialog panels */
  --radius-lg: 12px;    /* Slide-over drawers, sheets, bottom navigation */
  --radius-full: 9999px; /* Circular icons, count pills */

  /* Shadows (Used sparingly; hairlines preferred) */
  --shadow-none: none;
  --shadow-sm: 0 1px 2px 0 rgba(27, 43, 34, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(27, 43, 34, 0.08), 0 2px 4px -2px rgba(27, 43, 34, 0.04);
  --shadow-lg: 0 10px 15px -3px rgba(27, 43, 34, 0.10), 0 4px 6px -4px rgba(27, 43, 34, 0.05);
}

/* Admin Theme Scope */
.admin {
  --surface: #F6F7F5;
  --panel: #FFFFFF;
  --border: #DDE2DC;
  --paper: #F6F7F5;
  --paper-2: #EDF0EC;
  --rule: #DDE2DC;
}
```

### 3.2 Tailwind CSS v4 Configuration (`@theme` in `web/src/styles/app.css`)

```css
@import "tailwindcss";

@theme {
  /* Color Mappings */
  --color-paper: var(--paper);
  --color-paper-2: var(--paper-2);
  --color-ink: var(--ink);
  --color-ink-2: var(--ink-2);
  --color-leaf: var(--leaf);
  --color-leaf-2: var(--leaf-2);
  --color-sprout: var(--sprout);
  --color-tomato: var(--tomato);
  --color-tomato-ink: var(--tomato-ink);
  --color-turmeric: var(--turmeric);
  --color-rule: var(--rule);

  --color-ok: var(--ok);
  --color-warn: var(--warn);
  --color-warn-ink: var(--warn-ink);
  --color-err: var(--err);
  --color-info: var(--info);

  --color-admin-surface: var(--surface);
  --color-admin-panel: var(--panel);
  --color-admin-border: var(--border);

  /* Typography Font Stacks (DEC-005) */
  --font-sans: 'Be Vietnam Pro', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --font-display: 'Fraunces Variable', 'Fraunces', 'Be Vietnam Pro', Georgia, serif;
  --font-mono: ui-monospace, 'Cascadia Mono', 'SFMono-Regular', Menlo, Monaco, monospace;

  /* Border Radii */
  --radius-sm: var(--radius-sm);
  --radius-md: var(--radius-md);
  --radius-lg: var(--radius-lg);
  --radius-full: var(--radius-full);
}
```

---

## 4. Typography Scale & Hierarchy

### 4.1 Typeface Selection (`DEC-005`)
- **Body & UI Controls:** **Be Vietnam Pro** (SIL Open Font License 1.1, self-hosted via `@fontsource/be-vietnam-pro@5.3.0`, weights `400`, `500`, `600`, `700`). Designed natively for the Vietnamese language with optimized diacritic mark placement and horizontal proportions.
- **Editorial Headlines & Display:** **Fraunces Variable** (SIL Open Font License 1.1, self-hosted via `@fontsource-variable/fraunces@5.3.0`, weights `600`, `700`, optical size `opsz 9..144`). Imparts warmth, artisanal personality, and editorial dignity to storefront heroes and category titles.
- **Numeric Tabular Guarantee:** All prices, stock counts, quantities, countdowns, and table data use `font-variant-numeric: tabular-nums` (`tabular-nums` class in Tailwind) to ensure vertical digit alignment across varying values.

### 4.2 Type Scale (1.20 Minor Third Scale, Base 16px)

| Token | Size (px / rem) | Weight | Line Height | Tracking | Font Family | Typical Usage |
|---|---|---|---|---|---|---|
| `display` | `42px / 2.625rem` | Bold (`700`) | `1.20` (`50px`) | `-0.02em` | Fraunces Variable | Editorial Homepage Hero headline |
| `h1` | `34px / 2.125rem` | Bold (`700`) | `1.20` (`41px`) | `-0.015em` | Fraunces Variable | Major page title (Category/Aisle name, PDP title) |
| `h2` | `28px / 1.75rem` | SemiBold (`600`) | `1.25` (`35px`) | `-0.01em` | Fraunces / Be Vietnam Pro | Section titles ("Hôm nay nên mua", "Sản phẩm bán chạy") |
| `h3` | `23px / 1.4375rem`| SemiBold (`600`) | `1.30` (`30px`) | `-0.005em` | Be Vietnam Pro | Group headings, modal titles, sub-shelf headers |
| `h4` | `19px / 1.1875rem`| SemiBold (`600`) | `1.35` (`26px`) | `0em` | Be Vietnam Pro | Large display price (`PriceTag`), card titles, drawer headers |
| `body-lg` | `16px / 1.0rem` | Regular (`400`) / Medium (`500`) | `1.50` (`24px`) | `0em` | Be Vietnam Pro | Primary reading body, product descriptions, cart item names |
| `body-sm` | `14px / 0.875rem`| Regular (`400`) / Medium (`500`) | `1.50` (`21px`) | `0em` | Be Vietnam Pro | Secondary text, facet filter labels, table cell content |
| `caption` | `12px / 0.75rem` | Medium (`500`) / SemiBold (`600`)| `1.40` (`17px`) | `+0.01em` | Be Vietnam Pro | Unit prices (`₫/kg`), timestamps, badge text, input error helpers |

### 4.3 Diacritic Protection Guardrails
Because Vietnamese tones frequently stack accents (e.g. `ẳ`, `ỡ`, `ợ`, `ữ`, `ế`), containers with `overflow: hidden` must maintain **line-height $\ge 1.20$ on headings and $\ge 1.50$ on body text** with adequate top and bottom padding ($\ge 4\text{ px}$) to prevent diacritic clipping on mobile screens.

---

## 5. Spacing Scale & Layout Grid

### 5.1 Spacing Scale (4-px Base Grid)

| Spacing Token | Pixels | Rem Equivalent | Semantic Role & Application |
|---|---|---|---|
| `space-0` | `0px` | `0rem` | Reset / none |
| `space-1` | `4px` | `0.25rem` | Micro spacing (icon-to-badge margin, tag padding vertical) |
| `space-2` | `8px` | `0.5rem` | Tight spacing (button padding horizontal inside tags, icon-to-label gap) |
| `space-3` | `12px` | `0.75rem` | Compact element gap (input internal padding, list item vertical gap) |
| `space-4` | `16px` | `1.0rem` | Base component padding (card padding, mobile screen gutters, form field stack) |
| `space-5` | `20px` | `1.25rem` | Intermediate spacing (table cell horizontal padding, filter row gap) |
| `space-6` | `24px` | `1.5rem` | Medium block gap (tablet screen gutters, section header to content gap) |
| `space-8` | `32px` | `2.0rem` | Desktop screen gutters, spacing between minor content sections |
| `space-10` | `40px` | `2.5rem` | Major group separation on desktop |
| `space-12` | `48px` | `3.0rem` | Desktop section spacing ("Hôm nay nên mua" to Aisle strip) |
| `space-16` | `64px` | `4.0rem` | Major page section margins (Hero to browsing rails) |
| `space-20` | `80px` | `5.0rem` | Wide layout landing band spacing |
| `space-24` | `96px` | `6.0rem` | Editorial footer separation on wide screens |

### 5.2 Responsive Layout Grid

GroceryHub enforces a fluid container with responsive gutter steps:

| Viewport Profile | Target Width | Min Breakpoint | Grid Columns | Screen Margin / Gutter | Max Container Width |
|---|---|---|---|---|---|
| **Mobile Compact** | `360px` – `375px` | `0px` | 4 columns | `16px` margin / `12px` gutter | Full width (`100%`) |
| **Mobile Standard** | `390px` – `430px` | `390px` | 4 columns | `16px` margin / `16px` gutter | Full width (`100%`) |
| **Tablet Portrait** | `768px` | `md` (`768px`) | 8 columns | `24px` margin / `20px` gutter | `720px` |
| **Desktop Standard**| `1280px` | `xl` (`1280px`)| 12 columns | `32px` margin / `24px` gutter | `1240px` (Storefront max) |
| **Wide Desktop** | `1536px` + | `2xl` (`1536px`)| 12 columns | Fluid / centered | `1240px` (Storefront) / `1440px` (Admin) |

#### Responsive Rule Summary:
- **Storefront Max Content Width:** `1240px`, centered via `mx-auto` with responsive padding `px-4 md:px-6 xl:px-8`.
- **Admin Shell Max Content Width:** Fluid up to `1600px` for dense data tables, preserving fixed-width sidebars (`260px` expanded, `64px` collapsed).

---

## 6. Border Radius & Shape System

GroceryHub intentionally avoids "pill-everything" styling (which gives modern SaaS an undifferentiated, AI-generated appearance). Rounded geometries are purposeful and tiered:

| Token | Value | Target UI Elements | Rationale |
|---|---|---|---|
| `--radius-none` | `0px` | Hairline dividers, full-width section bands, table borders | Crisp market ledger look |
| `--radius-sm` | `4px` | Inputs, dropdown menus, tag chips, status badges | Subtle softening while keeping structure |
| `--radius-md` | `8px` | Action buttons, product cards (`ProductTile`), dialog modals | Tactile, thumb-friendly hit target |
| `--radius-lg` | `12px` | Mobile bottom sheets, slide-over cart drawers, floating panels | Smooth containment for overlay layers |
| `--radius-full` | `9999px` | Circular icon buttons, notification count badges | Unambiguous circular pill indicator |

---

## 7. Elevation, Borders & Hairlines

- **Hairline Dividers (`--rule` `#D9D1BD`):** GroceryHub relies on `1px solid var(--rule)` dividers and background contrast (`--paper` vs `--paper-2`) to delineate visual hierarchy rather than multi-layered card drop shadows.
- **Shadow Philosophy:** Shadows are reserved strictly for elements that physically float above the canvas in the z-axis:
  - `shadow-sm`: Interactive button/card hover lift (`translate-y-[-1px]`).
  - `shadow-md`: Contextual dropdown menus, comboboxes, and mobile sticky CTA bars.
  - `shadow-lg`: Slide-over cart sheets and confirmation dialogs.
  - Modals and sheets pair `shadow-lg` with a soft ink backdrop: `rgba(27, 43, 34, 0.45)`.

---

## 8. Motion Tokens & Accessibility Guardrails

GroceryHub motion is brief, crisp, and functional:
- **Fast Micro-interaction:** `120ms` ease-out (`transition-all duration-120 ease-out`) for hover, active presses, focus rings, and toggle switches.
- **Surface Transitions:** `200ms` ease-out (`transition-transform duration-200 ease-out`) for slide-over drawers, mobile filter sheets, and accordions.
- **Fly-to-Cart Animation:** Item tag flies toward cart badge in $\le 350\text{ ms}$ with cubic-bezier `(0.16, 1, 0.3, 1)`.
- **Mandatory Accessibility Override (`prefers-reduced-motion`):**
  ```css
  @media (prefers-reduced-motion: reduce) {
    *, ::before, ::after {
      animation-duration: 0.01ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.01ms !important;
      scroll-behavior: auto !important;
    }
  }
  ```
