# 04 — GitHub / UI Research

Research date: 14 Nov 2026. Repositories were checked on GitHub or through indexed READMEs; star counts and licences can change — agents must re-check before copying code. **No repository is adopted wholesale**; GroceryHub has its own design system (doc 08).

## 1. User-supplied repositories
### 1.1 Giogfr/aura-storefront
- Link: https://github.com/Giogfr/aura-storefront
- Tech: React 19, TypeScript (strict), Vite, Tailwind CSS v4, lucide-react, Vitest; no router library (query-string navigation), React Context state.
- Purpose: front-end-only e-commerce storefront template (shop with faceted filters, product page, slide-out cart/wishlist, multi-step checkout, search overlay). README states checkout/accounts/orders/reviews are **simulated client-side**.
- Pros: Same core stack as GroceryHub; solid UX patterns (faceted filtering, sticky add-to-cart, free-shipping progress, accessible dialogs, reduced-motion support); unit-tested pure helpers; CI.
- Cons: very new/low adoption (0 stars at check time); fashion-oriented look; no backend; Unsplash demo images (licence for commercial use must be handled separately); custom navigation instead of a router.
- Licence: MIT (code). Images are not covered.
- Antigravity fit: Good as a read-only reference (small repo, easy to read).
- Reusable: UX flows and cart/checkout state-machine ideas, accessibility patterns, utility tests as inspiration. Do not copy visual style or demo data.
- Conversion needed: Replace context-state with TanStack Query + server cart; replace query-string navigation with TanStack Router; replace simulated checkout with real API.

### 1.2 Kiranism/next-shadcn-dashboard-starter
- Link: https://github.com/Kiranism/next-shadcn-dashboard-starter
- Tech: Next.js 16 (App Router), React 19, TypeScript, Tailwind v4, shadcn/ui (v1.0.0 is the last Radix-based tag; development continues on Base UI from v2.0.0), Clerk auth, nuqs, React Query, Zustand, Recharts, kbar; Docker files included.
- Purpose: admin dashboard starter with working data tables (server prefetch, URL-synced filters), kanban, charts, product CRUD pages.
- Pros: well structured features/ folders; good data-table patterns; large community (thousands of stars, MIT).
- Cons: Next.js-specific (server components, App Router, Clerk) — not Vite-compatible without rewriting; Clerk auth irrelevant; look is generic SaaS.
- Licence: MIT.
- Antigravity fit: Medium (big repo; agents may over-copy).
- Reusable: data-table + URL-state patterns, feature folder structure, kbar command palette idea.
- Conversion: remove Next/Clerk; replace route files with TanStack Router; replace server prefetch with Query loaders; re-skin with GroceryHub tokens.

### 1.3 masondevx/orbynadmin
- Link: https://github.com/masondevx/orbynadmin
- Tech: Next.js 16, React 19, Tailwind v4, shadcn/ui (radix-ui), TanStack Table, Recharts, cmdk, sonner, date-fns, next-themes.
- Purpose: free admin template with industry dashboards (healthcare, logistics, HR, real estate), project board, data table, theme customizer. All data is mock data.
- Pros: MIT; broad dashboard layouts (logistics dashboard is relevant to order/delivery views); reusable data table.
- Cons: Next.js; mock data everywhere; very recent, small community; heavy demo surface not needed.
- Licence: MIT (README states everything MIT).
- Fit: Low–Medium; reference only for dashboard compositions.
- Conversion: same as 1.2.

### 1.4 shadcnstore/shadcn-dashboard-landing-template (also published under silicondeck/…)
- Link: https://github.com/shadcnstore/shadcn-dashboard-landing-template
- Tech: Two parallel builds: **Vite + React 19 + React Router + Tailwind v4 + shadcn/ui** and Next.js; Zustand, React Hook Form, Zod; 30+ pages (dashboard variants, auth, mail/tasks/chat/calendar apps, landing page).
- Purpose: free admin dashboard + landing page template.
- Pros: Vite version exists (direct fit); MIT; theme customizer (tweakcn) concept; many pages to borrow layout ideas from.
- Cons: Large demo surface (mail/chat/calendar irrelevant); generic SaaS look; React Router (we use TanStack Router).
- Licence: MIT.
- Fit: Good for studying Vite structure; copy selectively.
- Reusable: sidebar/layout scaffolding, auth page layouts, chart wrappers, form patterns.

## 2. Additional repositories found
| Repo | Tech | Licence | Verdict |
|---|---|---|---|
| **satnaing/shadcn-admin** — https://github.com/satnaing/shadcn-admin | Vite + React + TanStack Router + shadcn/ui (Radix) + Tailwind, ESLint/Prettier/Knip | MIT | **Best Vite-native admin shell reference.** Responsive, accessible, sidebar + global command search, light/dark. Recommended as the structural base for `web/src/admin` (copy selectively, re-skin). Re-verify Tailwind/shadcn versions. |
| dionola/ecommerce — https://github.com/dionola/ecommerce | React + Vite client with **ASP.NET Core (SQL Server) backend** option, Stripe checkout, admin panel | Not verified (no licence seen) | Architecture reference only (React+Vite ↔ ASP.NET Core). Do not copy code without a licence. |
| Adminiumjs/ecommerce-storefront | Vite + React storefront, no CSS framework | **AGPL-3.0** | **Do not copy** (copyleft). Observe only the DataSource abstraction idea. |
| chandumaram/grocery-store-landing and similar small grocery repos | React + Tailwind landing pages | Mixed/unclear | Too small/low quality; ignore. |
| BoroBazar (commercial template, Next.js) | Next.js + Tailwind grocery storefront | Commercial | Inspiration for grocery IA only; no code reuse. |

Finding: there is **no mature open-source grocery storefront** on React + Vite + ASP.NET Core to adopt. GroceryHub's storefront should be custom-designed; admin can borrow from `satnaing/shadcn-admin`.

## 3. Recommended reference set
| Need | Reference | How to use |
|---|---|---|
| Customer storefront UX | aura-storefront (patterns), plus public study of Vietnamese grocers (Bách Hóa Xanh, WinMart/WinMart+, Tiki Ngon, GrabMart) and international (Instacart, Ocado, Picnic) | Patterns only: aisle navigation, substitution/notes, per-unit price, delivery slot clarity. No copying of assets or layouts. Agents should capture screenshots of public sites for the design review **as private references**, not into the repo. |
| Admin UI | satnaing/shadcn-admin (structure), shadcnstore Vite template (page ideas), Kiranism starter (data tables + URL state) | Rebuild with GroceryHub tokens |
| Component library | shadcn/ui (Radix or Base UI variant; pick one at setup and stay consistent), cmdk for command palette, sonner for toasts, TanStack Table, Recharts | |
| Icons | lucide-react + custom SVGs for produce/meat/dairy etc. | No emoji |
| Animation | CSS + `motion` sparingly | |
| Fonts | Be Vietnam Pro + display serif (Fraunces candidate), self-hosted | Verify Vietnamese glyphs |
| Product imagery system | Uploaded originals → server-side derivatives (WebP/AVIF, 4:5 and 1:1 crops, 320/640/960 px), alt text required, consistent neutral background guideline for photographers; dev placeholders clearly labelled "DEV" | Never ship stock images with unclear licences |

## 4. Tasks for Phase 1 (research confirmation)
1. Re-open each repository, record commit date, licence file, and last release; update this table.
2. Capture 6–10 reference screenshots per source (private notes) and write observations (what to emulate / avoid) into `docs/design/references.md`.
3. Decide Radix vs Base UI variant of shadcn/ui and record in `docs/progress/DECISIONS.md`.
