---
name: ui-ux-design
description: Applies the GroceryHub design system to create or review storefront and admin screens - tokens, Vietnamese typography, market-tag prices, aisle navigation, page recipes, states, responsive layouts, accessibility and anti-"AI-look" checks. Use for any visual/layout/UX decision, new page design, component styling or UI review.
---

# ui-ux-design

## Goal
Screens that look deliberately designed for a Vietnamese grocery brand - not a generic template - and that are usable, accessible and consistent. Source of truth: `docs/spec/08-UI-DESIGN-SYSTEM.md`.

## Process
1. Identify the page type; open its recipe in spec 08 (home, aisle/category, search, product, cart, checkout, payment, order tracking, account; admin: dashboard, queue, detail, form, table).
2. List the user's primary task and the one primary action; remove anything that does not serve it.
3. Compose with tokens only (no raw hex), using the recipe's layout pattern. Define all states: loading (content-shaped skeleton), empty, error, partial, offline.
4. Check at 360/768/1280/1920 px. Verify contrast with a real tool and the Vietnamese test string with every font weight used.
5. Run `/wf-ui-review`; attach real screenshots.

## Standards (summary)
- Palette: paper `#FAF7F0`, ink green `#1F3A2D`, tomato `#D9432F` (price/discount only), turmeric `#E3A21A` (highlight/badges), warm neutrals. Admin uses a cooler neutral surface with the same green accent.
- Type: Be Vietnam Pro (UI/body) + a display serif with verified Vietnamese support (Fraunces candidate - verify glyph coverage) for editorial headlines; tabular numerals for prices.
- Signature elements: market-tag price component, aisle list navigation with counts, receipt-style cart summary, hairline rules instead of boxes.
- Density: storefront breathable; admin dense and keyboard-first.

## Anti-"AI-look" checklist
- [ ] No purple/blue-violet gradients, no glass blur, no blob backgrounds
- [ ] Not every section is a rounded card; list/table/rule layouts used where appropriate
- [ ] No emoji in UI; icons are consistent line icons
- [ ] Pages differ by recipe; hero is not a centered headline + two buttons over a gradient
- [ ] Real product photography ratios (4:5), consistent background treatment
- [ ] Copy is specific, Vietnamese, free of filler ("Chào mừng đến với trải nghiệm tuyệt vời...")
- [ ] Motion purposeful, <= 200 ms, reduced-motion respected

## Common mistakes
- Using default shadcn zinc palette and radius everywhere; inconsistent spacing scale.
- Price hierarchy unclear (discount vs original vs unit price per kg).
- Cart/checkout forms with too many fields; address entry without province/ward selectors.
- Tiny touch targets; sticky bars covering content on mobile.

## Official documentation
- https://www.w3.org/TR/WCAG22/
- https://ui.shadcn.com/docs/theming
- https://tailwindcss.com/docs/theme
- https://fonts.google.com/specimen/Be+Vietnam+Pro
