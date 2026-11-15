# 13 — Deployment Strategy (Docker + Compose)

## 1. Topology
```
Internet ─HTTPS─> [reverse proxy / TLS]  ─> web (nginx: SPA + /api proxy) ─> api (ASP.NET Core) ─> sqlserver
                                                                              └─> volumes: images
migrator (one-off)  ─> sqlserver          mailpit (dev only)
```
Production options (decide in Phase 12 with the user): (A) single VPS with Docker Compose + Caddy/Traefik/nginx for TLS (simplest); (B) managed container platform + managed SQL. The pack implements (A) and documents (B).

## 2. Images
- `api`: multi-stage; SDK `mcr.microsoft.com/dotnet/sdk:10.0` → runtime `mcr.microsoft.com/dotnet/aspnet:10.0`; non-root; `ASPNETCORE_URLS=http://+:8080`; healthcheck `/health/live`; read-only root FS with tmpfs for temp where possible.
- `web`: build with Node LTS → serve with `nginxinc/nginx-unprivileged`; config: SPA fallback, `/api/` proxy to `api:8080`, gzip/brotli, immutable caching for hashed assets, `index.html` no-cache, security headers (HSTS at TLS layer, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`), CSP (`default-src 'self'; img-src 'self' data: https://qr.sepay.vn; style-src 'self' 'unsafe-inline'` only if unavoidable — prefer nonces/hashes), body size limits (uploads 5 MB; webhook 64 KB).
- `sqlserver`: `mcr.microsoft.com/mssql/server:<verified tag>` (pin; verify on MCR; do not use `latest`), `ACCEPT_EULA=Y`, `MSSQL_PID` chosen deliberately: **Developer is not licensed for production**; Express (free, 10 GB limit) or a paid edition/managed service for production. Resource limits set; not published to host in production.
- `migrator`: EF Core migration bundle run to completion before `api` starts (`depends_on: condition: service_completed_successfully`).
- `mailpit` (dev only): SMTP catcher + API used by E2E.

## 3. Compose files
- `docker-compose.yml`: production-like base (internal network `gh-net`; only `web` publishes 80/443 or 8080 behind the proxy; named volumes `sql-data`, `product-images`; `restart: unless-stopped`; healthchecks; `depends_on` with health conditions).
- `docker-compose.override.yml`: dev (ports for api/sql/mailpit, hot reload not required, dev env vars).
- `.env.example` (committed) vs `.env` (ignored). Secrets: env vars or Docker/Compose secrets files (`/run/secrets/*`); never baked into images.

## 4. Environment variables (minimum; devops finalizes)
`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__Default` (or `MSSQL_SA_PASSWORD` + app login), `Jwt__SigningKey` (≥ 32 random bytes), `Jwt__Issuer/Audience`, `App__PublicBaseUrl`, `Smtp__Host/Port/User/Password/From`, `SePay__*` (see doc 11), `Seed__AdminEmail`, `Seed__AdminPassword` (first-run only; force change), `Storage__ImagesPath`, `Cors__AllowedOrigins` (dev only), `Serilog__MinimumLevel`.

## 5. Database operations
- Create an application SQL login with least privilege (not `sa`).
- Migrations via bundle only; backward-compatible (expand/contract). Before each release: backup.
- Backups: nightly full + (optional) log backups via `BACKUP DATABASE` to a volume, copied off-host; retention 14 days; **restore drill** scripted (`scripts/restore-drill.*`) and executed in Phase 12 with saved output; document RPO/RTO.
- `READ_COMMITTED_SNAPSHOT ON` applied by a migration script.

## 6. CI/CD (GitHub Actions)
- `ci.yml` on PR/push: backend (restore, build, unit+arch+integration tests with Testcontainers, vulnerable packages), frontend (npm ci, lint, typecheck, unit tests, build, audit), `verify-reality`, gitleaks, docker build (no push on PR), image scan.
- `release.yml` on tag `v*`: build and push images to GHCR with version + digest, generate SBOM (optional), create release notes.
- Dependabot for NuGet, npm, GitHub Actions, Docker. Branch protection: required checks, PR review, no direct pushes to `main`.

## 7. Release and rollback
Release: tag → images → on server `docker compose pull && docker compose up -d` after backup + migration bundle. Rollback: redeploy previous image tag; if a migration was applied, rely on expand/contract compatibility, else restore backup (documented decision tree).

## 8. Monitoring and operations
`/health/live`, `/health/ready`; structured JSON logs (`docker logs`), optional Loki/Grafana later; alerts (basic): healthcheck failing, payments review queue > N, reconciliation job failed, disk usage. Log rotation configured on the Docker daemon.

## 9. Hardening checklist
TLS everywhere with HSTS; SQL Server not public; non-root containers; `no-new-privileges`; resource limits; secrets rotation procedure; SePay webhook path IP allowlist + rate limit; firewall (80/443 only); automatic security updates for the host; regular dependency updates; admin area optionally restricted by IP/VPN.

## 10. Smoke test (scripts/smoke-test.*; run after every deploy)
`GET /health/ready` → healthy; `GET /api/v1/categories` → 200; register + login test user (disposable) → 200; `GET /api/v1/products?pageSize=1` → 200; static `index.html` served; webhook endpoint rejects unsigned request with 401.
