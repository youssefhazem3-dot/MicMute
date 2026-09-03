param(
    [ValidateSet('all','settings','hotkey','ui')][string]$Area = 'all',
    [string]$CompilerPath,
    [string]$DotnetPath,
    [switch]$NoRestore
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (!$DotnetPath) {
    $localDotnet = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
    $DotnetPath = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.tools/nuget-packages'
if (!$CompilerPath) {
    $testProject = Join-Path $projectRoot 'tests/MicMute.Tests/MicMute.Tests.csproj'
    $runArgs = @('run','--project',$testProject,'--configuration','Release')
    if ($NoRestore) { $runArgs += '--no-restore' }
    & $DotnetPath @runArgs -- $Area
    exit $LASTEXITCODE
}

# Optional installed-compiler fallback for isolated regression work without an SDK.
$outDir = Join-Path $projectRoot ".artifacts/tests-$Area"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$runtimeLines = & $DotnetPath --list-runtimes
function Find-Runtime([string]$Name) {
    $line = $runtimeLines | Where-Object { $_ -match ('^' + [regex]::Escape($Name) + ' 8\.') } | Select-Object -Last 1
    if (!$line -or $line -notmatch '^\S+ (\S+) \[(.+)\]$') { throw "Missing .NET 8 runtime: $Name" }
    return Join-Path $Matches[2] $Matches[1]
}
$coreDir = Find-Runtime 'Microsoft.NETCore.App'
$desktopDir = Find-Runtime 'Microsoft.WindowsDesktop.App'
$references = @{}
foreach ($directory in @($coreDir,$desktopDir,$projectRoot)) {
    foreach ($file in Get-ChildItem -LiteralPath $directory -Filter '*.dll' -File) {
        if ($file.Name -like 'MicMute*.dll') { continue }
        try { $null = [Reflection.AssemblyName]::GetAssemblyName($file.FullName); $references[$file.Name] = $file.FullName } catch { }
    }
}
$sources = @(Join-Path $projectRoot 'tests/MicMute.Tests/Program.cs')
if ($Area -eq 'all') { $sources += @(Get-ChildItem -LiteralPath $projectRoot -Filter '*.cs' -File | ForEach-Object FullName) }
else {
    $patterns = switch ($Area) { 'settings' { @('Settings*.cs','AppSettings.cs') }; 'hotkey' { @('Hotkey*.cs','RawKeyboard*.cs') }; 'ui' { @('UiBehavior.cs','RefreshGeneration.cs') } }
    foreach ($pattern in $patterns) { $sources += @(Get-ChildItem -LiteralPath $projectRoot -Filter $pattern -File | ForEach-Object FullName) }
}
$casePattern = if ($Area -eq 'all') { '*Cases.cs' } else { "$($Area)Cases.cs" }
$sources += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests/MicMute.Tests') -Filter $casePattern -File | ForEach-Object FullName)
$output = Join-Path $outDir 'MicMute.Tests.dll'
$response = @('/nologo','/target:exe','/nostdlib+','/langversion:12','/nullable:enable','/platform:x64','/main:MicMute.Tests.Program',('/out:"' + $output + '"'))
foreach ($reference in $references.Values) { $response += '/r:"' + $reference + '"' }
foreach ($source in ($sources | Select-Object -Unique)) { $response += '"' + $source + '"' }
$responseFile = Join-Path $outDir 'compile.rsp'
[IO.File]::WriteAllLines($responseFile,$response)
& $DotnetPath exec $CompilerPath "@$responseFile"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$runtimeConfig = @{runtimeOptions=@{tfm='net8.0';frameworks=@(@{name='Microsoft.NETCore.App';version='8.0.0'},@{name='Microsoft.WindowsDesktop.App';version='8.0.0'})}} | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $outDir 'MicMute.Tests.runtimeconfig.json'),$runtimeConfig)
foreach ($reference in $references.Values | Where-Object { $_ -like (Join-Path $projectRoot 'NAudio*') }) { Copy-Item -LiteralPath $reference -Destination $outDir -Force }
& $DotnetPath $output $Area
exit $LASTEXITCODE
