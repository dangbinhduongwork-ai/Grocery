---
name: react-frontend
description: Implements and reviews the GroceryHub React 19 + TypeScript + Vite frontend - TanStack Router/Query, generated OpenAPI client, React Hook Form + Zod, Tailwind v4 + shadcn/ui, auth/session handling, cart/checkout/payment pages, admin tables, accessibility and performance. Use for any web/ code, component, hook, route, form or frontend build/test task.
---

# react-frontend

## Goal
Type-safe, accessible, fast UI that consumes the real API and never fakes server behavior.

## Process
1. Read the requirement IDs, the page recipe in `docs/design/` (or spec 08), and the endpoint rows in spec 07.
2. Regenerate API types from `docs/contracts/openapi.json` (`npm run api:generate`); never hand-write DTOs.
3. Build route -> feature hooks (TanStack Query) -> components -> forms (RHF + Zod). Implement loading/empty/error states first, then the happy path.
4. Add tests (Vitest + Testing Library) for hooks/logic and forms; E2E belongs to QA.
5. Run `npm run lint`, `npm run typecheck`, `npm test`, `npm run build`; save output. Take screenshots at 360/768/1280/1920 px against a running backend.

## Standards
- Structure: `app/`, `shared/`, `storefront/`, `admin/` per `.agents/rules/frontend.md`; admin lazy-loaded; no cross imports between storefront and admin.
- URL is state for filters/sort/page (TanStack Router search params with Zod validation).
- Query keys centralised per feature; mutations invalidate precisely; optimistic updates only for cart quantity with rollback.
- Auth: access token in memory; one refresh-on-401 interceptor; route guards from `/auth/me` permissions (UX only).
- Forms: accessible labels, inline errors in Vietnamese, server ProblemDetails mapped to field errors.
- Payment page: show server QR payload; poll status with backoff (2s -> 5s -> 10s) up to the expiry time; stop on terminal status; never mark paid client-side.
- Formatting: `formatVnd`, Vietnamese dates, pluralisation-free copy.
- Performance: route splitting, responsive images (`srcset`), `loading="lazy"`, skeletons the same size as content, no CLS; budgets in spec 12.
- A11y: keyboard, focus management in dialogs/drawers, `aria-live` for cart/toast, reduced motion.

## Best practices
- Co-locate feature code; extract to `shared` only after 3 uses.
- Keep components small and presentational; side effects in hooks.
- Prefer composition over config-heavy components; use shadcn primitives through the themed wrappers.

## Common mistakes
- Storing JWT in localStorage; trusting client-side role checks.
- Duplicating server types by hand; `any`; casting API data.
- Re-fetch loops from unstable query keys; using `useEffect` for derived state.
- Layout shift from images without dimensions; huge admin bundle on the storefront.
- Default shadcn look left untouched; emoji as icons.

## Checklist
- [ ] Types generated, no `any`
- [ ] Loading/empty/error states present
- [ ] Keyboard + screen-reader pass on the page (axe output saved)
- [ ] Screenshots at 4 widths
- [ ] Lint/typecheck/test/build evidence saved

## Official documentation
- https://react.dev/reference/react
- https://vite.dev/guide/
- https://tanstack.com/router/latest/docs
- https://tanstack.com/query/latest/docs
- https://tailwindcss.com/docs
- https://ui.shadcn.com/docs
- https://react-hook-form.com/docs
- https://zod.dev
