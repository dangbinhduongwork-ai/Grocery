# Windows PowerShell wrapper: runs the same checks with ripgrep (install: winget install BurntSushi.ripgrep.MSVC)
# Run from the repository root:  powershell -ExecutionPolicy Bypass -File scripts/verify-reality.ps1
$ErrorActionPreference = 'Continue'
$fail = 0
function Check($title, [string[]]$rgArgs) {
  $out = & rg --no-heading -n @rgArgs 2>$null
  if ($out) {
    Write-Host "### VIOLATION: $title" -ForegroundColor Red
    $out | Select-Object -First 40
    Write-Host ""
    $script:fail = 1
  }
}
Check 'NotImplementedException in source' @('-g','*.cs','NotImplementedException','src')
Check 'TODO/FIXME without issue id'      @('--pcre2','-g','*.cs','-g','*.ts','-g','*.tsx','(TODO|FIXME)(?!\(Q-[0-9]+\))','src','web/src')
Check 'Skipped tests (C#)'               @('-g','*.cs','\[(Fact|Theory)\(Skip','tests')
Check 'Skipped tests (JS/TS)'            @('-g','*.ts','-g','*.tsx','(\.skip\(|\bxit\(|\bxdescribe\(|test\.fixme)','web')
Check 'Thread.Sleep'                     @('-g','*.cs','Thread\.Sleep','src','tests')
Check 'Fake libs in production code'     @('-g','*.cs','(Bogus|Faker)','src')
Check 'Client-side paid confirmation'    @('-g','*.ts','-g','*.tsx','(markAsPaid|setPaid|paymentSucceeded\s*=\s*true)','web/src')
Check 'Tokens in browser storage'        @('-i','-g','*.ts','-g','*.tsx','(localStorage|sessionStorage)\.(set|get)Item\(.*(token|jwt)','web/src')
Check 'Commercial-license libs'          @('-g','*.csproj','-g','Directory.Packages.props','(MediatR|AutoMapper|FluentAssertions|MassTransit)','.')
if ($fail -eq 0) { Write-Host 'verify-reality: no violations' -ForegroundColor Green }
exit $fail
