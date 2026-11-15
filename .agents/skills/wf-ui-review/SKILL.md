---
name: wf-ui-review
description: Reusable procedure (invoke with /wf-ui-review) to review implemented GroceryHub screens against the design system and usability/accessibility standards, with real screenshots at four widths and an anti-generic-AI-look checklist. Use before declaring any storefront or admin page done.
---

# /wf-ui-review

Inputs: route(s) to review, page recipe reference (spec 08), running stack with realistic data.

1. Start the stack; load the page with real API data (not mocks).
2. Capture screenshots at **360, 768, 1280, 1920 px** for: default, loading, empty, error states; save to `docs/evidence/ui/<page>/`. If a screenshot tool is unavailable, say so explicitly - do not describe unseen pixels.
3. **Design-system conformance**: tokens only (search CSS/TSX for raw hex), fonts self-hosted with Vietnamese glyphs (render test string), spacing scale, price component usage, icon style consistency, no emoji.
4. **Anti-AI-look checklist** (from `ui-ux-design`): no purple/blue gradients or glass; not card soup; page follows its own recipe; copy specific; motion restrained.
5. **Usability**: one primary action per view, mobile reach, touch targets >= 44 px, form flow (address, checkout) minimal, error messages actionable.
6. **Accessibility**: keyboard path through the page, visible focus, labels, landmarks, contrast (run a real checker), axe run saved, reduced motion tested.
7. **Performance sanity**: Lighthouse (mobile) on key pages; note LCP/CLS; compare to budgets in spec 12.
8. Report `docs/evidence/ui/review-<page>.md`: PASS / FIX with findings as `file:line - problem - expected`. Send CR notes to frontend-developer or ui-ux-designer according to ownership.
