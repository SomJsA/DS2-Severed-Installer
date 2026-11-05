$ErrorActionPreference='Stop'
$out=Join-Path (Split-Path -Parent $PSScriptRoot) 'DS2StartupDiagnostics.exe'
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $csc /nologo /target:exe /platform:x86 /optimize+ /reference:System.Core.dll /reference:System.Management.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/win32manifest:$PSScriptRoot\app.manifest" "/out:$out" "$PSScriptRoot\DS2StartupDiagnostics.cs"
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
