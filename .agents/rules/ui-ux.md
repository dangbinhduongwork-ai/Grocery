---
trigger: model_decision
description: "Apply when designing or reviewing any UI: pages, components, layout, typography, color, motion, copy for GroceryHub storefront or admin."
---

# UI/UX rules (see docs/spec/08-UI-DESIGN-SYSTEM.md for tokens and page recipes)

1. Use the GroceryHub tokens (paper background, ink green, tomato price accent, turmeric highlight). No purple/blue-violet gradients, no glassmorphism, no generic gradient hero blobs, no default shadcn zinc look.
2. Do not put everything in cards. Use cards only for objects that are selectable/actionable units (a product in a browse grid, a saved address). Use lists, tables, rules (hairlines), and whitespace for everything else.
3. Do not make pages identical. Each page type follows its own recipe in the design doc (home, category aisle, search, product, cart, checkout, order tracking, account; admin: dashboard, queue, detail, form, table).
4. No emoji in UI. No stock "AI" illustrations. Product imagery is real photography on a consistent neutral ground; empty states use a typographic or line-icon treatment.
5. Vietnamese-first typography: verified Vietnamese diacritics rendering for every font; test string `Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ`. Prices use tabular numerals and the market-tag component.
6. Copy: short, concrete, Vietnamese, no marketing filler. Error messages say what happened and what to do.
7. Motion: 120–200 ms ease-out for state changes, one meaningful entrance per page at most, respect reduced motion.
8. Mobile first: thumb-reachable primary actions, sticky add-to-cart/checkout bars, 44px min touch targets, bottom sheets for cart/filters.
9. Each screen needs all states designed and implemented: loading (skeleton shaped like content), empty, error, partial, offline.
10. Review every screen at 360, 768, 1280 and 1920 px widths, light theme (storefront) and the admin theme, and take real screenshots as evidence.
