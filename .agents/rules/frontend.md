---
trigger: glob
globs: "web/**/*.ts, web/**/*.tsx, web/**/*.json, web/**/*.css"
description: "Frontend rules: React 19, TypeScript strict, Vite, TanStack, shadcn/ui, API client."
---

# Frontend rules

- Layout: `web/src/{app,shared,storefront,admin}`. `app` = bootstrapping/providers/router; `shared` = design tokens, ui primitives, api client, i18n, utils; `storefront` and `admin` = feature folders (`features/<feature>/{components,hooks,routes,api}`), each lazy-loaded as its own route tree. `storefront` must never import from `admin` and vice versa.
- TypeScript `strict`, `noUncheckedIndexedAccess`, no `any` (use `unknown` + narrowing). ESLint + Prettier + `tsc --noEmit` must pass.
- Server state: TanStack Query only. Client/UI state: local state or a tiny Zustand store (cart drawer, theme). Forms: React Hook Form + Zod; schemas validate user input only, backend remains the authority.
- API client generated from `docs/contracts/openapi.json` (openapi-typescript + openapi-fetch). Do not hand-write DTO types that exist in the contract. Handle ProblemDetails errors in one place.
- Auth: access token in memory only (never localStorage/sessionStorage); refresh via httpOnly cookie; silent refresh on 401 once, then redirect to login. Route guards use `permissions` from `GET /auth/me`; they are UX only — the server enforces access.
- Payment: the UI never decides a payment succeeded. It shows the QR, polls `GET /payments/{code}/status` (with backoff) and renders whatever the server says.
- Money formatting only through `formatVnd()` (Intl `vi-VN`, no decimals). Dates through one date util with `Asia/Ho_Chi_Minh`.
- shadcn/ui components are copied into `web/src/shared/ui` and then themed through tokens (see design system). Do not leave default shadcn look untouched.
- Performance: route-level code splitting, image `width/height` + `loading="lazy"`, no layout shift, avoid re-render storms in product grids (virtualize only if measured need). Lighthouse targets are in `docs/spec/12-TESTING-STRATEGY.md`.
- Accessibility: semantic HTML, labelled inputs, visible focus, keyboard-operable dialogs/menus, `prefers-reduced-motion` respected, status never conveyed by color alone.
- Never use emoji as icons or decoration. Icons: lucide-react (stroke 1.5) + a small custom SVG set under `web/src/shared/icons`.
