#Requires -Version 5.1
# Build YangBao Inno Setup installer (Traditional Chinese wizard).
param(
    [string] $Version = "",
    [switch] $SkipPublish,
    [switch] $InstallInnoIfMissing
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$Proj = Join-Path $Root "src\WwLauncher\WwLauncher.csproj"
$PublishDir = Join-Path $Root "publish\win-x64"
$Iss = Join-Path $Root "installer\YangBao.iss"
$OutDir = Join-Path $Root "docs\releases"

function Get-AppVersionFromCsproj {
    [xml] $xml = Get-Content -LiteralPath $Proj -Encoding UTF8
    $v = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $v) { throw "Cannot read Version from csproj." }
    return $v.Trim()
}

function Find-ISCC {
    $roots = @(
        (Join-Path $env:LocalAppData "Programs\Inno Setup 6"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6"),
        (Join-Path $env:ProgramFiles "Inno Setup 6"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 7"),
        (Join-Path $env:ProgramFiles "Inno Setup 7")
    )
    foreach ($dir in $roots) {
        if (-not $dir) { continue }
        $p = Join-Path $dir "ISCC.exe"
        if (Test-Path -LiteralPath $p) { return $p }
    }
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

if (-not $Version) {
    $Version = Get-AppVersionFromCsproj
}

Write-Host "==> Version: $Version"

if (-not $SkipPublish) {
    Write-Host "==> Publish win-x64 -> $PublishDir"
    if (Test-Path -LiteralPath $PublishDir) {
        Remove-Item -LiteralPath $PublishDir -Recurse -Force
    }
    & dotnet publish $Proj -c Release -r win-x64 -p:Platform=x64 --self-contained false -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }
}

$exe = Join-Path $PublishDir "WwLauncher.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Missing $exe. Run publish first."
}

$iscc = Find-ISCC
if (-not $iscc) {
    if ($InstallInnoIfMissing) {
        Write-Host "==> Installing Inno Setup via winget..."
        & winget install --id JRSoftware.InnoSetup -e --accept-package-agreements --accept-source-agreements
        $iscc = Find-ISCC
    }
    if (-not $iscc) {
        throw "ISCC.exe not found. Install with: winget install JRSoftware.InnoSetup"
    }
}

Write-Host "==> Inno Setup: $iscc"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

& $iscc ("/DMyAppVersion=$Version") $Iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed." }

$setup = Join-Path $OutDir ("YangBao-Setup-{0}-win-x64.exe" -f $Version)
if (-not (Test-Path -LiteralPath $setup)) {
    throw "Expected output missing: $setup"
}

Write-Host "==> Done: $setup"
Get-Item -LiteralPath $setup | Format-List FullName, Length, LastWriteTime
