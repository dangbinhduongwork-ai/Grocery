# GroceryHub — Canonical Commands Reference

Exact commands for auditing, building, testing, linting, and running GroceryHub. Updated as each phase introduces new tooling.

## 1. Reality Scan & Repository Verification (Available in Phase 00)

```powershell
# Run anti-fabrication / reality scan on Windows PowerShell
powershell -ExecutionPolicy Bypass -File scripts/verify-reality.ps1
```

```bash
# Run anti-fabrication / reality scan on Bash / CI
bash scripts/verify-reality.sh
```

## 2. Missing Prerequisite Installation Commands (Windows `winget` — Required before Phase 04)

```powershell
# 1. Install .NET 10 SDK (LTS)
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget

# 2. Install Docker Desktop for Windows (requires WSL 2 enabled and system restart/sign-out)
winget install --id Docker.DockerDesktop --exact --source winget

# 3. Install ripgrep (rg) in system PATH for verify-reality scripts
winget install --id BurntSushi.ripgrep.MSVC --exact --source winget
```

## 3. Backend Commands (Active from Phase 04+)

```powershell
dotnet restore
dotnet build -warnaserror
dotnet test --configuration Release --no-build
```

## 4. Frontend Commands (Active from Phase 08+)

```powershell
cd web
npm ci
npm run lint
npm run typecheck
npm test -- --run
npm run build
```

## 5. Docker Compose Stack Commands (Active from Phase 04 / Phase 12+)

```powershell
docker compose up -d --build
docker compose ps
docker compose logs -f api
docker compose down
```
