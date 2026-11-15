# GroceryHub — Incident Response & Outage Checklist

**Document ID:** `OPS-INC-001`  
**Version:** 1.0.0  
**Phase:** 12 — Docker & Deployment  
**Target Roles:** On-Call Engineer, Technical Lead, Incident Commander  
**Domain:** `grocery.dangbinhduong.io.vn`

---

## 1. Incident Severity Definitions & SLAs

| Severity | Definition | Examples | Response SLA | Target Resolution |
|---|---|---|---|---|
| **P1 — Critical Outage** | Complete storefront outage, inability to place orders, SePay webhook rejection, database crash. | 500 errors on checkout, `/health/ready` failing, payment QR not generating. | **$\le 15$ Minutes** | **$\le 1$ Hour** |
| **P2 — Degraded Service** | Partial impairment of business workflows, high latency (> 2s), delayed confirmation emails. | Search latency degraded, slow report generation, admin queue delay. | **$\le 1$ Hour** | **$\le 4$ Hours** |
| **P3 — Low Severity** | Minor non-blocking issue, visual UI inconsistency, isolated edge case. | Typo in email template, non-critical admin filter glitch. | $\le 1$ Business Day | Next scheduled release |

---

## 2. Immediate P1 Outage Triage Checklist

```
[ ] Step 1: ACKNOWLEDGE & ISOLATE
    - Announce incident in engineering communication channel (#incidents).
    - Designate Incident Commander (IC).
    - Inspect health endpoint: curl -i https://grocery.dangbinhduong.io.vn/health/ready

[ ] Step 2: IDENTIFY FAILURE DOMAIN
    - Web Gateway (Nginx): Is port 8080 responding?
    - Backend API: Are .NET containers crashing or restarting (OOM / Unhandled Exception)?
    - Database (SQL Server): Is connection pool exhausted? CPU / Disk full?
    - Third-Party (SePay / SMTP): Is SePay webhook returning 401/500?

[ ] Step 3: CAPTURE FORENSIC LOGS
    - Capture snapshot of recent errors before restarting:
      docker compose logs api --tail=500 > /tmp/incident_api_crash.log
      docker compose logs web --tail=500 > /tmp/incident_web_crash.log

[ ] Step 4: APPLY MITIGATION OR ROLLBACK (See Decision Tree below)
    - If bad deployment: Execute container tag rollback.
    - If database deadlocked: Kill blocking sessions or restart SQL Server.
    - If out of memory: Increase container memory limit and restart.

[ ] Step 5: VERIFY RECOVERY WITH SMOKE TEST
    - Run: bash scripts/smoke-test.sh https://grocery.dangbinhduong.io.vn
    - Confirm all 7 steps PASS.

[ ] Step 6: CLOSE INCIDENT & SCHEDULE POSTMORTEM
    - Post resolution notice to stakeholders.
    - Schedule postmortem within 48 hours.
```

---

## 3. Rollback Decision Tree

```
                                  [ Incident Detected ]
                                            │
                             Did incident begin immediately
                             after a new release deployment?
                                     ┌──────┴──────┐
                                    YES            NO
                                     │              │
                       Did the release include      Investigate System /
                       a DB migration script?       Resource Issues:
                        ┌────────────┴────────────┐  - OOM / CPU spike
                       YES                        NO  - DB connection pool
                        │                          │  - SePay API disruption
             Is the migration backwards-           │
             compatible (expand/contract)?         │
                 ┌──────┴──────┐                   │
                YES            NO                  │
                 │              │                  │
        [ Redeploy Previous ]  [ Emergency DB ]    │
        [   Container Tag   ]  [ Restore from ]    │
                 │             [ Pre-deploy   ]    │
                 │             [    Backup    ]    │
                 │                     │           │
                 └──────────────┬──────┴───────────┘
                                │
                      [ Run Smoke Test ]
                                │
                      [ Service Restored ]
```

### Rollback Commands:
```bash
# 1. Rollback API & Web images to previous release tag (e.g. v1.0.0)
sed -i 's/v1.0.1/v1.0.0/g' .env
docker compose -f docker-compose.yml up -d

# 2. Verify rollback health
curl -f https://grocery.dangbinhduong.io.vn/health/live
```

---

## 4. Postmortem Template (Blameless)

```markdown
# Incident Postmortem: [INCIDENT-TITLE]

**Date:** YYYY-MM-DD  
**Incident Commander:** [Name]  
**Severity:** P1 / P2  
**Total Downtime:** [X] Minutes  
**Impact:** [Number of affected users, dropped orders, or failed payments]

## 1. Timeline (UTC+7)
- **HH:MM** - Anomaly detected / alert fired.
- **HH:MM** - Incident acknowledged by on-call engineer.
- **HH:MM** - Root cause identified.
- **HH:MM** - Mitigation applied.
- **HH:MM** - Smoke test passed, incident resolved.

## 2. Root Cause
[Detailed technical description of what failed and why.]

## 3. Resolution & Mitigation
[What immediate actions resolved the outage?]

## 4. Preventative Action Items
- [ ] Action item 1 (Owner: [Name], Due: YYYY-MM-DD)
- [ ] Action item 2 (Owner: [Name], Due: YYYY-MM-DD)
```
