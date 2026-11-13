param([string]$DotnetPath,[switch]$NoRestore,[switch]$SkipTests,[switch]$UpdateRoot)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
if(!$DotnetPath){$localDotnet=Join-Path $projectRoot '.tools/dotnet/dotnet.exe';$DotnetPath=if(Test-Path -LiteralPath $localDotnet){$localDotnet}else{(Get-Command dotnet -ErrorAction Stop).Source}}
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_NOLOGO='1'
$env:DOTNET_CLI_HOME=Join-Path $projectRoot '.tools/cli-home'
$env:NUGET_PACKAGES=Join-Path $projectRoot '.tools/nuget-packages'
if(!$SkipTests){
    & (Join-Path $PSScriptRoot 'test.ps1') -DotnetPath $DotnetPath -NoRestore:$NoRestore
    if($LASTEXITCODE -ne 0){throw 'Regression tests failed; package was not updated.'}
}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$stage=Join-Path $projectRoot ".artifacts/publish-$stamp"
$arguments=@('publish',(Join-Path $projectRoot 'MicMute.csproj'),'-c','Release','-r','win-x64','--self-contained','true','-p:PublishSingleFile=true','-p:IncludeNativeLibrariesForSelfExtract=true','-p:EnableCompressionInSingleFile=true','-o',$stage,'-p:DebugType=None','-p:DebugSymbols=false')
if($NoRestore){$arguments+='--no-restore'}
& $DotnetPath @arguments
if($LASTEXITCODE -ne 0){throw 'Publish failed; package was not updated.'}
$files=@(Get-ChildItem -LiteralPath $stage -File | Where-Object {$_.Extension -eq '.exe'})
foreach($required in @('MicMute.exe')){
    if($required -notin $files.Name){throw "Missing release artifact: $required"}
}
# Verify that launching the SDK-generated host will not open a console window.
$exeStagePath = Join-Path $stage 'MicMute.exe'
$fs = [IO.File]::OpenRead($exeStagePath)
try {
    $header = New-Object byte[] 4096
    [void]$fs.Read($header, 0, $header.Length)
    $peOffset = [BitConverter]::ToInt32($header, 0x3c)
    if ([BitConverter]::ToUInt16($header, $peOffset + 0x5c) -ne 2) {
        throw 'The release executable is not a Windows GUI application.'
    }
} finally {
    $fs.Dispose()
}

function Copy-Release([string]$destination){
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    foreach($file in $files){
        $target=Join-Path $destination $file.Name
        if(Test-Path -LiteralPath $target){
            try{$stream=[IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$stream.Dispose()}
            catch{Write-Warning "Release is locked at $destination; its files were left unchanged.";return $false}
        }
    }
    foreach($file in $files){Copy-Item -LiteralPath $file.FullName -Destination $destination -Force}
    return $true
}

$zipPath=Join-Path $projectRoot 'MicMute.zip'
Compress-Archive -LiteralPath $files.FullName -DestinationPath $zipPath -CompressionLevel Optimal -Force
$publishUpdated=Copy-Release (Join-Path $projectRoot 'publish')
$rootUpdated=if($UpdateRoot){
    $r = Copy-Release $projectRoot
    $parentDir = Split-Path -Parent $projectRoot
    if ($parentDir -and (Test-Path -LiteralPath (Join-Path $parentDir 'MicMute.exe'))) {
        [void](Copy-Release $parentDir)
    }
    $r
}else{$false}

# Prune older staging directories and legacy backup bloat to protect SSD space and lifespan
$artifactsDir = Join-Path $projectRoot '.artifacts'
if (Test-Path -LiteralPath $artifactsDir) {
    $legacyPrev = Join-Path $artifactsDir 'previous-release'
    if (Test-Path -LiteralPath $legacyPrev) {
        Remove-Item -LiteralPath $legacyPrev -Recurse -Force -ErrorAction SilentlyContinue
    }
    Get-ChildItem -LiteralPath $artifactsDir -Directory -Filter 'publish-*' |
        Sort-Object CreationTime -Descending |
        Select-Object -Skip 1 |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    Get-ChildItem -LiteralPath $artifactsDir -File -Filter 'MicMute-*.zip' |
        Remove-Item -Force -ErrorAction SilentlyContinue
}

[pscustomobject]@{Package=$zipPath;PackageSHA256=(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash;StagingDirectory=$stage;PublishUpdated=$publishUpdated;RootUpdated=$rootUpdated}
