# 03 — Tech Stack Research (verified Nov 2026)

Sources are official docs unless noted. Agents must re-verify versions with `dotnet --list-sdks`, `npm view <pkg> version` before pinning.

## 1. Decisions
| Area | Decision | Why | Evidence / source |
|---|---|---|---|
| Runtime | .NET 10 (LTS), ASP.NET Core 10, EF Core 10, C# 14 | .NET 10 is LTS supported until 14 Nov 2028; .NET 8/9 reach end of support 10 Nov 2026 | Microsoft .NET Support Policy (dotnet.microsoft.com/platform/support/policy) |
| Architecture | Modular monolith, single deployable, schema-per-module, single DbContext | Solo/small team; transactional consistency for stock/orders/payments; easy Docker deploy; modules keep a path to extraction | Design decision (doc 05) |
| API style | REST + OpenAPI 3.1 (built-in `Microsoft.AspNetCore.OpenApi`), minimal APIs with endpoint classes | Contract-first TS client generation; less ceremony than controllers | ASP.NET Core 10 docs |
| Auth | ASP.NET Core Identity (user store, hashing, lockout, tokens) + JwtBearer access token (15 min) + rotating refresh token (httpOnly cookie) | Avoid home-made crypto; permission-based RBAC on top | Microsoft Learn |
| DB | SQL Server + EF Core 10 migrations | Required by user | Microsoft Learn |
| Validation | FluentValidation (Apache-2.0) | Mature; license permissive | Verify before adding |
| Logging | Serilog (Apache-2.0) + console JSON | Standard; container-friendly | |
| Tests (BE) | xUnit, Shouldly (or built-in asserts), Testcontainers for .NET, NetArchTest or ArchUnitNET (license check) | Avoid FluentAssertions 8+ (commercial) | Licence notes below |
| Frontend | React 19 + TypeScript strict + Vite | Required by user | react.dev, vite.dev |
| Routing/data | TanStack Router + TanStack Query + TanStack Table | Type-safe URL search params (filters), cache management, admin tables; matches `satnaing/shadcn-admin` stack so its shell can be reused | tanstack.com |
| UI | Tailwind CSS v4 + shadcn/ui (copied components) | Required by user; full control of look | ui.shadcn.com, tailwindcss.com |
| Forms | React Hook Form + Zod | Type-safe validation, small | |
| API client | `openapi-typescript` + `openapi-fetch` generated from `docs/contracts/openapi.json` | No duplicate DTOs | |
| Icons | lucide-react (+ small custom SVG set for grocery-specific glyphs) | Consistent line icons, ISC licence | |
| Animation | CSS transitions first; `motion` library only for 2–3 purposeful interactions (cart drawer, add-to-cart feedback) | Avoid decorative motion | |
| Fonts | Be Vietnam Pro (UI/body; OFL; designed for Vietnamese) + display serif candidate Fraunces (verify Vietnamese subset); self-host via Fontsource | Vietnamese diacritics correctness, no CDN dependency | fonts.google.com |
| Payments | SePay VietQR + webhook (HMAC-SHA256) | Required by user | developer.sepay.vn |
| Infra | Docker + Compose; nginx reverse proxy; GitHub Actions | Required by user | docs.docker.com |
| Mail | SMTP via provider; Mailpit in dev | Email verification/reset | |
| Images | Local volume behind `IFileStorage` abstraction; server-side re-encode to WebP/AVIF using a permissively licensed library (Magick.NET Apache-2.0 or SkiaSharp MIT; verify) | Avoid licence surprises (e.g. ImageSharp's split licence) | Verify before adding |

## 2. Licence watch-list (verify at install time)
Several popular .NET packages moved to commercial/dual licences in 2025: **MediatR**, **AutoMapper**, **FluentAssertions (v8+)**, **MassTransit (v9+)**. The pack forbids them; use plain handlers, manual mapping (or a verified MIT source-generator mapper), and Shouldly. Agents must record each dependency's licence in `docs/progress/DEPENDENCIES.md`.

## 3. Antigravity compatibility notes
See `09-AGENT-ORCHESTRATION.md` and `10-SKILLS-AND-RULES.md` for the verified mechanisms (rules, skills, custom subagents, /teamwork, /swarm, deprecated workflows).

## 4. Vietnamese-market technical facts
- Administrative model: since 01/07/2025 Vietnam has **34 province-level units and ~3,321 commune-level units**; the district level is abolished. Address forms must be Province/City → Ward/Commune → street. Candidate dataset: `ThangLeQuoc/vietnamese-provinces-database` (SQL Server scripts and JSON; verify licence and data version before vendoring). Alternative: the official General Statistics Office catalog.
- Currency: VND has no minor unit; store integers. SePay amounts are integer VND.
- Phone: `^(0|\+84)(3|5|7|8|9)\d{8}$` is a reasonable mobile pattern (verify with tests; landlines optional).

## 5. Risks and mitigations
| Risk | Mitigation |
|---|---|
| Antigravity features differ between App, CLI and IDE | Dual-mode orchestration (subagent mode + role-adoption mode); Phase 02 verifies real behavior |
| SePay docs change | Phase 10 starts by re-reading the official pages; differences go to QUESTIONS.md |
| Reference repos are Next.js-based | Use only as UX/structure references; admin shell from a Vite-native repo |
| Agents fabricate completion | Evidence rules, `verify-reality`, independent reviewers, clean-checkout gate runs |
| SQL Server licence | Developer edition for dev only; decide production edition in Phase 12 |
