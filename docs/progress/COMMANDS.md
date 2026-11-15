# GroceryHub — Canonical Commands Reference

Exact commands for auditing, building, testing, linting, and running GroceryHub. Updated as each phase introduces new tooling.

---

## 1. Reality Scan & Repository Verification

```powershell
# Run anti-fabrication / reality scan on Windows PowerShell
powershell -ExecutionPolicy Bypass -File scripts/verify-reality.ps1
```

```bash
# Run anti-fabrication / reality scan on Bash / Linux CI
bash scripts/verify-reality.sh
```

---

## 2. Backend Build, Format & Test Commands (`.NET 10`)

```powershell
# 1. Restore NuGet packages centrally via Directory.Packages.props
dotnet restore GroceryHub.sln

# 2. Verify code formatting against .editorconfig
dotnet format GroceryHub.sln --verify-no-changes --no-restore

# 3. Build entire solution treating warnings as errors
dotnet build GroceryHub.sln -warnaserror --no-restore

# 4. Check for vulnerable dependencies across direct and transitive packages
dotnet list GroceryHub.sln package --vulnerable --include-transitive

# 5. Run all Unit, Architecture, and Integration tests
dotnet test GroceryHub.sln --no-build

# 6. Run only Unit tests
dotnet test tests/GroceryHub.UnitTests/GroceryHub.UnitTests.csproj --no-build

# 7. Run Architecture tests (verifying module boundaries, default-deny auth, banned libraries)
dotnet test tests/GroceryHub.ArchitectureTests/GroceryHub.ArchitectureTests.csproj --no-build

# 8. Run Integration tests (using real SQL Server via Testcontainers or LocalDB + Respawn)
dotnet test tests/GroceryHub.IntegrationTests/GroceryHub.IntegrationTests.csproj --no-build
```

---

## 3. OpenAPI Contract Generation & Host Execution

```powershell
# 1. Export formatted OpenAPI 3.1 contract to docs/contracts/openapi.json
dotnet run --project src/GroceryHub.Api -- --export-openapi docs/contracts/openapi.json

# 2. Run the API host locally (reads appsettings.Development.json)
dotnet run --project src/GroceryHub.Api

# 3. Test liveness and readiness endpoints via curl
curl -i http://localhost:8080/health/live
curl -i http://localhost:8080/health/ready
curl -i http://localhost:8080/api/v1/system/info
```

---

## 4. Entity Framework Core 10 & Migration Bundle Commands

```powershell
# 1. Restore local dotnet tools (includes dotnet-ef 10.0.0 from .config/dotnet-tools.json)
dotnet tool restore

# 2. Add a new migration
dotnet ef migrations add <MigrationName> --project src/GroceryHub.Persistence --startup-project src/GroceryHub.Api --output-dir Migrations

# 3. Generate idempotent SQL script for a migration
dotnet ef migrations script --project src/GroceryHub.Persistence --startup-project src/GroceryHub.Api --idempotent --output deploy/db/migration.sql

# 4. Build a self-contained executable Migration Bundle
dotnet ef migrations bundle --project src/GroceryHub.Persistence --startup-project src/GroceryHub.Api --output efbundle.exe --force

# 5. Execute Migration Bundle against a target SQL Server instance
./efbundle.exe --connection "Server=localhost,1433;Database=GroceryHub;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

---

## 5. Docker Compose Stack Commands

```powershell
# 1. Validate Docker Compose configuration
docker compose --env-file .env.example config

# 2. Start local SQL Server and Mailpit background containers
docker compose --env-file .env.example up -d sqlserver mailpit

# 3. Check container health status
docker compose ps

# 4. View logs
docker compose logs -f sqlserver

# 5. Stop containers
docker compose down
```
