# GroceryHub — Environment Audit & Workstation Profile

- **Last Audited:** 2026-11-15 (Phase 00)
- **Evidence Folder:** `docs/evidence/phase-00/`

## 1. Host Machine & Operating System

| Property | Value | Status | Evidence |
|---|---|---|---|
| OS | Microsoft Windows 10 Pro `10.0.19045` (Build 19045, 64-bit) | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| Shell | Windows PowerShell `5.1.19041.6456` (Desktop) | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| Total Visible RAM | `31.91 GB` | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| Free Physical RAM | `16.47 GB` (SQL Server container requires >= 2 GB) | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| Disk Space (`C:`) | `56.34 GB` free (`181.53 GB` used) | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| Disk Space (`D:`) | `352.02 GB` free (`155.67 GB` used) | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| Disk Space (`E:` workspace) | `254.12 GB` free (`169.70 GB` used) | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| Antigravity Surface | **Antigravity IDE** `ideVersion: 2.7.0` (VS Code base `1.107.0`, commit `71bedb45923debaf49871bdcb7c7d3a47918e247`) | **PASS** | `docs/evidence/phase-00/16-antigravity-surface-version.txt` |

## 2. Installed & Missing Development Tools

| Tool | Command | Actual Output | Requirement | Status | Evidence |
|---|---|---|---|---|---|
| `.NET SDK` | `dotnet --list-sdks` | `9.0.316 [C:\Program Files\dotnet\sdk]` | `.NET 10.x LTS SDK` (`10.0.404` available in winget) | **FAIL (Missing .NET 10 SDK)** | `docs/evidence/phase-00/03-dotnet-sdks.txt` |
| `Node.js` | `node --version` | `v24.11.1` | Node LTS (>= 20/22) | **PASS** | `docs/evidence/phase-00/04-node-version.txt` |
| `npm` | `npm --version` | `11.6.2` | npm >= 10 | **PASS** | `docs/evidence/phase-00/05-npm-version.txt` |
| `Git` | `git --version` | `git version 2.55.0.windows.5` | Git >= 2.30 | **PASS** | `docs/evidence/phase-00/06-git-version.txt` |
| `Docker CLI` | `docker --version` | `CommandNotFoundException` | Docker CLI installed | **FAIL (Missing)** | `docs/evidence/phase-00/07-docker-version.txt` |
| `Docker Compose` | `docker compose version` | `CommandNotFoundException` | Docker Compose v2 | **FAIL (Missing)** | `docs/evidence/phase-00/08-docker-compose-version.txt` |
| `Docker Daemon` | `docker info` | `CommandNotFoundException` | Running Docker daemon | **FAIL (Missing)** | `docs/evidence/phase-00/11-docker-info.txt` |
| `ripgrep (rg)` | `rg --version` | Not in system `PATH` (bundled `ripgrep 13.0.0` present in Antigravity IDE) | `rg` in system `PATH` | **FAIL (Not in system PATH; local fallback active)** | `docs/evidence/phase-00/09-ripgrep-version.txt`, `15-bundled-rg-search.txt` |
| `sqlcmd` | `sqlcmd -?` | `Version 16.0.1000.6 NT` | Optional | **PASS** | `docs/evidence/phase-00/10-sqlcmd.txt` |
| `winget` | `winget --version` | `v1.29.380` | Package installer | **PASS** | `docs/evidence/phase-00/13-antigravity-and-winget.txt` |

## 3. Required Local Ports Check

| Port | Service | State | Status | Evidence |
|---|---|---|---|---|
| `1433` | SQL Server container | `FREE` (4 local Windows SQL Server services are `Stopped`) | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt`, `13-antigravity-and-winget.txt` |
| `5173` | Vite dev server (`web`) | `FREE` | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| `8080` | `GroceryHub.Api` / reverse proxy | `FREE` | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |
| `8025` | Mailpit web UI (dev SMTP catcher) | `FREE` | **PASS** | `docs/evidence/phase-00/12-system-and-ports.txt` |

## 4. Exact Windows Installation Commands (User Action Before Phase 04)

Verified via `winget search` (`docs/evidence/phase-00/18-devops-tool-verification.txt`):

```powershell
# 1. Install .NET 10.0 LTS SDK (v10.0.404)
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget

# 2. Install ripgrep (v15.2.0) into system PATH
winget install --id BurntSushi.ripgrep.MSVC --exact --source winget

# 3. Ensure WSL 2 is enabled and install Docker Desktop (v4.99.0)
wsl --install --no-distribution
winget install --id Docker.DockerDesktop --exact --source winget
```
After installation, open a new PowerShell window, start **Docker Desktop**, and confirm:
`dotnet --list-sdks`, `rg --version`, `docker --version`, `docker compose version`, and `docker info`.
