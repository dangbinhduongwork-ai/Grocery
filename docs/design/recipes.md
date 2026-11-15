# GroceryHub — Storefront Page Recipes & UX Specifications (`recipes.md`)

- **Design System:** **"Sổ chợ"** (The Market Ledger)
- **Requirements Covered:** `FR-CUS-001` (Homepage), `FR-CUS-002` (Category Tree), `FR-CUS-003` (Search & Filters), `FR-CUS-004` (Product Detail Page), `FR-CUS-016` (Reviews), `FR-CUS-017` (Responsive UI 360–1920 px & WCAG 2.2 AA), `NFR-002` (Web Performance), `NFR-006` (Accessibility), `NFR-007` (Vietnamese UI Localization).
- **Approved Decisions:** `DEC-004` (Radix UI shadcn/ui primitives), `DEC-005` (Self-hosted fonts: Be Vietnam Pro & Fraunces), `DEC-007` (Shipping zones & thresholds), `DEC-009` (WCAG 2.2 AA Contrast Guardrails: `--tomato-ink` `#B8321F`, `--warn-ink` `#8A560D`), `DEC-011` (`MaxQuantityPerLine = 20`).
- **Design Tokens:** [tokens.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer-Phase-06-Design-Docs-ui-ux-designer-8568a0c4/docs/design/tokens.md)
- **Contrast Calculations:** [contrast.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer-Phase-06-Design-Docs-ui-ux-designer-8568a0c4/docs/design/contrast.md)
- **Component Specifications:** [components.md](file:///C:/Users/Admin/.gemini/antigravity/worktrees/d92f5437-cefe-4593-bfb8-546ce3fd7f4f/subagent-UI-UX-Designer-Phase-06-Design-Docs-ui-ux-designer-8568a0c4/docs/design/components.md)

---

## 1. Storefront UX Design Principles

GroceryHub treats online grocery shopping as a purposeful, recurring domestic task — not an aimless social media feed or a SaaS dashboard. The storefront adheres to four foundational rules:
1. **Asymmetric Editorial Composition:** Every page type features a distinct composition rooted in physical store layouts. No centered gradient hero blobs, no purple button clusters, and no repetitive "card soup".
2. **Transparent Freshness & Pricing:** Strikethrough list prices, bold discounted selling prices, unit price comparisons (`₫/kg`, `₫/chai`), and batch freshness dates are presented without clutter or deceptive dark patterns.
3. **Vietnamese Market Mental Models:** Aisles (*Rau củ quả*, *Thịt cá trứng*, *Gia vị*), packaging units (*gói*, *khay*, *bó*, *lốc*, *vỉ*), and evening cutoff rules (18:00) align with Vietnamese domestic shopping routines.
4. **Resilient Multi-State Coverage:** Every screen is fully specified for 5 discrete operational states: **Initial Loading** (content-shaped skeletons), **Empty**, **Error** (RFC 7807 ProblemDetails integration), **Partial Availability**, and **Offline**.

---

## 2. Page Recipe 1: Homepage (`FR-CUS-001`)

### 2.1 User Intent & Primary Task
- **Primary Task:** Rapidly orient to today's fresh arrivals and promotional staples, search for grocery essentials, and navigate directly into specialized aisles.
- **Primary Action:** Search bar input or clicking a curated seasonal product card.

### 2.2 Layout Structure & Sectional Breakdown

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ STOREFRONT HEADER: Logo ("GroceryHub") | Search Bar | Delivery Zone | Cart  │
├─────────────────────────────────────────────────────────────────────────────┤
│ 1. HERO BANNER (Asymmetric 7/12 Editorial + 5/12 Fresh Mosaic)              │
│    - Left: Headline in Fraunces 700 + Delivery Promise + Search Suggestion  │
│    - Right: High-resolution real product photography mosaic (neutral ground)│
├─────────────────────────────────────────────────────────────────────────────┤
│ 2. AISLE STRIP: Horizontal pills with item counts ("Rau củ quả (142)", ...) │
├─────────────────────────────────────────────────────────────────────────────┤
│ 3. CURATED RAIL: "Hôm nay nên mua" (ProductTiles with MarketTagPrice)       │
├─────────────────────────────────────────────────────────────────────────────┤
│ 4. PROMO BAND: Turmeric hairline coupon card ("Nhập CHOBANMOI giảm 30k")    │
├─────────────────────────────────────────────────────────────────────────────┤
│ 5. EDITORIAL CATEGORY FEATURES: 2 Split Cards (Rau Hữu Cơ & Hải Sản Sống)   │
├─────────────────────────────────────────────────────────────────────────────┤
│ 6. BEST SELLERS: High-density list (ProductRow) for rapid shopping          │
├─────────────────────────────────────────────────────────────────────────────┤
│ 7. TRUST STRIP: Delivery promise, Freshness guarantee, COD & VietQR safety  │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Section Details:
1. **Asymmetric Editorial Hero Banner:**
   - **Desktop Layout (`≥ 1024px`):** Split 7/12 left and 5/12 right.
     - Left Column: Editorial headline set in `Fraunces Variable` 700 (`42px`, `text-display`, `var(--ink)`): *"Đi chợ tươi ngon mỗi sáng — Giao nhanh tận bếp."*
     - Subtitle in `Be Vietnam Pro` 400 (`16px`, `text-body-lg`, `var(--ink-2)`): *"Nông sản sạch Đà Lạt, thịt cá tươi sống nhập mới mỗi ngày trước 06:00. Đổi trả tận tay nếu không hài lòng."*
     - Delivery Promise Pill: `var(--paper-2)` container with `Clock3` line icon: *"Đặt trước 18:00 — Giao trong ngày tại TP.HCM & lân cận"*.
     - Quick Search Bar: Full-width search input with leading search icon and popular search tags (*Cải bó xôi*, *Thịt ba chỉ*, *Trứng gà ta*, *Sữa tươi*).
     - **Anti-AI Rule:** Strictly NO centered headline over a purple/blue mesh gradient; NO floating mockups.
     - Right Column: A curated 3-photo mosaic of authentic Vietnamese farm produce (leafy greens, vine tomatoes, tropical fruit) on off-white neutral ground (`#F2EDE0`) with crisp hairline borders (`1px solid var(--rule)`).
   - **Mobile Layout (`< 768px`):** Stacked. Editorial headline (`28px`), compact delivery guarantee pill, quick search input, followed by a single full-width featured seasonal banner image (ratio 16:9).
2. **Aisle Navigation Pills Strip:**
   - Persistent horizontal scrolling rail on mobile / flex wrap on desktop.
   - Text links styled as clean market tabs with active leaf indicator:
     - *"Tất cả"*
     - *"Rau củ quả (142)"*
     - *"Thịt cá trứng (89)"*
     - *"Sữa & chế phẩm (54)"*
     - *"Đồ khô & gia vị (120)"*
     - *"Đồ uống (65)"*
     - *"Trái cây tươi (48)"*
   - Active pill: Background `var(--leaf)`, text `var(--paper)`. Inactive: Background `var(--paper-2)`, border `1px solid var(--rule)`, text `var(--ink)`.
3. **Curated Seasonal Carousel ("Hôm nay nên mua"):**
   - Section Header in `Fraunces Variable` 600 (`28px`, `text-h2`): *"Hôm nay nên mua — Tươi ngon vào vụ"*.
   - Horizontal snap carousel (`scroll-snap-type: x mandatory`) displaying 4 to 6 `ProductTile` components.
   - Each tile includes: 4:5 real product photo, `FreshnessNote` badge ("Mới thu hoạch hôm nay"), product title (max 2 lines), `MarketTagPrice` (selling price in `--tomato-ink`, strikethrough list price, discount notch `-20%`), and quick add-to-cart button.
4. **Promotional Coupon Band:**
   - Full-width band with background `var(--paper-2)` and `1px solid var(--turmeric)`.
   - Content: Turmeric notch icon + bold voucher code `"CHOBANMOI"` + description *"Giảm 30.000 ₫ cho đơn hàng từ 250.000 ₫ dành cho khách hàng mới"* + "Lưu mã" button.
5. **Editorial Category Features:**
   - Two equal columns (`50% / 50%` desktop, stacked mobile).
   - Real photography + solid paper text card:
     - Left: *"Rau củ hữu cơ Đà Lạt — Đạt chuẩn VietGAP, thu hái tươi trong ngày"*. CTA: *"Khám phá nông trại"*.
     - Right: *"Thịt sạch & Thủy hải sản — Truy xuất nguồn gốc rõ ràng, không kháng sinh"*. CTA: *"Xem thực đơn"*.
6. **Best Sellers Section ("Sản phẩm bán chạy nhất"):**
   - High-density list format using `ProductRow` rather than large tiles. Enables power-shoppers to scan 8–10 items quickly and adjust quantities with inline `QuantityStepper` controls without navigating away.
7. **Grocery Trust Strip:**
   - 3 clean columns separated by hairline dividers (`var(--rule)`):
     - `Truck`: *"Giao hàng đúng hẹn — Đảm bảo chuỗi lạnh cho thực phẩm tươi."*
     - `ShieldCheck`: *"Cam kết độ tươi — Đổi trả miễn phí trong ngày nếu hàng dập hỏng."*
     - `CreditCard`: *"Thanh toán an toàn — Hỗ trợ COD và chuyển khoản VietQR tự động."*

### 2.3 Responsive Behavior Matrix

| Viewport | Container | Hero Composition | Aisle Strip | Curated Rail | Best Sellers |
|---|---|---|---|---|---|
| **Mobile (`375px`)** | `100%`, `px-4` | Stacked headline + search bar; 16:9 banner | Horizontal drag scroll | 1.5 tiles visible (snap) | Single-column `ProductRow` list |
| **Tablet (`768px`)** | `720px`, `px-6` | Stacked headline + search; 2-photo grid | Horizontal wrap with gap | 2.5 tiles visible | 2-column `ProductRow` grid |
| **Desktop (`1280px`)**| `1240px`, `px-8`| Asymmetric 7/12 left + 5/12 mosaic | Clean wrapped pill list | 4 tiles grid/rail | 2-column `ProductRow` grid |
| **Wide (`1536px`)** | `1240px`, centered | Asymmetric 7/12 left + 5/12 mosaic | Clean wrapped pill list | 4-5 tiles grid | 2-column `ProductRow` grid |

### 2.4 State Specifications
- **Initial Loading State:** Content-shaped skeletons:
  - Hero skeleton: Left rectangle for headline (`h-12 w-3/4 bg-[#EBE5D6] animate-pulse rounded`), search bar skeleton (`h-11 w-full bg-[#EBE5D6] rounded-sm`). Right rectangle for photo mosaic (`h-[320px] bg-[#EBE5D6] rounded-md`).
  - Curated rail skeleton: 4 card skeletons with 4:5 image placeholder + 2 lines of text + price tag pill.
- **Empty State:** N/A (Homepage always renders curated catalog and static trust pillars).
- **Error State:** If catalog API fails (`ProblemDetails`):
  - Sticky warning alert at top of canvas in `var(--paper-2)` with `1px solid var(--tomato-ink)`:
    *"Không thể tải danh sách sản phẩm mới nhất lúc này. Vui lòng kiểm tra lại kết nối."*
  - Action: "Tải lại trang" (`Button variant="outline"`).
- **Partial State:** Per-rail error recovery: if the Curated Rail query fails, that individual section displays *"Tạm thời chưa có sản phẩm gợi ý"* while the rest of the homepage remains functional.
- **Offline State:** Top banner in `var(--tomato-ink)` with white text: *"Bạn đang ngoại tuyến. Vui lòng kiểm tra kết nối mạng."*

---

## 3. Page Recipe 2: Category / Aisle Page (`FR-CUS-002`, `FR-CUS-003`)

### 3.1 User Intent & Primary Task
- **Primary Task:** Drill down into a specific grocery aisle (*Rau củ quả*, *Thịt cá trứng*), filter by brand or price, and compare pack options.
- **Primary Action:** Selecting filters or adding products directly to cart.

### 3.2 Layout Structure & Sectional Breakdown

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ BREADCRUMB: Trang chủ  >  Rau củ quả  >  Rau lá xanh tươi                   │
├─────────────────────────────────────────────────────────────────────────────┤
│ AISLE HEADER: Title in Fraunces 700 ("Rau củ quả") • 142 sản phẩm           │
│ Sub-category filter pills: [Tất cả] [Rau lá xanh] [Củ & Quả] [Nấm tươi]     │
├──────────────────────────────────────┬──────────────────────────────────────┤
│ LEFT SIDEBAR (Desktop ≥ 1024px)      │ MAIN CONTENT AREA                    │
│ 1. AisleNav Tree (collapsible)       │ Top Toolbar:                         │
│ 2. Facet Filter Groups:              │ - Sort Dropdown: [Phổ biến nhất v]   │
│    - Khoảng giá (VND presets/slider) │ - Density Toggle: [Grid ▦] [List ☰]  │
│    - Thương hiệu (checkbox list)     │                                      │
│    - Tình trạng: [x] Còn hàng        │ Product Results:                     │
│    - Tiêu chuẩn: [x] VietGAP         │ - Grid view: Responsive ProductTiles │
│ 3. Active filter clear button        │ - Pagination / Load More Bar         │
└──────────────────────────────────────┴──────────────────────────────────────┘
│ MOBILE: Sticky Bottom Filter Bar ("Bộ lọc (2) • Sắp xếp")                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Section Details:
1. **Breadcrumb Hierarchy:**
   - Structured navigation: `Trang chủ` > `Rau củ quả` > `Rau lá xanh`.
   - Text in `var(--ink-2)` `14px`, linked with hover underline in `var(--leaf)`. Separators are forward slashes `/`.
2. **Aisle Header & Sub-Category Pills:**
   - Title set in `Fraunces Variable` 700 (`34px`, `text-h1`): e.g. *"Rau củ quả tươi"*.
   - Total product count indicator in `var(--ink-2)`: *"142 sản phẩm"*.
   - Sub-category navigation pills allowing 1-tap filtering within the current aisle.
3. **Left Sidebar (`AisleNav` + Facet Filters):**
   - Width: `260px` fixed, sticky on desktop (`top-24`).
   - Group 1 — **Aisle Tree (`AisleNav`):** Hierarchical category tree with product counts, active aisle marked with a 3px vertical `var(--leaf)` bar.
   - Group 2 — **Khoảng giá (Price Presets):**
     - Dưới 25.000 ₫
     - 25.000 ₫ – 50.000 ₫
     - 50.000 ₫ – 100.000 ₫
     - Trên 100.000 ₫
   - Group 3 — **Thương hiệu (Brand):** Searchable checkbox list (Dalat GAP, VinEco, Ba Huân, CP, v.v.).
   - Group 4 — **Tình trạng hàng hóa:** Checkbox *"Chỉ hiện sản phẩm còn hàng"*.
   - Group 5 — **Tiêu chuẩn chất lượng:** Checkbox *"Chứng nhận VietGAP / Hữu cơ"*.
   - Active Filter Pill Bar: Shows active filters with an `(X)` dismiss button and a "Xóa tất cả" link.
4. **Top Sorting & Density Toolbar:**
   - Sort Dropdown (`DropdownMenu`):
     - *Phổ biến nhất* (Relevance)
     - *Bán chạy nhất* (Best-selling)
     - *Giá: Thấp đến Cao* (Price ascending)
     - *Giá: Cao đến Thấp* (Price descending)
     - *Hàng mới về* (Newest)
   - Density Toggle: Switches between **Grid (`ProductTile`)** (default for desktop browse) and **List (`ProductRow`)** (default for mobile).
5. **Product Results Grid:**
   - 3 columns on tablet, 4 columns on desktop.
   - Every card renders:
     - 4:5 real photography ratio on neutral `#F2EDE0` ground.
     - `StockBadge` ("Còn hàng" / "Sắp hết (còn 3)").
     - Title in Be Vietnam Pro SemiBold (`16px`, 2-line clamp).
     - `MarketTagPrice` with selling price, list price, and unit metric (`₫/kg`).
     - Interactive Add-to-Cart button or inline `QuantityStepper` once added.
6. **Mobile Filter Sheet (`MobileFilterSheet`):**
   - On screens `< 1024px`, the left sidebar hides.
   - A sticky bottom bar appears: `"Bộ lọc (2) • Sắp xếp"`.
   - Tapping opens a full-height bottom `Sheet` with accordion filter sections, a reset button, and a prominent primary CTA: *"Xem 142 sản phẩm"*.

### 3.3 State Specifications
- **Initial Loading State:**
  - Sidebar: Skeleton rectangle for category list + 3 skeleton checkboxes.
  - Main area: 8 `ProductTile` card skeletons with 4:5 image placeholder, 2-line title skeleton, and price pill.
- **Empty Filter State:**
  - Trigger: User selects filter combinations yielding 0 results (e.g. price > 500k in Vegetables).
  - Visual: Line icon `FilterX` (`48px`) in `var(--ink-2)`.
  - Headline in Fraunces 600 (`23px`): *"Không tìm thấy sản phẩm phù hợp"*.
  - Body: *"Không có sản phẩm nào khớp với các bộ lọc bạn đã chọn. Hãy thử xóa bớt bộ lọc hoặc xem danh mục khác."*
  - Primary Action: `Button variant="default"`: *"Xóa tất cả bộ lọc"*.
- **Error State:**
  - Centered notice panel with `AlertTriangle` in `var(--tomato-ink)`.
  - ProblemDetails message: *"Không thể tải danh sách sản phẩm thuộc danh mục này. Mã lỗi: ERR_CATALOG_FAIL."*
  - Action: "Thử lại".
- **Partial State:**
  - Items that become out of stock during browsing display an overlaid banner: *"Vừa hết hàng"*, with disabled add-to-cart button.

---

## 4. Page Recipe 3: Product Detail Page - PDP (`FR-CUS-004`, `FR-CUS-016`)

### 4.1 User Intent & Primary Task
- **Primary Task:** Inspect product quality, origin, unit price, freshness promise, and customer reviews before making a purchase decision.
- **Primary Action:** Clicking "Thêm vào giỏ" (Add to Cart).

### 4.2 Layout Structure & Sectional Breakdown

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ BREADCRUMB: Trang chủ > Rau củ quả > Cải bó xôi hữu cơ Đà Lạt              │
├──────────────────────────────────────┬──────────────────────────────────────┤
│ LEFT COLUMN (5/12 Desktop):          │ RIGHT COLUMN (7/12 Desktop):         │
│ 1. Large 4:5 Main Product Image      │ 1. Origin & Brand Tag (Dalat GAP)    │
│    (Zoom on hover / click modal)     │ 2. Product Title in Fraunces 700     │
│ 2. Thumbnail Strip (4-5 images)      │ 3. MarketTagPrice (Detailed mode):   │
│ 3. Freshness Guarantee Pill          │    - 48.000 ₫ (Tiết kiệm 12.000 ₫)   │
│                                      │    - 60.000 ₫ list price struck      │
│                                      │    - 96.000 ₫/kg • Đóng gói khay 500g│
│                                      │ 4. StockBadge ("Còn hàng")           │
│                                      │ 5. FreshnessNote ("Hạn dùng ≥ 5 ng") │
│                                      │ 6. Pack Size Selector (Khay 500g, 1kg)│
│                                      │ 7. QuantityStepper & Add-to-Cart CTA │
│                                      │ 8. Delivery ETA Strip (TP.HCM: 2h)   │
├──────────────────────────────────────┴──────────────────────────────────────┤
│ 9. PRODUCT DETAILS & SPECS TABLE (Hairlines, not card soup)                  │
├─────────────────────────────────────────────────────────────────────────────┤
│ 10. CUSTOMER REVIEWS (FR-CUS-016): Verified Purchases, 4.8/5★, Breakdown    │
├─────────────────────────────────────────────────────────────────────────────┤
│ 11. RELATED PRODUCTS RAIL: "Sản phẩm cùng loại"                              │
└─────────────────────────────────────────────────────────────────────────────┘
│ MOBILE: Sticky Bottom Bar (Thumb, Title, Price, Stepper, Primary CTA)       │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Section Details:
1. **Responsive Image Gallery (Left 5/12):**
   - Main Image: 4:5 aspect ratio, neutral off-white background (`#F2EDE0`), minimum `1200px` source image loaded via responsive WebP/AVIF srcset.
   - Hover zoom: Subtle 1.5x magnifying lens on desktop (`prefers-reduced-motion: no-preference`).
   - Thumbnail Strip: Horizontal carousel of 4–5 angles (packaging, cross-section, cert badge). Active thumbnail marked with `2px solid var(--leaf)`.
2. **Purchase Control Panel (Right 7/12):**
   - Brand & Origin Link: `var(--leaf)` `14px` Medium: *"Thương hiệu: Dalat GAP • Xuất xứ: Lâm Đồng"*.
   - Product Title: `Fraunces Variable` 700 (`34px`, `text-h1`, `var(--ink)`): *"Cải bó xôi hữu cơ Đà Lạt (Organic Spinach)"*.
   - Detailed `MarketTagPrice`:
     - Selling price: `28px` Bold in `var(--tomato-ink)` (`48.000 ₫`).
     - List price: `16px` Strikethrough in `var(--ink-2)` (`60.000 ₫`).
     - Discount notch: Turmeric `-20%` with bold `--ink` text.
     - Unit price conversion: `"96.000 ₫/kg • Quy cách: Khay 500g"`.
   - Freshness & Stock Indicators:
     - `StockBadge`: `"Còn hàng"` with `CheckCircle2` in `var(--ok)`.
     - `FreshnessNote`: `"Hạn dùng còn ≥ 5 ngày"` with `Clock3` in `var(--leaf)` (strictly rendered only when database batch data exists).
   - Quantity Stepper & Add-to-Cart:
     - `QuantityStepper` bounded by `1` and `min(stock, 20)` (`BR-16`, `DEC-011`).
     - Primary Button: `Button variant="default" size="lg"`: *"Thêm vào giỏ hàng"* (height `48px`, width `100%` on mobile / `auto` on desktop).
     - Secondary Button: `Button variant="secondary" size="lg"`: *"Mua ngay"*.
   - Delivery Estimate Strip:
     - `Truck` icon in `var(--leaf)`: *"Giao hàng tiêu chuẩn trong 2 giờ hoặc chọn khung giờ nhận hàng tại bước thanh toán."*
3. **Product Description & Specifications Table:**
   - Description: 2–3 paragraphs of editorial copy set in `Be Vietnam Pro` `16px` (`line-height: 1.5`).
   - Specifications Table (Strict Anti-Card-Soup Rule: Formatted as a clean hairline table using `1px solid var(--rule)`):
     - *Xuất xứ:* Đà Lạt, Lâm Đồng, Việt Nam
     - *Tiêu chuẩn canh tác:* Hữu cơ USDA & VietGAP
     - *Quy cách đóng gói:* Khay màng bọc thực phẩm 500g
     - *Hướng dẫn bảo quản:* Ngăn mát tủ lạnh ở nhiệt độ 4°C – 8°C
     - *Hạn sử dụng:* 5 ngày kể từ ngày đóng gói
4. **Customer Reviews Section (`FR-CUS-016`):**
   - Header: *"Đánh giá từ khách hàng đã mua"* (Only customers with a `Delivered` order may review per `BR-09`).
   - Summary Metric: Large `4.8 / 5.0` star rating + star distribution bars (5★: 85%, 4★: 10%, 3★: 3%, 2★: 1%, 1★: 1%).
   - Review Cards: Formatted with hairline dividers (not heavy drop-shadow boxes):
     - Reviewer name + verified purchase badge (`Check` *"Đã mua hàng"* in `var(--ok)`).
     - Rating stars (`5/5`) + date of purchase.
     - Review content: *"Rau rất tươi, lá xanh non không bị dập, đóng gói sạch sẽ."*
     - Staff Moderation Reply: If store replied (`FR-ADM-013`), rendered in a subtle `var(--paper-2)` inset block:
       *"GroceryHub cảm ơn bạn đã tin tưởng ủng hộ nông sản Đà Lạt!"*
5. **Mobile Sticky Action Bar:**
   - Anchored to bottom of viewport on screens `< 1024px` when the primary add-to-cart button scrolls out of view.
   - Layout: Product thumbnail (`40x40px`), selling price (`18px` bold `var(--tomato-ink)`), compact `QuantityStepper`, and "Thêm vào giỏ" button ($44\text{ px}$ height).
   - Elevated with `var(--shadow-md)` and border-top `1px solid var(--rule)`.

### 4.3 State Specifications
- **Initial Loading State:** Skeleton gallery rectangle (4:5) + right panel skeleton lines (title, price, button pill).
- **Out of Stock State:**
  - `StockBadge` swaps to `OutOfStock` ("Hết hàng" with `XCircle` in `var(--tomato-ink)`).
  - Add-to-cart button is disabled and replaced by *"Nhận thông báo khi có hàng lại"*.
- **Error State:** If product ID is not found (`404` ProblemDetails):
  - Centered panel: *"Sản phẩm này không tồn tại hoặc đã ngừng kinh doanh."*
  - Action: "Quay lại danh mục sản phẩm" (`Button variant="default"`).

---

## 5. Page Recipe 4: Search Results Page (`FR-CUS-003`)

### 5.1 User Intent & Primary Task
- **Primary Task:** Locate specific grocery items quickly, even with typos, missing Vietnamese accents, or informal search terms.
- **Primary Action:** Refining search queries or adding matched products to cart.

### 5.2 Layout Structure & Sectional Breakdown

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ SEARCH HEADER BAR:                                                          │
│ [ 🔍 Tìm kiếm sản phẩm, rau củ, thịt cá...                       (X) ]     │
├─────────────────────────────────────────────────────────────────────────────┤
│ QUERY ECHO & RESULT COUNT:                                                  │
│ "Kết quả tìm kiếm cho: cà chua" • Tìm thấy 18 sản phẩm                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ 1. MATCHING AISLES BAR:                                                     │
│    Aisle phù hợp: [Rau củ quả (14)]  [Đồ khô & Gia vị (3)]  [Đồ hộp (1)]    │
├─────────────────────────────────────────────────────────────────────────────┤
│ 2. ACTIVE FILTERS & SORTING TOOLBAR:                                        │
│    Active pills: [Còn hàng (X)]  [Dưới 50.000 ₫ (X)]   |   Sort: [Phổ biến v]│
├─────────────────────────────────────────────────────────────────────────────┤
│ 3. PRODUCT RESULTS (ProductTile grid / ProductRow list)                     │
│    - Matched keywords highlighted (cà chua)                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│ 4. EMPTY / NO-RESULTS STATE (If 0 results found):                           │
│    - Icon SearchX + Friendly guidance                                       │
│    - Spelling suggestions ("Có phải bạn muốn tìm: Cà chua bi?")             │
│    - Suggested popular aisles links                                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Section Details:
1. **Search Header Bar:**
   - Dedicated search input with clear button `(X)` and search line icon.
   - Handles Vietnamese IME composition (`compositionstart` / `compositionend`).
   - Query string synchronized bidirectionally with URL: `?q=ca+chua&sort=relevance` (`FR-CUS-003`).
2. **Vietnamese Accent-Insensitive Highlighting:**
   - Whether the user types `"ca chua"` or `"cà chua"`, matches on *"Cà chua bi Đà Lạt"*, *"Sốt cà chua nguyên chất"*, and *"Cà chua Beef"* are highlighted with subtle background tint `var(--paper-2)` and font weight SemiBold.
3. **Matching Aisles Group:**
   - High-intent grocery shoppers often search for a category rather than a single item. If the query matches an aisle name or subcategory, display matching aisle links at the top of the results:
     - *"Aisle gợi ý: Rau củ quả (14 sản phẩm), Đồ khô & Gia vị (3 sản phẩm)"*.
4. **Results Grid:**
   - Identical in composition to the Category Aisle results grid (Section 3.2), retaining the `ProductTile` / `ProductRow` density toggle and pagination.
5. **No-Results Empty State:**
   - Trigger: Query matches 0 items in the catalog.
   - Icon: Line icon `SearchX` (`48px`) in `var(--ink-2)`.
   - Headline in `Fraunces Variable` 600 (`23px`): *"Không tìm thấy kết quả nào cho '{query}'"*.
   - Helpful Copy: *"Xin lỗi, chúng tôi không tìm thấy sản phẩm nào khớp với từ khóa của bạn. Hãy thử kiểm tra lỗi chính tả, sử dụng từ khóa tổng quát hơn, hoặc khám phá các danh mục phổ biến bên dưới:"*
   - Suggested Aisle Links:
     - `Button variant="outline"`: *"Rau củ quả tươi"*
     - `Button variant="outline"`: *"Thịt cá trứng"*
     - `Button variant="outline"`: *"Trái cây nội địa"*
     - `Button variant="outline"`: *"Sữa & Bánh kẹo"*

### 5.3 State Specifications
- **Initial Loading State:** Skeleton search bar + 6 card skeletons with shimmering pulse animation.
- **Empty Query State:** When visiting `/search` with no query: displays recent searches (stored locally) and popular trending search tags.
- **No-Results State:** Fully documented in Section 5.2.5.
- **Error State:** If search service is unavailable (`503` ProblemDetails):
  - Notice banner: *"Hệ thống tìm kiếm đang bảo trì trong giây lát. Vui lòng thử lại sau hoặc duyệt theo danh mục."*
  - CTA: "Về trang chủ".

---

## 6. Storefront State Checklist for UI Implementation Review (`/wf-ui-review`)

Frontend developers implementing storefront routes in Phase 08 must verify each screen against this exhaustive state checklist before filing for design review:

| State | Required Implementation Criteria | Anti-Pattern to Reject |
|---|---|---|
| **Initial Loading** | Content-shaped skeletons matching exact 4:5 image ratio, text line heights, and stepper geometry. Shimmering pulse animation (`animate-pulse`). | Generic centered spinning wheel; full-screen blank whiteout. |
| **Empty State** | Line icon (`FilterX`, `SearchX`, `ShoppingBag`), concrete Vietnamese explanation, single primary recovery CTA button. | Blank empty container with no guidance; English default copy ("No data found"). |
| **Error State** | Structured RFC 7807 ProblemDetails integration: clear user-facing message, correlation trace ID in small type, retry button. | Raw stack traces; alert dialog popups; untranslated HTTP 500 errors. |
| **Partial State** | In-stock badges updated dynamically; price change notifications; disabled CTA for sold-out items with restock alert signup. | Silent failure; allowing checkout of out-of-stock items. |
| **Offline State** | Persistent top notification banner in `var(--tomato-ink)`: *"Bạn đang ngoại tuyến. Vui lòng kiểm tra kết nối mạng."* | Unhandled crash; broken image placeholders without fallback styling. |
| **Accessibility** | All interactive targets $\ge 44 \times 44\text{ px}$ on mobile; focus rings 2px leaf with 2px offset; all text contrast $\ge 4.5:1$ (`contrast.md`). | Missing focus outline; low-contrast red text; non-keyboard accessible menus. |
| **Typography** | Diacritics rendering verified with test string `Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ ữ ế 25.000 ₫` across all weights. | Diacritic clipping in `overflow: hidden`; non-tabular numerals in prices. |
