# Windows PowerShell wrapper: runs the same checks with ripgrep (install: winget install BurntSushi.ripgrep.MSVC)
# Run from the repository root:  powershell -ExecutionPolicy Bypass -File scripts/verify-reality.ps1
$ErrorActionPreference = 'Continue'

# Locate ripgrep (rg): prefer system PATH, fallback to local Antigravity IDE / VS Code bundled rg.exe with a warning, fail if neither exists
$rgCmd = Get-Command rg -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
if (-not $rgCmd) {
  $fallbackCandidates = @(
    "$env:LOCALAPPDATA\Programs\Antigravity IDE\resources\app\node_modules\@vscode\ripgrep\bin\rg.exe",
    "$env:LOCALAPPDATA\Programs\Microsoft VS Code\resources\app\node_modules\@vscode\ripgrep\bin\rg.exe",
    "C:\Program Files\Azure Data Studio\resources\app\node_modules.asar.unpacked\@vscode\ripgrep\bin\rg.exe"
  )
  foreach ($candidate in $fallbackCandidates) {
    if (Test-Path $candidate) {
      $rgCmd = $candidate
      Write-Host "WARNING: 'rg' is not in system PATH; using local binary: $rgCmd (Install globally via: winget install --id BurntSushi.ripgrep.MSVC --exact)" -ForegroundColor Yellow
      break
    }
  }
}

if (-not $rgCmd) {
  Write-Host "ERROR: ripgrep (rg) is not installed or not in PATH. Install with: winget install --id BurntSushi.ripgrep.MSVC --exact" -ForegroundColor Red
  exit 2
}

$fail = 0
function Check($title, [string[]]$rgArgs, [string[]]$paths) {
  $existingPaths = @($paths | Where-Object { Test-Path $_ })
  if ($existingPaths.Count -eq 0) {
    return
  }
  $out = & $rgCmd --no-heading -n @rgArgs @existingPaths 2>$null
  if ($out) {
    Write-Host "### VIOLATION: $title" -ForegroundColor Red
    $out | Select-Object -First 40
    Write-Host ""
    $script:fail = 1
  }
}

Check 'NotImplementedException in source'        @('-g','*.cs','NotImplementedException')                                                     @('src','web/src')
Check 'TODO/FIXME without issue id'              @('--pcre2','-g','*.cs','-g','*.ts','-g','*.tsx','(TODO|FIXME)(?!\(Q-[0-9]+\))')             @('src','web/src')
Check 'Skipped tests (C#)'                       @('-g','*.cs','\[(Fact|Theory)\(Skip')                                                       @('tests')
Check 'Skipped tests (JS/TS)'                    @('-g','*.ts','-g','*.tsx','(\.skip\(|\bxit\(|\bxdescribe\(|test\.fixme)')                   @('web')
Check 'Thread.Sleep in source/tests'             @('-g','*.cs','Thread\.Sleep')                                                               @('src','tests')
Check 'Fake/seed libraries in production code'   @('-g','*.cs','(Bogus|Faker)')                                                               @('src')
Check 'Client-side paid confirmation'            @('-g','*.ts','-g','*.tsx','(markAsPaid|setPaid|paymentSucceeded\s*=\s*true)')               @('web/src')
Check 'Tokens in browser storage'                @('-i','-g','*.ts','-g','*.tsx','(localStorage|sessionStorage)\.(set|get)Item\(.*(token|jwt)') @('web/src')
Check 'Emoji used in UI source'                  @('--pcre2','-g','*.tsx','[\x{1F300}-\x{1FAFF}\x{2600}-\x{27BF}]')                           @('web/src')
Check 'Hard-coded secrets patterns'              @('-g','!*.md','(sk_live|Authorization: Apikey [A-Za-z0-9]{8,}|Password=[^;{$]{4,};)')       @('src','web/src','deploy')
Check 'Commercial-license libs referenced'       @('-g','*.csproj','-g','Directory.Packages.props','(MediatR|AutoMapper|FluentAssertions|MassTransit)') @('.')

if ($fail -eq 0) { Write-Host 'verify-reality: no violations' -ForegroundColor Green }
exit $fail
