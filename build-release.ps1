# Build portable app + installer for GitHub Releases
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$out = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$appOut = Join-Path $out "app"
$setupOut = Join-Path $out "setup"
Remove-Item $appOut, $setupOut -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish "ThesGamer\ThesGamer.csproj" -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -p:PublishTrimmed=false -o $appOut

dotnet publish "ThesGamer.Setup\ThesGamer.Setup.csproj" -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -p:PublishTrimmed=false -o $setupOut

Copy-Item (Join-Path $appOut "ThesGamer.exe") (Join-Path $setupOut "ThesGamer.exe") -Force

$ver = "0.4.0"
$appZip = Join-Path $out "ThesGamer-$ver-win-x64.zip"
$setupZip = Join-Path $out "ThesGamer-Setup-$ver-win-x64.zip"
Remove-Item $appZip, $setupZip -Force -ErrorAction SilentlyContinue

Compress-Archive -Path (Join-Path $appOut "ThesGamer.exe") -DestinationPath $appZip
Compress-Archive -Path @(
  (Join-Path $setupOut "ThesGamer-Setup.exe"),
  (Join-Path $setupOut "ThesGamer.exe")
) -DestinationPath $setupZip

Copy-Item (Join-Path $appOut "ThesGamer.exe") (Join-Path $out "ThesGamer.exe") -Force
Copy-Item (Join-Path $setupOut "ThesGamer-Setup.exe") (Join-Path $out "ThesGamer-Setup.exe") -Force

$h1 = (Get-FileHash $appZip -Algorithm SHA256).Hash
$h2 = (Get-FileHash $setupZip -Algorithm SHA256).Hash
@"
$h1  ThesGamer-$ver-win-x64.zip
$h2  ThesGamer-Setup-$ver-win-x64.zip
"@ | Set-Content (Join-Path $out "CHECKSUMS-v$ver.sha256")

Write-Host "OK"
Write-Host $appZip
Write-Host $setupZip
