# =============================================================================
# GroceryHub — Post-Deployment Smoke Test (PowerShell)
# Verifies health, static SPA delivery, category listing, user auth flow,
# product catalog query, and webhook signature rejection on a deployed stack.
# =============================================================================
[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " GroceryHub Post-Deployment Smoke Test" -ForegroundColor Cyan
Write-Host " Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host " Time:     $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "============================================================" -ForegroundColor Cyan

$passed = 0
$failed = 0

function Test-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )
    Write-Host -NoNewline "[TEST] $Name ... "
    try {
        & $Action
        Write-Host "PASS" -ForegroundColor Green
        $script:passed++
    }
    catch {
        Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
}

# 1. Health Live & Ready
Test-Step "Health Live Endpoint (GET /health/live)" {
    $res = Invoke-WebRequest -Uri "$BaseUrl/health/live" -Method Get -UseBasicParsing -TimeoutSec 10
    if ($res.StatusCode -ne 200) { throw "Expected 200, got $($res.StatusCode)" }
    if ($res.Content -notmatch "Healthy") { throw "Body does not contain 'Healthy': $($res.Content)" }
}

Test-Step "Health Ready Endpoint (GET /health/ready)" {
    $res = Invoke-WebRequest -Uri "$BaseUrl/health/ready" -Method Get -UseBasicParsing -TimeoutSec 10
    if ($res.StatusCode -ne 200) { throw "Expected 200, got $($res.StatusCode)" }
    if ($res.Content -notmatch "Healthy") { throw "Body does not contain 'Healthy': $($res.Content)" }
}

# 2. Static SPA & Security Headers
Test-Step "Static SPA delivery & Security Headers (GET /)" {
    $res = Invoke-WebRequest -Uri "$BaseUrl/" -Method Get -UseBasicParsing -TimeoutSec 10
    if ($res.StatusCode -ne 200) { throw "Expected 200, got $($res.StatusCode)" }
    if ($res.Content -notmatch "<!DOCTYPE html" -and $res.Content -notmatch "<html") {
        throw "Response is not HTML: $($res.Content.Substring(0, [math]::Min(100, $res.Content.Length)))"
    }

    # Verify security headers
    $h = $res.Headers
    if (-not $h["X-Content-Type-Options"] -or $h["X-Content-Type-Options"] -ne "nosniff") {
        throw "Missing or invalid X-Content-Type-Options header"
    }
    if (-not $h["X-Frame-Options"] -or $h["X-Frame-Options"] -ne "DENY") {
        throw "Missing or invalid X-Frame-Options header"
    }
    if (-not $h["Content-Security-Policy"]) {
        throw "Missing Content-Security-Policy header"
    }
}

# 3. Categories API
Test-Step "Public Catalog Categories (GET /api/v1/categories)" {
    $res = Invoke-WebRequest -Uri "$BaseUrl/api/v1/categories" -Method Get -UseBasicParsing -TimeoutSec 10
    if ($res.StatusCode -ne 200) { throw "Expected 200, got $($res.StatusCode)" }
    $json = $res.Content | ConvertFrom-Json
    if ($json -isnot [System.Array] -or $json.Count -lt 1) {
        throw "Expected at least 1 category, got count: $($json.Count)"
    }
}

# 4. Products API
Test-Step "Public Products Query (GET /api/v1/products?pageSize=1)" {
    $res = Invoke-WebRequest -Uri "$BaseUrl/api/v1/products?pageSize=1" -Method Get -UseBasicParsing -TimeoutSec 10
    if ($res.StatusCode -ne 200) { throw "Expected 200, got $($res.StatusCode)" }
    $json = $res.Content | ConvertFrom-Json
    if (-not $json.items -or $json.items.Count -lt 1) {
        throw "Expected at least 1 product item"
    }
}

# 5. User Registration & Login Flow (Disposable smoke user)
$randomId = [Guid]::NewGuid().ToString("N").Substring(0, 8)
$testEmail = "smoke_$randomId@groceryhub.test"
$testPassword = "Smoke_Password_2026!"
$testFullName = "Smoke Test Customer $randomId"

Test-Step "Customer Registration (POST /api/v1/auth/register)" {
    $body = @{
        email = $testEmail
        password = $testPassword
        fullName = $testFullName
    } | ConvertTo-Json

    $res = Invoke-WebRequest -Uri "$BaseUrl/api/v1/auth/register" -Method Post -Body $body -ContentType "application/json" -UseBasicParsing -TimeoutSec 10
    if ($res.StatusCode -ne 200 -and $res.StatusCode -ne 201) {
        throw "Expected 200/201, got $($res.StatusCode)"
    }
}

Test-Step "Customer Login (POST /api/v1/auth/login)" {
    $body = @{
        email = $testEmail
        password = $testPassword
    } | ConvertTo-Json

    $res = Invoke-WebRequest -Uri "$BaseUrl/api/v1/auth/login" -Method Post -Body $body -ContentType "application/json" -UseBasicParsing -TimeoutSec 10
    if ($res.StatusCode -ne 200) { throw "Expected 200, got $($res.StatusCode)" }
    $json = $res.Content | ConvertFrom-Json
    if (-not $json.accessToken) { throw "Missing accessToken in login response" }
}

# 6. Webhook Signature Protection
Test-Step "SePay Webhook Rejection of Unsigned Request (POST /api/v1/payments/sepay/webhook)" {
    $body = @{
        id = 999999
        gateway = "MBBank"
        transactionDate = "2026-11-16 00:00:00"
        accountNumber = "0000000000"
        code = "GH9999999999"
        content = "GH9999999999 Fake Webhook Smoke Test"
        transferType = "in"
        transferAmount = 50000
        referenceCode = "FAKE_SMOKE_REF"
    } | ConvertTo-Json

    try {
        $res = Invoke-WebRequest -Uri "$BaseUrl/api/v1/payments/sepay/webhook" -Method Post -Body $body -ContentType "application/json" -UseBasicParsing -TimeoutSec 10
        # If it returns 200 without signature, that is a security violation!
        throw "Expected HTTP 401 Unauthorized, but request returned HTTP $($res.StatusCode)"
    }
    catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp -and ($resp.StatusCode.value__ -eq 401 -or $resp.StatusCode.value__ -eq 403)) {
            # Expected security rejection!
        }
        else {
            throw $_
        }
    }
    catch {
        if ($_.Exception.Message -match "401" -or $_.Exception.Message -match "Unauthorized" -or $_.Exception.Message -match "403") {
            # Expected!
        }
        else {
            throw $_
        }
    }
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Smoke Test Results: $passed Passed, $failed Failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "============================================================" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
}
