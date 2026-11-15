# =============================================================================
# GroceryHub — Database Backup Utility (PowerShell)
# Backs up SQL Server database, validates CHECKSUM, and computes SHA-256 hash
# =============================================================================
[CmdletBinding()]
param(
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
    [string]$BackupDir = "$PSScriptRoot/../backups",
    [string]$DatabaseName = "GroceryHub"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=GroceryHub;Integrated Security=True;TrustServerCertificate=True;"
}

$resolvedBackupDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../backups"))
if (-not (Test-Path $resolvedBackupDir)) {
    New-Item -ItemType Directory -Path $resolvedBackupDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFileName = "${DatabaseName}_Full_${timestamp}.bak"
$backupFilePath = Join-Path $resolvedBackupDir $backupFileName

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " GroceryHub Database Backup" -ForegroundColor Cyan
Write-Host " Database:   $DatabaseName" -ForegroundColor Gray
Write-Host " Target File: $backupFilePath" -ForegroundColor Gray
Write-Host "============================================================" -ForegroundColor Cyan

$conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
try {
    $conn.Open()
    
    # Query SQL Server Edition to determine backup feature support
    $verCmd = $conn.CreateCommand()
    $verCmd.CommandText = "SELECT CAST(SERVERPROPERTY('Edition') AS NVARCHAR(128));"
    $edition = [string]$verCmd.ExecuteScalar()
    Write-Host " SQL Edition: $edition" -ForegroundColor Gray

    $isExpress = ($edition -match "Express")
    $compressionClause = if ($isExpress) { "" } else { ", COMPRESSION" }

    $backupSql = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$backupFilePath'
WITH FORMAT, INIT, CHECKSUM$compressionClause, STATS = 10;
RESTORE VERIFYONLY FROM DISK = N'$backupFilePath' WITH CHECKSUM;
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $backupSql
    $cmd.CommandTimeout = 300

    Write-Host "[1/3] Executing BACKUP DATABASE with CHECKSUM$(if (-not $isExpress) { ' and COMPRESSION' })..." -ForegroundColor Yellow
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "[2/3] RESTORE VERIFYONLY validation succeeded." -ForegroundColor Green
}
finally {
    $conn.Close()
}

if (-not (Test-Path $backupFilePath)) {
    throw "Backup file was not created at expected path: $backupFilePath"
}

$fileInfo = Get-Item $backupFilePath
$fileSizeMb = [math]::Round($fileInfo.Length / 1MB, 2)

Write-Host "[3/3] Calculating SHA-256 checksum..." -ForegroundColor Yellow
$hashResult = Get-FileHash -Path $backupFilePath -Algorithm SHA256

Write-Host ""
Write-Host "Backup Complete Successfully!" -ForegroundColor Green
Write-Host "  File:     $backupFileName" -ForegroundColor White
Write-Host "  Path:     $backupFilePath" -ForegroundColor White
Write-Host "  Size:     $fileSizeMb MB" -ForegroundColor White
Write-Host "  SHA-256:  $($hashResult.Hash)" -ForegroundColor White
Write-Host ""

# Output JSON summary
$summary = [PSCustomObject]@{
    DatabaseName = $DatabaseName
    BackupFile   = $backupFilePath
    SizeBytes    = $fileInfo.Length
    SizeMB       = $fileSizeMb
    Sha256       = $hashResult.Hash
    TimestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    Status       = "Verified"
}
$summaryJsonPath = [System.IO.Path]::ChangeExtension($backupFilePath, ".json")
$summary | ConvertTo-Json -Depth 3 | Set-Content -Path $summaryJsonPath -Encoding UTF8
Write-Host "Metadata saved to $summaryJsonPath" -ForegroundColor Gray
