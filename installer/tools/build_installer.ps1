[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$MarkerPatchDll,
    [Parameter(Mandatory = $true)][string]$SeveredAsi,
    [Parameter(Mandatory = $true)][string]$SevenZipExe,
    [Parameter(Mandatory = $true)][string]$SevenZipDll,
    [string]$AssetsRoot,
    [string]$UiAssetsDir,
    [string]$ChineseAssets,
    [string]$MarkerIni,
    [string]$OutputExe,
    [string]$Csc
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $AssetsRoot) { $AssetsRoot = Join-Path $root 'assets' }
if (-not $UiAssetsDir) { $UiAssetsDir = Join-Path $AssetsRoot 'ui' }
if (-not $ChineseAssets) { $ChineseAssets = Join-Path $AssetsRoot 'Chinese.assets.R8.7z' }
if (-not $MarkerIni) { $MarkerIni = Join-Path $AssetsRoot 'MarkerPatch.ini' }
if (-not $OutputExe) { $OutputExe = Join-Path $root 'output\DS2SeveredOneClick.exe' }
if (-not $Csc) { $Csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }

function Assert-Hash {
    param([string]$Path, [string]$Expected, [long]$Bytes)
    $item = Get-Item -LiteralPath $Path
    if ($item.Length -ne $Bytes) { throw "Size mismatch: $Path" }
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Expected) { throw "SHA256 mismatch: $Path`n$actual" }
}

Assert-Hash $MarkerPatchDll 'c6923b5ecd4c941a9894c21f145a51931efea9ef4ed384a16ee53680563232e5' 1725952
Assert-Hash $SeveredAsi '9a4542eccdbf88ab3ca0c02c52aca34eff9d0ccf3d2fdf71fd1a6314cb561a46' 46080
Assert-Hash $SevenZipExe '94205cfc054dd3bbeb978905bc619a65b3f04e9e73bb2717841110b6f46d962e' 577536
Assert-Hash $SevenZipDll '78bf6b668d75801e3aa180b4ccb16e6d8395fba80dec8a065055bb6f8dfabb82' 1908736
Assert-Hash $ChineseAssets '8c035b6ba2b0ad9e96967866281607b55db14a5a1261b413ec3faee93bd7105a' 6981480


$outputDir = Split-Path -Parent $OutputExe
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
if ($sources.Count -lt 3) { throw 'Missing installer source files.' }
foreach ($name in @('main_background.png','button.png','Severed.poster.jpg','MarkerPatch_Logo.png','app.ico')) {
    if (-not (Test-Path -LiteralPath (Join-Path $UiAssetsDir $name))) { throw "Missing UI asset: $name" }
}
$manifest = Join-Path $root 'src\app.manifest'

$compileArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:x86',
    '/optimize+',
    "/win32manifest:$manifest",
    "/win32icon:$(Join-Path $UiAssetsDir 'app.ico')",
    "/out:$OutputExe",
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Web.Extensions.dll',
    "/resource:$(Join-Path $UiAssetsDir 'app.ico'),DS2_APP_ICON",
    "/resource:$(Join-Path $UiAssetsDir 'main_background.png'),DS2_LEGACY_BG",
    "/resource:$(Join-Path $UiAssetsDir 'button.png'),DS2_LEGACY_BUTTON",
    "/resource:$(Join-Path $UiAssetsDir 'Severed.poster.jpg'),DS2_DLC_POSTER",
    "/resource:$(Join-Path $UiAssetsDir 'MarkerPatch_Logo.png'),DS2_MARKER_LOGO",
    "/resource:$SevenZipExe,DS2_7Z_EXE",
    "/resource:$SevenZipDll,DS2_7Z_DLL",
    "/resource:$ChineseAssets,DS2_CHINESE_ASSETS",
    "/resource:$MarkerPatchDll,DS2_MARKER_DLL",
    "/resource:$SeveredAsi,DS2_SEVERED_ASI",
    "/resource:$MarkerIni,DS2_MARKER_INI"
)
$compileArgs += $sources
& $Csc @compileArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$built = Get-Item -LiteralPath $OutputExe
[pscustomobject]@{
    path = $built.FullName
    bytes = $built.Length
    sha256 = (Get-FileHash -LiteralPath $built.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
} | Format-List
