# Phase 08 — Customer Frontend (storefront) + Reviews backend

**Goal:** The complete storefront on React/Vite using the real API and the GroceryHub design system, plus the Reviews module (backend) needed by product pages.
**Agents:** frontend-developer (scaffold) → ui-ux-designer (tokens/primitives code) → frontend-developer (pages) ‖ backend-developer (reviews, CR fixes) → qa-engineer (E2E, a11y) → ui review; security-reviewer (token handling).
**Inputs:** FR-CUS-001..017, BR-09; specs 07, 08; OpenAPI contract; Phase 06 tokens/recipes.
**Outputs:** `web/` app (app shell, storefront routes), generated API client, tokens/themed primitives, Reviews module, Playwright journeys J1/J3/J7 (COD), screenshots, Lighthouse reports.
**Dependencies:** Phase 07 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 08 — CUSTOMER FRONTEND. Requirements: FR-CUS-001..FR-CUS-017, BR-09, NFR-002, NFR-006, NFR-007. Load skills react-frontend, ui-ux-design. Obey .agents/rules/frontend.md and ui-ux.md and docs/spec/08. The backend is real: run it with docker compose; no mock data in the app.

Sequence:
1. frontend-developer (FIRST - owns web/): scaffold web/ (Vite + React 19 + TS strict, Tailwind v4, shadcn/ui init with the Radix/Base UI decision from DECISIONS.md, TanStack Router/Query, RHF + Zod, openapi-typescript/openapi-fetch with `npm run api:generate` reading docs/contracts/openapi.json, ESLint/Prettier, Vitest), app shell, auth (in-memory access token, refresh-on-401 once, route guards), i18n vi.ts, formatVnd/date utils, folder layout from .agents/rules/frontend.md. Commit and tell the designer the scaffold is ready.
2. ui-ux-designer (after the scaffold exists): implement the design docs from Phase 06 as code: tokens in web/src/shared/tokens + Tailwind v4 @theme in web/src/styles, self-hosted fonts (verified Vietnamese glyphs), themed shadcn primitives in web/src/shared/ui (Button, Input, Select, Dialog/Sheet, Drawer, Tabs, Accordion, Toast, Skeleton, Badge, Table basics), signature components (PriceTag, AisleNav, ReceiptSummary, StockBadge, DeliveryStrip, ProductTile/Row) and a /__design dev route showing all states. Deliver/update docs/design/{tokens,contrast,components,recipes,content-guidelines}.md. Run /wf-ui-review on the design route.
2b. frontend-developer (after step 2): build pages in this order: Home, Category/Aisle, Search, Product, Cart (guest + merge), Auth pages, Account (profile, addresses with Province/Ward selects), Checkout (COD; payment methods come from the API's availablePaymentMethods; QR shown disabled with honest copy until Phase 10), Order confirmation, Order history + tracking + cancel + reorder, Reviews UI. Every page implements loading/empty/error/partial states and responsive layouts per recipes. No emoji. Page recipes must differ as specified. FR-CUS-018 (wishlist) is NOT built unless the user approves it.
3. backend-developer (parallel): Reviews module (BR-09: verified purchase eligibility from Ordering contract, one per order line, moderation states, aggregates updated via outbox handler, 7-day edit window, endpoints in doc 07), plus CRs from frontend (contract fixes) — through CR notes only.
4. qa-engineer: Playwright E2E J1 (COD), J3 (n/a until Phase 10: skip is NOT allowed — mark journey as 'not in scope this phase' in test plan, do not write a skipped test), J7 mobile; axe checks on Home, Category, Product, Cart, Checkout; Vitest for hooks/forms; run Lighthouse (mobile) on Home/Category/Product.
5. ui-ux-designer: run /wf-ui-review for every storefront page with real screenshots at 360/768/1280/1920 px against the running stack; file CR notes; frontend fixes; repeat until PASS.
6. security-reviewer: verify no tokens in browser storage, XSS handling of review text, CSP compatibility, no client-side payment/price logic.
7. You: integrate, clean-checkout (backend + frontend), verify-reality, /wf-progress-review, report.
```

## Acceptance criteria
- All storefront pages in doc 08 §5 exist, use real API data, and follow their distinct recipes (UI review PASS with screenshots saved at 4 widths).
- `npm run lint`, `typecheck`, unit tests, `build` pass; generated API types in sync (CI check regenerates and diffs).
- E2E J1 passes against the compose stack; J7 passes at 360 px.
- axe: 0 serious/critical on key pages. Lighthouse mobile: Performance ≥ 85, LCP < 2.5 s, CLS < 0.1 on Home/Category/Product (or documented, user-accepted exceptions).
- No localStorage/sessionStorage token usage (`verify-reality` clean). No emoji in UI source.
- Reviews: eligibility, moderation states and aggregates verified by integration tests.
- Anti-AI-look checklist signed in the UI review report.

## How to verify
Open the app at 360 and 1280 px and walk J1 manually; run the E2E suite; open `docs/evidence/ui/`; run Lighthouse; try to review a product you did not buy.

## Gate to Phase 09
All PASS with evidence; UI review PASS; PM GO.
