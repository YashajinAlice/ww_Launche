#Requires -Version 5.1
# Publish a GitHub Release (zip + Setup) under the repo owner's account.
param(
    [string] $Version = "",
    [switch] $SkipBuild,
    [switch] $Draft
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$Proj = Join-Path $Root "src\WwLauncher\WwLauncher.csproj"
$OutDir = Join-Path $Root "docs\releases"
$Repo = "YashajinAlice/ww_Launche"

function Get-AppVersionFromCsproj {
    [xml] $xml = Get-Content -LiteralPath $Proj -Encoding UTF8
    $v = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $v) { throw "Cannot read Version from csproj." }
    return $v.Trim()
}

function Get-GitHubToken {
    if (-not [string]::IsNullOrEmpty($env:GH_TOKEN)) { return $env:GH_TOKEN }
    if (-not [string]::IsNullOrEmpty($env:GITHUB_TOKEN)) { return $env:GITHUB_TOKEN }
    $credInput = "protocol=https`nhost=github.com`n`n"
    $cred = $credInput | git credential fill 2>$null
    if (-not $cred) { throw "No GitHub token. Set GH_TOKEN or run git credential / gh auth login." }
    $line = ($cred -split "`n") | Where-Object { $_ -like "password=*" } | Select-Object -First 1
    if (-not $line) { throw "Git credential missing password." }
    return $line.Substring(9)
}

function Get-GhHeaders([string] $Token) {
    return @{
        Authorization = "Bearer $Token"
        Accept = "application/vnd.github+json"
        "User-Agent" = "YangBao-Release"
        "X-GitHub-Api-Version" = "2022-11-28"
    }
}

if (-not $Version) {
    $Version = Get-AppVersionFromCsproj
}

$tag = "v$Version"
$zip = Join-Path $OutDir ("YangBao-{0}-win-x64.zip" -f $Version)
$setup = Join-Path $OutDir ("YangBao-Setup-{0}-win-x64.exe" -f $Version)

if (-not $SkipBuild) {
    Write-Host "==> Build installer"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root "scripts\build-installer.ps1") -Version $Version
    if ($LASTEXITCODE -ne 0) { throw "build-installer failed." }

    $publishDir = Join-Path $Root "publish\win-x64"
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    Push-Location $publishDir
    try {
        tar -a -cf $zip *
    } finally {
        Pop-Location
    }
}

if (-not (Test-Path -LiteralPath $zip)) { throw "Missing zip: $zip" }
if (-not (Test-Path -LiteralPath $setup)) { throw "Missing setup: $setup" }

$notes = @"
## 秧寶 $Version

- 安裝檔：``YangBao-Setup-$Version-win-x64.exe``（繁中：使用權政策、安裝路徑、捷徑）
- 免安裝更新包：``YangBao-$Version-win-x64.zip``（應用內檢查更新）
"@

$token = Get-GitHubToken
$headers = Get-GhHeaders $token

Write-Host "==> Create release $tag"
$bodyObj = @{
    tag_name = $tag
    name = "秧寶 $Version"
    body = $notes
    draft = [bool]$Draft
    prerelease = $false
}
$bodyJson = $bodyObj | ConvertTo-Json -Depth 5
$utf8 = New-Object System.Text.UTF8Encoding $false
$bodyBytes = $utf8.GetBytes($bodyJson)

$release = Invoke-RestMethod `
    -Method Post `
    -Uri "https://api.github.com/repos/$Repo/releases" `
    -Headers $headers `
    -ContentType "application/json; charset=utf-8" `
    -Body $bodyBytes

$uploadUrlTemplate = $release.upload_url
if (-not $uploadUrlTemplate) { throw "Release created but upload_url missing." }

function Upload-Asset([string] $Path) {
    $name = [IO.Path]::GetFileName($Path)
    $uploadUrl = ($uploadUrlTemplate -replace '\{\?name,label\}', '') + "?name=$([Uri]::EscapeDataString($name))"
    $bytes = [IO.File]::ReadAllBytes($Path)
    $uploadHeaders = Get-GhHeaders $token
    $uploadHeaders["Content-Type"] = "application/octet-stream"
    Write-Host ("==> Upload " + $name)
    Invoke-RestMethod -Method Post -Uri $uploadUrl -Headers $uploadHeaders -Body $bytes | Out-Null
}

Upload-Asset $zip
Upload-Asset $setup

Write-Host ("==> Done: " + $release.html_url)
