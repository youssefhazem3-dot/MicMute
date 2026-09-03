param([string]$DotnetPath,[ValidateSet('Debug','Release')][string]$Configuration='Release',[switch]$NoRestore)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
if(!$DotnetPath){$localDotnet=Join-Path $projectRoot '.tools/dotnet/dotnet.exe';$DotnetPath=if(Test-Path -LiteralPath $localDotnet){$localDotnet}else{(Get-Command dotnet -ErrorAction Stop).Source}}
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_NOLOGO='1'
$env:DOTNET_CLI_HOME=Join-Path $projectRoot '.tools/cli-home'
$env:NUGET_PACKAGES=Join-Path $projectRoot '.tools/nuget-packages'
$buildArgs=@('build',(Join-Path $projectRoot 'MicMute.csproj'),'--configuration',$Configuration)
if($NoRestore){$buildArgs+='--no-restore'}
& $DotnetPath @buildArgs
exit $LASTEXITCODE
