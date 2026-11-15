# Phase 12 — Docker and Deployment

**Goal:** Reproducible production-like deployment with Docker Compose, CI/CD, backups, runbooks and a smoke test, ready for the user's chosen host.
**Agents:** devops-engineer (lead), security-reviewer, qa-engineer (smoke), database-engineer (migrations/backup), payment-specialist (webhook path), PM.
**Inputs:** NFR-004 (restart policy, health, graceful shutdown, restore drill); doc 13, DECISIONS (hosting target, SQL Server edition), SePay doc 11.
**Outputs:** Dockerfiles, compose files, nginx config, CI/CD workflows, scripts (smoke, backup, restore-drill), `docs/ops/*` (deployment guide, runbooks), image scan reports.
**Dependencies:** Phase 11 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 12 — DOCKER & DEPLOYMENT. Load skills docker-devops, security-review. Follow docs/spec/13-DEPLOYMENT-STRATEGY.md. Confirm with the user (Vietnamese): hosting target (VPS/cloud), domain, SQL Server edition for production (Developer is not licensed for production), SMTP provider. Record in DECISIONS.md. Never put real secrets in files that are committed.

1. devops-engineer: finalize Dockerfiles (api, web) multi-stage, non-root, healthchecks, pinned tags; nginx config (SPA fallback, /api proxy, headers, CSP incl. img-src qr.sepay.vn, body limits, webhook rate limit + IP allowlist from SePay's official IP page), docker-compose.yml (internal network, only web exposed, volumes sql-data/product-images, migrator service with service_completed_successfully), compose override for dev, .env.example complete, secrets via env/Docker secrets; ci.yml hardened + release.yml (GHCR images by tag), Dependabot config coordination with security-reviewer; scripts: smoke-test, backup-db, restore-drill, rotate-secrets notes.
2. database-engineer: backup/restore scripts using BACKUP/RESTORE inside the container, least-privilege app login, READ_COMMITTED_SNAPSHOT verification.
3. From a CLEAN clone: cp .env.example .env (fill dev values), docker compose up -d --build; wait healthy; run smoke test; run the Playwright suite against the stack; run restore drill; capture all output as evidence.
4. security-reviewer: container/compose hardening review (non-root, capabilities, exposed ports, secrets, image scan with Trivy or equivalent), headers/CSP verification with curl -I, webhook path checks.
5. qa-engineer: smoke + E2E on the compose stack; test that SQL Server port is NOT reachable from the host in the production compose.
6. devops-engineer writes docs/ops/{deployment-guide,runbook,backup-restore,incident-checklist}.md (the deployment guide has numbered steps in Vietnamese and English for the user's chosen host); payment-specialist finalizes docs/ops/payments-runbook.md. You: /wf-progress-review, phase-12-report.md.
```

## Acceptance criteria
- Clean clone → `docker compose up -d --build` → all services healthy → smoke test passes (evidence).
- Playwright suite passes against the compose stack.
- Production compose exposes only the web port(s); SQL Server not reachable externally (test).
- Containers run as non-root; image tags pinned; no secrets in images or repo (gitleaks clean).
- Backup + restore drill output saved; RPO/RTO stated.
- CI green on a PR (or the user is told exactly how to run it after pushing to GitHub; local simulation evidence provided) and fails on a deliberately broken test.
- Image scan: no unaddressed Critical findings.
- Security sign-off recorded.

## How to verify
Do the clean clone yourself on a different folder; run `docker ps`, `docker compose config`, `curl -I` on the site; stop and start the stack; try connecting to port 1433 from the host.

## Gate to Phase 13
All PASS; user confirms hosting/edition decisions; PM GO.
