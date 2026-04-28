[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$SdkVersion = "6.0.428"
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$dotnetDir = Join-Path $repoRoot ".dotnet"
$dotnetExe = Join-Path $dotnetDir "dotnet.exe"
$projectPath = Join-Path $repoRoot "src\\BannerlordCompanionCoop\\BannerlordCompanionCoop.csproj"

if (-not (Test-Path $dotnetExe)) {
    $installScript = Join-Path $env:TEMP "bannerlord-companion-coop-dotnet-install.ps1"
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript
    & $installScript -Version $SdkVersion -InstallDir $dotnetDir -NoPath
}

& $dotnetExe build $projectPath -c $Configuration -nologo
exit $LASTEXITCODE
