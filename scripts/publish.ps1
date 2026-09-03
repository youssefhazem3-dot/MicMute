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
$arguments=@('publish',(Join-Path $projectRoot 'MicMute.csproj'),'-c','Release','-r','win-x64','--self-contained','false','-o',$stage,'-p:DebugType=None','-p:DebugSymbols=false')
if($NoRestore){$arguments+='--no-restore'}
& $DotnetPath @arguments
if($LASTEXITCODE -ne 0){throw 'Publish failed; package was not updated.'}
$files=@(Get-ChildItem -LiteralPath $stage -File | Where-Object {$_.Extension -in @('.exe','.dll','.json','.ico')})
foreach($required in @('MicMute.exe','MicMute.dll','MicMute.deps.json','MicMute.runtimeconfig.json','app.ico')){
    if($required -notin $files.Name){throw "Missing release artifact: $required"}
}
# Verify that launching the SDK-generated host will not open a console window.
$hostBytes=[IO.File]::ReadAllBytes((Join-Path $stage 'MicMute.exe'))
$peOffset=[BitConverter]::ToInt32($hostBytes,0x3c)
if([BitConverter]::ToUInt16($hostBytes,$peOffset+0x5c) -ne 2){throw 'The release executable is not a Windows GUI application.'}

function Copy-Release([string]$destination){
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    # Preflight all destination files before changing any of them.
    foreach($file in $files){
        $target=Join-Path $destination $file.Name
        if(Test-Path -LiteralPath $target){
            try{$stream=[IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$stream.Dispose()}
            catch{Write-Warning "Release is locked at $destination; its files were left unchanged. Extract the new ZIP after closing that instance.";return $false}
        }
    }
    $backup=Join-Path $projectRoot ('.artifacts/previous-release/'+$stamp+'/'+$(if($destination -eq $projectRoot){'root'}else{'publish'}))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    foreach($file in $files){$target=Join-Path $destination $file.Name;if(Test-Path -LiteralPath $target){Copy-Item -LiteralPath $target -Destination $backup -Force}}
    foreach($file in $files){Copy-Item -LiteralPath $file.FullName -Destination $destination -Force}
    return $true
}

$stageZip=Join-Path $projectRoot ".artifacts/MicMute-$stamp.zip"
Compress-Archive -LiteralPath $files.FullName -DestinationPath $stageZip -CompressionLevel Optimal
$zipPath=Join-Path $projectRoot 'MicMute.zip'
if(Test-Path -LiteralPath $zipPath){Copy-Item -LiteralPath $zipPath -Destination (Join-Path $projectRoot ".artifacts/MicMute-before-$stamp.zip")}
Copy-Item -LiteralPath $stageZip -Destination $zipPath -Force
$publishUpdated=Copy-Release (Join-Path $projectRoot 'publish')
$rootUpdated=if($UpdateRoot){Copy-Release $projectRoot}else{$false}
[pscustomobject]@{Package=$zipPath;PackageSHA256=(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash;StagingDirectory=$stage;PublishUpdated=$publishUpdated;RootUpdated=$rootUpdated}
