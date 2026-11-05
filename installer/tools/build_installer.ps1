[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$MarkerPatchDll,
    [Parameter(Mandatory = $true)][string]$SeveredAsi,
    [Parameter(Mandatory = $true)][string]$NodeAsi,
    [Parameter(Mandatory = $true)][string]$SaveAsi,
    [Parameter(Mandatory = $true)][string]$MiningAsi,
    [Parameter(Mandatory = $true)][string]$VCRedist,
    [Parameter(Mandatory = $true)][string]$SevenZipExe,
    [Parameter(Mandatory = $true)][string]$SevenZipDll,
    [string]$AssetsRoot,
    [string]$MarkerAssets,
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
if (-not $MarkerAssets) { $MarkerAssets = Join-Path $root 'build-deps\MarkerPatch.3.0.7.zip' }
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

Assert-Hash $MarkerPatchDll '30af0b5ccf50a34d9303d99c04d899c6f6368e522f509ca9d077586542622b55' 1718784
Assert-Hash $SeveredAsi '48874e8ea9ebe97191e7058cc1832eed5d164a62b22c1bd4e875698d2023158f' 124416
Assert-Hash $NodeAsi 'b4433be75687bef192914bc39e8a2c0ac31e3115d098a4c153f25ae722dfc55a' 726528
Assert-Hash $SaveAsi 'add55bbb72932bf70ff177fb71e9bba8358790f5e2c0b8208a1f50cf97014f35' 730112
Assert-Hash $MiningAsi '114042713c525ad46485d5e02b3e52b705f7c5e0dc08b7e94159c0043e62e944' 718848
Assert-Hash $VCRedist 'f0bab33a302b3cdb2e11113760d016f54fd3d2632c65ba7834fac4f0abd7f1a3' 6941536
Assert-Hash $SevenZipExe '94205cfc054dd3bbeb978905bc619a65b3f04e9e73bb2717841110b6f46d962e' 577536
Assert-Hash $SevenZipDll '78bf6b668d75801e3aa180b4ccb16e6d8395fba80dec8a065055bb6f8dfabb82' 1908736
Assert-Hash (Join-Path $root 'assets\save-icons\SaveIcons.tail.bin') '30b1b4dbb505faead10878fceac4f5cbcb70d5d1a99502aad4007aeac4eb1af0' 729022
Assert-Hash (Join-Path $root 'assets\save-icons\SaveIcons.toc.bin') 'bb86263f00028a0f545239a9d28d8829cded328c7f7ed62b1085e18c6df0917c' 2048
Assert-Hash (Join-Path $root 'assets\save-icons\NativeSaveIcons.tail.bin') '847c35a2053387e5057e78629139148194ffb3430ec2c528cd33abff0c177385' 195320
Assert-Hash (Join-Path $root 'assets\save-icons\NativeSaveIcons.toc.bin') 'e31493cf6b8fa0ecb99212ea8f9decb801847f688d33e0f91eeb38ae5efdddc3' 32768
Assert-Hash $MarkerAssets '95781f153738dbc7a67d014476fea730195933812e36e746051279a0218b62e9' 1191931
Assert-Hash $ChineseAssets '990a5bb9590cfc65724e206134a8b6806f5061672241cc0468261b511a5d83a7' 21705707


$outputDir = Split-Path -Parent $OutputExe
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
if ($sources.Count -lt 3) { throw 'Missing installer source files.' }
foreach ($name in @('ChinesePatch.logo.jpg','button.png','Severed.poster.jpg','MarkerPatch_Logo.png','app.ico')) {
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
    '/reference:System.IO.Compression.dll',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Web.Extensions.dll',
    "/resource:$(Join-Path $UiAssetsDir 'app.ico'),DS2_APP_ICON",
    "/resource:$(Join-Path $UiAssetsDir 'ChinesePatch.logo.jpg'),DS2_CHINESE_LOGO",
    "/resource:$(Join-Path $root 'src\Locales\en.json'),DS2_LANG_EN",
    "/resource:$(Join-Path $UiAssetsDir 'button.png'),DS2_LEGACY_BUTTON",
    "/resource:$(Join-Path $UiAssetsDir 'Severed.poster.jpg'),DS2_DLC_POSTER",
    "/resource:$(Join-Path $UiAssetsDir 'MarkerPatch_Logo.png'),DS2_MARKER_LOGO",
    "/resource:$SevenZipExe,DS2_7Z_EXE",
    "/resource:$SevenZipDll,DS2_7Z_DLL",
    "/resource:$ChineseAssets,DS2_CHINESE_ASSETS",
    "/resource:$VCRedist,DS2_VC_REDIST_X86",
    "/resource:$(Join-Path $root 'assets\InstallAudit.references.json'),DS2_AUDIT_REFERENCES",
    "/resource:$MarkerPatchDll,DS2_MARKER_DLL",
    "/resource:$SeveredAsi,DS2_SEVERED_ASI",
    "/resource:$NodeAsi,DS2_NODE_ASI",
    "/resource:$SaveAsi,DS2_SAVE_ASI",
    "/resource:$MiningAsi,DS2_MINING_ASI",
    "/resource:$(Join-Path $root 'assets\save-icons\SaveIcons.tail.bin'),DS2_SAVE_ICON_TAIL",
    "/resource:$(Join-Path $root 'assets\save-icons\SaveIcons.toc.bin'),DS2_SAVE_ICON_TOC",
    "/resource:$(Join-Path $root 'assets\save-icons\NativeSaveIcons.toc.bin'),DS2_NATIVE_ICON_TOC",
    "/resource:$(Join-Path $root 'assets\save-icons\NativeSaveIcons.tail.bin'),DS2_NATIVE_ICON_TAIL",
    "/resource:$MarkerIni,DS2_MARKER_INI",
    "/resource:$MarkerAssets,DS2_MARKER_ASSETS",
    "/resource:$(Join-Path $AssetsRoot 'MarkerAssets.version.json'),DS2_MARKER_ASSET_MANIFEST"
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
