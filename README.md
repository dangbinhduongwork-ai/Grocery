# GroceryHub — Vietnamese Online Grocery Modular Monolith

[![CI Build & Test](https://github.com/mykey-repository/GroceryHub/actions/workflows/ci.yml/badge.svg)](https://github.com/mykey-repository/GroceryHub/actions/workflows/ci.yml)
[![.NET Version](https://img.shields.io/badge/.NET-10.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![React Version](https://img.shields.io/badge/React-19.0-blue.svg)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.7%20Strict-blue.svg)](https://www.typescriptlang.org/)
[![Database](https://img.shields.io/badge/SQL%20Server-2022%2B%20%7C%20Azure%20SQL-red.svg)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

GroceryHub is a production-grade online grocery e-commerce platform tailored for the Vietnamese market. It features a complete storefront and admin back-office, exact integer VND accounting, 2-level administrative division addresses (provinces and communes/wards), and automated VietQR payment processing via SePay with cryptographic HMAC-SHA256 webhooks and reconciliation.

---

## Key Highlights & Architecture

- **Backend Architecture:** Modular Monolith built on **C# .NET 10 LTS** and **ASP.NET Core Web API** (Minimal APIs + Feature Endpoints). Enforces strict module isolation across 12 domain modules.
- **Database Architecture:** Single **Microsoft SQL Server** database with **schema-per-module** (`identity`, `catalog`, `inventory`, `purchasing`, `cart`, `ordering`, `payments`, `promotions`, `customers`, `reviews`, `reporting`, `audit`). Configured with Read Committed Snapshot Isolation (RCSI) for non-blocking reads.
- **Frontend Architecture:** **React 19** + **TypeScript (strict)** + **Vite**, styled with **Tailwind CSS v4** and **shadcn/ui** using the custom *"Sổ chợ"* warm editorial Vietnamese design tokens. Fully responsive (360px to 1920px) and WCAG 2.1 AA accessible.
- **Payment Gateway:** Hybrid support for Cash on Delivery (COD) and automated **SePay VietQR** bank transfers. Inbound webhooks are authenticated via HMAC-SHA256 with constant-time equality comparisons, idempotency deduplication, and a four-eyes two-person refund approval workflow.
- **Monetary Precision:** Strictly 64-bit integer Vietnamese Dong (`long` / `bigint`). Zero floating-point drift.
- **Zero Commercial Package Dependency:** Strictly uses permissible open-source dependencies (e.g. `System.Threading.Channels` for outbox messaging, native endpoint routing, xUnit v2). Banned commercial packages (MediatR, AutoMapper, FluentAssertions 8+) are verified absent by automated architecture tests.

---

## System Overview & Module Map

```mermaid
graph TD
    Client[Browser / Mobile Client] -->|HTTPS :8080| Nginx[Unprivileged Nginx Gateway]
    Nginx -->|SPA Static Files| Web[React 19 Frontend]
    Nginx -->|Reverse Proxy /api/*| Api[ASP.NET Core 10 Web API Host]
    
    subgraph Modular Monolith [src/GroceryHub.Modules.*]
        Api --> Identity[Identity & RBAC Module]
        Api --> Catalog[Catalog Module]
        Api --> Inventory[Inventory Module]
        Api --> Purchasing[Purchasing Module]
        Api --> Cart[Cart Module]
        Api --> Ordering[Ordering Module]
        Api --> Payments[Payments Module - SePay VietQR]
        Api --> Promotions[Promotions Module]
        Api --> Customers[Customers Module - 2-Level VN]
        Api --> Reviews[Reviews Module]
        Api --> Reporting[Reporting & Margin Module]
        Api --> Audit[Audit Logging Module]
    end
    
    Modular Monolith -->|EF Core 10| SqlDb[(SQL Server 2022+ / Azure SQL)]
    SePay[SePay Webhook Ingress] -->|HMAC-SHA256 Signed| Nginx
```

---

## Project Structure

```
GroceryHub/
├── src/
│   ├── GroceryHub.Api/               # Web API host, middleware, OpenAPI, rate limiting
│   ├── GroceryHub.SharedKernel/      # Domain primitives (Money, Result, VietnamesePhone)
│   ├── GroceryHub.Persistence/       # EF Core DbContext, schema-per-module mappings, migrations
│   └── GroceryHub.Modules.*/         # 12 Independent feature domain modules
├── web/
│   ├── src/
│   │   ├── admin/                    # Admin/Staff/Manager dashboard and back-office UI
│   │   ├── storefront/               # Storefront customer pages (aisles, cart, checkout, tracking)
│   │   └── shared/                   # Design system tokens, Vietnamese i18n, components
│   ├── nginx/default.conf            # Hardened Nginx reverse proxy configuration
│   └── Dockerfile                    # Multi-stage unprivileged web container
├── tests/
│   ├── GroceryHub.UnitTests/         # 241 fast, zero-dependency unit tests
│   ├── GroceryHub.ArchitectureTests/ # 6 module boundary & forbidden package architecture tests
│   └── GroceryHub.IntegrationTests/  # 208 real SQL Server integration & E2E journey tests
├── scripts/                          # Backup, restore drills, reality audit, smoke tests
└── docs/                             # Full specification, operational runbooks, and handover guides
```

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22 LTS](https://nodejs.org/) & `npm`
- [Docker Desktop](https://www.docker.com/) or Docker Engine
- Microsoft SQL Server 2022 (or LocalDB on Windows)

### Option 1: Running with Docker Compose (Recommended)

1. Clone the repository and configure environment variables:
   ```bash
   git clone https://github.com/mykey-repository/GroceryHub.git
   cd GroceryHub
   cp .env.example .env
   ```
2. Start the complete production-like stack:
   ```bash
   docker compose up -d --build
   ```
3. Access the application:
   - **Storefront:** [http://localhost:8080](http://localhost:8080)
   - **Admin Back-Office:** [http://localhost:8080/admin](http://localhost:8080/admin)
   - **Health Checks:** [http://localhost:8080/health/ready](http://localhost:8080/health/ready)
   - **API OpenAPI Contract:** [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json)

### Option 2: Local Development Setup

#### Backend API (.NET 10)
```bash
# Apply EF Core migrations
dotnet run --project src/GroceryHub.Api -- --migrate

# Run API in Development mode (with Swagger UI)
dotnet run --project src/GroceryHub.Api
# API listens on https://localhost:5001 and http://localhost:5000
```

#### Frontend SPA (React 19 + Vite)
```bash
cd web
npm install
npm run dev
# Vite dev server runs at http://localhost:5173 with proxy to backend
```

---

## Testing & Quality Verification

GroceryHub is backed by a deterministic test suite consisting of **473 tests** with **0 failures and 0 skips**:

```bash
# 1. Run all .NET Backend Tests (Unit, Architecture, Integration)
dotnet test --configuration Release

# 2. Run all Frontend Web Tests (TypeScript, Vitest, Lint)
cd web
npm test
npm run build

# 3. Execute Reality Audit (zero TODOs, zero NotImplementedException, zero banned packages)
pwsh ./scripts/verify-reality.ps1
```

---

## Documentation Index

Comprehensive engineering and operational documentation is maintained in the `docs/` directory:

- **System Specifications:**
  - [`docs/spec/02-PROJECT-REQUIREMENTS.md`](file:///e:/25.Mykey/GroceryHub/docs/spec/02-PROJECT-REQUIREMENTS.md) — Functional (FR), Non-Functional (NFR), and Business Rules (BR).
  - [`docs/spec/05-SYSTEM-ARCHITECTURE.md`](file:///e:/25.Mykey/GroceryHub/docs/spec/05-SYSTEM-ARCHITECTURE.md) — System Architecture & Modular Monolith rules.
  - [`docs/spec/08-UI-DESIGN-SYSTEM.md`](file:///e:/25.Mykey/GroceryHub/docs/spec/08-UI-DESIGN-SYSTEM.md) — "Sổ chợ" Design System & Token specifications.
  - [`docs/spec/11-SEPAY-INTEGRATION.md`](file:///e:/25.Mykey/GroceryHub/docs/spec/11-SEPAY-INTEGRATION.md) — SePay VietQR integration architecture.
- **Operations & Handover:**
  - [`docs/ops/handover.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/handover.md) — **Production Handover & Operational Manual**.
  - [`docs/ops/deployment-guide.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/deployment-guide.md) — Production Deployment Guide (Cloud Container & VPS).
  - [`docs/ops/runbook.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/runbook.md) — Day-to-day operations and alert response.
  - [`docs/ops/backup-restore.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/backup-restore.md) — Backup schedules and Disaster Recovery runbook.
  - [`docs/ops/incident-checklist.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/incident-checklist.md) — Emergency incident checklists.
- **Audits & Final Evidence:**
  - [`docs/progress/FINAL-REPORT.md`](file:///e:/25.Mykey/GroceryHub/docs/progress/FINAL-REPORT.md) — Master Final Project Report & Go-Live sign-off.
  - [`docs/progress/TRACEABILITY.md`](file:///e:/25.Mykey/GroceryHub/docs/progress/TRACEABILITY.md) — Complete Requirements Traceability Matrix.
  - [`docs/security/review-final.md`](file:///e:/25.Mykey/GroceryHub/docs/security/review-final.md) — Final Security Audit & RBAC verification.
  - [`docs/evidence/phase-13/`](file:///e:/25.Mykey/GroceryHub/docs/evidence/phase-13/) — Phase 13 independent verification evidence files.

---

## Default Production Roles & Seeded Accounts

For development and staging environments, the following baseline accounts are seeded:

| Role | Email | Default Password | Permissions Scope |
|---|---|---|---|
| **Admin** | `admin@groceryhub.vn` | `Admin@123456` | Full superuser access, system settings, RBAC role editor |
| **Manager** | `manager@groceryhub.vn` | `Manager@123456` | Inventory management, purchasing, reviews, reports, refund approval |
| **Staff** | `staff@groceryhub.vn` | `Staff@123456` | Order picking, order packing, shipping status, refund request |
| **Customer** | `customer@groceryhub.vn` | `Customer@123456` | Storefront browsing, cart management, ordering, review posting |

> **IMPORTANT:** In production deployments, change all default passwords immediately and set `Database__SeedSampleData=false` in your `.env` configuration.

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
All external packages and third-party libraries comply with non-commercial open-source licenses (MIT, Apache-2.0, BSD-3-Clause).
