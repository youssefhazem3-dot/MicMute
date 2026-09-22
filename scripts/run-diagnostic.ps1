param(
    [string]$DotnetPath,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "   MicMute Local Diagnostic Runner (PowerShell)" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Terminate any running MicMute processes to avoid locks
Get-Process -Name "MicMute" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "[*] Terminating existing MicMute process (PID: $($_.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force
}

# 2. Resolve dotnet path
if (!$DotnetPath) {
    $localDotnet = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
    $DotnetPath = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.tools/nuget-packages'

if (!$NoBuild) {
    Write-Host "[*] Compiling MicMute (Debug)..." -ForegroundColor Gray
    $csproj = Join-Path $projectRoot "MicMute.csproj"
    & $DotnetPath build $csproj -c Debug --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

$exePath = Join-Path $projectRoot "bin/Debug/net8.0-windows/win-x64/MicMute.exe"
if (!(Test-Path -LiteralPath $exePath)) {
    Write-Error "Could not locate binary at: $exePath"
    exit 1
}

Write-Host ""
Write-Host "[*] Launching MicMute with --diagnostic --show..." -ForegroundColor Green
Write-Host "[*] Live events will stream in the console. Close the window or press Ctrl+C to exit." -ForegroundColor DarkGray
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

$proc = Start-Process -FilePath $exePath -ArgumentList "--diagnostic", "--show" -PassThru
$proc.WaitForExit()
Write-Host "[INFO] MicMute process exited with code $($proc.ExitCode)." -ForegroundColor Cyan

