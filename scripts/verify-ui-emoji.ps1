$files = Get-ChildItem -Path "$PSScriptRoot/../web/src" -Recurse -Include *.tsx,*.ts
$emojiPattern = '[\uD83C-\uDBFF\uDC00-\uDFFF\u2600-\u26FF\u2700-\u27BF]'
$hits = @()
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match $emojiPattern) {
        $hits += $f.FullName
    }
}
if ($hits.Count -gt 0) {
    Write-Output "Emoji found in files:"
    $hits | ForEach-Object { Write-Output $_ }
    exit 1
} else {
    Write-Output "Zero emoji found in web/src. Design system Lucide icons used exclusively."
}
