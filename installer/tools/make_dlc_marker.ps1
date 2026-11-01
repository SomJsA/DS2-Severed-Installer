[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Dat2,
    [Parameter(Mandatory = $true)][string]$Poster,
    [Parameter(Mandatory = $true)][string]$Output,
    [Parameter(Mandatory = $true)][string]$SevenZipExe,
    [string]$PackageVersion = '1.1.0',
    [string]$Variant = 'base-en-v1',
    [string]$MinimumInstallerVersion = '1.0.9.0',
    [string]$ExpectedPayloadSha256 = '81a145da647660d34a6fcb15ea3a8709c74855cf1beef23fc668a3ae42eb6ea0',
    [long]$ExpectedPayloadBytes = 3187759104
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($PackageVersion -notmatch '^\d+\.\d+(\.\d+){0,2}$' -or $MinimumInstallerVersion -notmatch '^\d+\.\d+(\.\d+){0,2}$') { throw 'Invalid numeric version.' }
[void][version]::Parse($PackageVersion)
[void][version]::Parse($MinimumInstallerVersion)
if ($Variant -notmatch '^[a-z0-9][a-z0-9._-]{0,63}$') { throw 'Invalid base variant.' }
if ($ExpectedPayloadSha256 -notmatch '^[0-9a-fA-F]{64}$' -or $ExpectedPayloadBytes -lt 16 -or $ExpectedPayloadBytes -gt [uint32]::MaxValue) { throw 'Invalid expected payload identity.' }
$payload = Get-Item -LiteralPath $Dat2
$payloadHash = (Get-FileHash -LiteralPath $payload.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
if ($payload.Length -ne $ExpectedPayloadBytes -or $payloadHash -ne $ExpectedPayloadSha256.ToLowerInvariant()) { throw 'Input is not the independently verified English payload.' }
$posterFile = Get-Item -LiteralPath $Poster
if ($posterFile.Length -lt 1 -or $posterFile.Length -gt 8MB) { throw 'Poster size must be between 1 byte and 8 MiB.' }
$posterHash = (Get-FileHash -LiteralPath $posterFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$sevenZipPath = (Get-Item -LiteralPath $SevenZipExe).FullName
$outputPath = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $outputPath) { throw "Output already exists: $outputPath" }
$outputDir = [IO.Path]::GetDirectoryName($outputPath)
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$staging = Join-Path $outputDir ('.base-en-stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null
$metadata = [ordered]@{
    schema_version = 1
    protocol_version = 1
    package_id = 'ds2-severed-base-en'
    package_version = $PackageVersion
    minimum_installer_version = $MinimumInstallerVersion
    variant = $Variant
    language = 'en'
    localized = $false
    payload_file = 'DS2DAT2.DAT'
    payload_bytes = $payload.Length
    payload_sha256 = $payloadHash
    poster = [ordered]@{file = 'Severed.poster.jpg'; bytes = $posterFile.Length; sha256 = $posterHash}
}
# Copy before setting timestamps: never mutate the original input through a hard link.
Copy-Item -LiteralPath $payload.FullName -Destination (Join-Path $staging 'DS2DAT2.DAT')
Copy-Item -LiteralPath $posterFile.FullName -Destination (Join-Path $staging 'Severed.poster.jpg')
[IO.File]::WriteAllText((Join-Path $staging 'DLC.version.json'), ($metadata | ConvertTo-Json -Depth 5) + "`n", [Text.UTF8Encoding]::new($false))
$stamp = [DateTimeOffset]::FromUnixTimeSeconds(1793491200).UtcDateTime
foreach ($name in @('DS2DAT2.DAT','DLC.version.json','Severed.poster.jpg')) {
    $p = Join-Path $staging $name
    [IO.File]::SetAttributes($p, [IO.FileAttributes]::Archive)
    [IO.File]::SetLastWriteTimeUtc($p, $stamp)
}
Push-Location -LiteralPath $staging
try {
    & $sevenZipPath a -t7z -m0=lzma2:d256m -mx=9 -mmt=8 -ms=off -mtc=off -mta=off $outputPath 'DS2DAT2.DAT' 'DLC.version.json' 'Severed.poster.jpg'
    if ($LASTEXITCODE -ne 0) { throw "7-Zip failed: $LASTEXITCODE" }
} finally { Pop-Location }
& $sevenZipPath t $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Archive test failed.' }
[pscustomobject]@{
    Path = $outputPath
    Bytes = (Get-Item -LiteralPath $outputPath).Length
    Sha256 = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Protocol = 1
    PackageVersion = $PackageVersion
    Variant = $Variant
    StagingRetained = $staging
}
# Keep the exact staging path for review and later scoped cleanup.
