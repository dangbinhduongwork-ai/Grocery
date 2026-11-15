---
name: docker-devops
description: Docker, Docker Compose, reverse proxy, SQL Server container, migrations bundle, environment/secrets handling, GitHub Actions CI, backups and deployment runbooks for GroceryHub. Use for Dockerfiles, compose files, CI/CD workflows, deployment, environment configuration and operational scripts.
---

# docker-devops

## Goal
One-command local stack, reproducible CI, safe production deployment for a small team.

## Process
1. Dockerfiles: multi-stage; API `mcr.microsoft.com/dotnet/sdk:10.0` -> `aspnet:10.0`; web `node` build -> nginx (unprivileged). Non-root user, healthcheck, `.dockerignore`.
2. Compose: services `sqlserver`, `migrator` (one-off), `api`, `web` (nginx: static + `/api` proxy, security headers), dev-only `mailpit`. Named volumes for SQL data and product images. Internal network; only `web` publishes a port. `depends_on` with `condition: service_healthy`.
3. Config: `.env.example` lists every variable with placeholder/description; real `.env` git-ignored. Secrets via env or Docker secrets; no defaults for passwords/keys in compose.
4. SQL Server: pin an image tag verified on Microsoft Container Registry (e.g. `mcr.microsoft.com/mssql/server:<verified-tag>`), `ACCEPT_EULA=Y`, set `MSSQL_PID` deliberately (Developer is for non-production only; Express for small production; otherwise licensed edition or managed service). Memory limit set. Not exposed publicly in production.
5. Migrations: `dotnet ef migrations bundle` executed by the `migrator` service before `api` starts; the API never migrates on startup in Production.
6. CI (GitHub Actions): jobs for backend (restore, build -warnaserror, test with Testcontainers, vulnerable packages), frontend (install --frozen-lockfile, lint, typecheck, test, build, audit), secrets scan, docker build + image scan. Cache NuGet/npm. Required status checks on `main`.
7. Backups: scheduled `BACKUP DATABASE` to a volume + off-host copy; document and rehearse restore; record RPO/RTO targets.
8. Webhook path hardening at the proxy: IP allowlist from SePay's published IP list (re-verify), rate limits, body size limits.

## Best practices
- Pin versions/digests; update via Dependabot PRs with CI.
- Healthchecks: `/health/ready` checks DB connectivity.
- Log to stdout in JSON; rotate at the Docker level.
- Read-only root filesystem where possible; `no-new-privileges`.

## Common mistakes
- `latest` tags; secrets baked into images; running as root.
- Running EF migrations on app start in Production.
- Exposing port 1433 publicly; weak `sa` password defaults.
- Missing volume for SQL data (data loss on `down -v`).
- CORS/cookie issues from serving SPA and API on different origins (prefer same-origin proxy).

## Checklist
- [ ] `docker compose up -d --build` from clean clone is healthy
- [ ] Smoke test script passes (health, register/login, product list)
- [ ] CI green on PR; failing test makes CI red (verified once)
- [ ] Restore drill executed with output saved
- [ ] Image scan results reviewed

## Official documentation
- https://docs.docker.com/compose/
- https://docs.docker.com/build/building/best-practices/
- https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-deployment
- https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#bundles
- https://docs.github.com/actions
