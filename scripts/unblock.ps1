param(
    [string]$Target = "$PSScriptRoot\..\MicMute.exe"
)

$resolved = Resolve-Path $Target -ErrorAction SilentlyContinue
if ($resolved) {
    Unblock-File -Path $resolved.Path
    Write-Host "Successfully stripped Mark of the Web (Zone.Identifier) from: $($resolved.Path)" -ForegroundColor Green
} else {
    Write-Warning "File not found: $Target"
}
