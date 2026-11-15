# Verify production compose port isolation
$composeContent = Get-Content -Path "$PSScriptRoot/../docker-compose.yml" -Raw

# Parse lines under services
$lines = $composeContent -split "`r?`n"
$currentService = ""
$servicePorts = @{}

foreach ($line in $lines) {
    if ($line -match '^  ([a-zA-Z0-9_-]+):') {
        $currentService = $matches[1]
    }
    elseif ($line -match '^    ports:') {
        $servicePorts[$currentService] = $true
    }
}

Write-Output "Production Docker Compose Port Audit:"
foreach ($svc in @("sqlserver", "migrator", "api", "web")) {
    $hasPorts = [bool]$servicePorts[$svc]
    Write-Output "  Service [$svc]: Exposed = $hasPorts"
}

if ($servicePorts["sqlserver"]) {
    Write-Error "CRITICAL: sqlserver port is exposed in production docker-compose.yml!"
    exit 1
}

if ($servicePorts["api"]) {
    Write-Error "CRITICAL: api port is exposed directly in production docker-compose.yml!"
    exit 1
}

if (-not $servicePorts["web"]) {
    Write-Error "CRITICAL: web port should be exposed in production docker-compose.yml!"
    exit 1
}

Write-Output "AUDIT PASS: Only 'web' service publishes host port. Database and API are strictly internal."
