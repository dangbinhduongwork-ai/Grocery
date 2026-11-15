# GroceryHub — Open Questions & Escalations

All unresolved requirement questions, external dependency questions, and technical blockers are tracked here. Every `TODO(Q-xxx)` in source code must reference an active ID in this file.

## Active Questions (To be resolved with the user in Phase 01)

| ID | Phase | Raised By | Question | Suggested Default | Status |
|---|---|---|---|---|---|
| Q-001 | 01 | `project-manager` | Receiving bank account for SePay (bank short name, account number, account holder name) and whether the account supports SePay webhooks with payment-code (`GH` + 10 digits) detection? | Use configurable placeholder (`MBBank` / `0000000000` / `CONG TY TNHH GROCERYHUB`) in dev/test with SePay webhook simulator; switch via `.env` for production. | Open |
| Q-002 | 01 | `project-manager` | Delivery area (provinces served), initial shipping rates per zone, free-shipping threshold, and COD maximum order value? | Serve TP. Hồ Chí Minh (25.000 ₫) & Bình Dương / Đồng Nai (35.000 ₫) initially; free-shipping threshold `300.000 ₫`; COD max `2.000.000 ₫` (all editable in Admin Settings). | Open |
| Q-003 | 01 | `project-manager` | Product catalog source (manual entry in Admin vs CSV import) and initial categories? | Seed 8 standard Vietnamese grocery aisles (Rau củ quả, Thịt cá trứng, Sữa & chế phẩm, Đồ khô & gia vị, Đồ uống, Bánh kẹo & ăn vặt, Chăm sóc cá nhân, Vệ sinh nhà cửa) + Admin manual CRUD (~200 dev sample items). | Open |
| Q-004 | 01 | `project-manager` | Brand assets (logo, brand name usage, custom colors) if any? | Use the **"Sổ chợ"** design system in `docs/spec/08-UI-DESIGN-SYSTEM.md` (`#FAF7F0` paper, `#1F3A2D` leaf green, `#C84B31` market tag red, `Be Vietnam Pro` + `Fraunces`). | Open |
| Q-005 | 01 | `project-manager` | Production hosting target (single VPS with Docker Compose vs managed cloud) and SQL Server edition for production (`Express` free 10 GB vs licensed `Standard`/`Web`)? | Single Linux VPS with Docker Compose + reverse proxy TLS (Option A in `docs/spec/13-DEPLOYMENT-STRATEGY.md`) and SQL Server Express for initial production launch (`Developer` only in local dev/test). | Open |
