# MCC 2.0 installer for Windows. Save this file, then run it with PowerShell.
param(
    [string]$Version = 'latest',
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'MCC'),
    [ValidateSet('win-x86','win-x64','win-arm64')][string]$Rid,
    [switch]$PrintRid
)
$ErrorActionPreference = 'Stop'
if (-not $Rid) {
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()
    if ($arch -notin @('x86','x64','arm64')) { throw "Unsupported process architecture: $arch" }
    $Rid = "win-$arch"
}
if ($PrintRid) { Write-Output $Rid; exit 0 }
$endpoint = if ($Version -eq 'latest') { 'latest' } else { 'tags/' + [Uri]::EscapeDataString($Version) }
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/MCCTeam/Minecraft-Console-Client/releases/$endpoint"
$tag = $release.tag_name
if ($tag -notmatch '^v?2\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') { throw 'The selected release is not MCC 2.0. Build from source until an MCC 2.0 release is published.' }
$name = "Mcc-$tag-$Rid.zip"
$asset = @($release.assets | Where-Object name -eq $name)
$checksum = @($release.assets | Where-Object name -eq 'SHA256SUMS')
if ($asset.Count -ne 1 -or $checksum.Count -ne 1) { throw "Release must contain $name and SHA256SUMS." }
$Destination = [IO.Path]::GetFullPath($Destination)
$target = Join-Path $Destination "releases/$tag/$Rid"
if (Test-Path $target) { throw "Already installed: $target. Select another version or installation directory." }
$scratch = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $scratch | Out-Null
try {
    $archive = Join-Path $scratch $name
    $sumFile = Join-Path $scratch 'SHA256SUMS'
    Invoke-WebRequest -Uri $asset[0].browser_download_url -OutFile $archive -UseBasicParsing
    Invoke-WebRequest -Uri $checksum[0].browser_download_url -OutFile $sumFile -UseBasicParsing
    $line = @(Get-Content $sumFile | Where-Object { $_ -match ('^[0-9a-fA-F]{64}\s+\*?' + [regex]::Escape($name) + '$') })
    if ($line.Count -ne 1) { throw "No unique checksum for $name." }
    $expected = ($line[0] -split '\s+')[0]
    if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne $expected) { throw 'Archive checksum does not match.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $package = [IO.Compression.ZipFile]::OpenRead($archive)
    $stage = Join-Path $scratch 'payload'
    New-Item -ItemType Directory -Path $stage | Out-Null
    try {
        foreach ($entry in $package.Entries) {
            $entryName = $entry.FullName.Replace('\','/')
            if ($entryName.StartsWith('/') -or $entryName.Contains(':') -or ($entryName.Split('/') -contains '..')) { throw "Unsafe archive path: $entryName" }
            if (($entry.ExternalAttributes -shr 16 -band 61440) -eq 40960) { throw 'Archive links are not allowed.' }
        }
    } finally { $package.Dispose() }
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $stage)
    if (-not (Test-Path (Join-Path $stage 'Mcc.Cli.exe')) -or -not (Test-Path (Join-Path $stage 'Mcc.Cli.dll'))) { throw 'Archive does not contain MCC executable and assemblies.' }
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Move-Item $stage $target
    $launcher = Join-Path $Destination 'mcc.cmd'
    $binary = "`"%~dp0releases\$tag\$Rid\Mcc.Cli.exe`""
    $content = "@echo off`r`ncd /d `"%~dp0`"`r`n"
    foreach ($mode in @('run','lint','format','--validate-plugin','--validate-marketplace','--help','--help-short')) {
        $content += "if `"%~1`"==`"$mode`" goto tool`r`n"
    }
    $content += "$binary --configurations `"%~dp0configurations`" %*`r`nexit /b %errorlevel%`r`n:tool`r`n$binary %*`r`n"
    [IO.File]::WriteAllText($launcher, $content, [Text.Encoding]::ASCII)
    Write-Output "Installed $tag for $Rid."
    Write-Output "Run: $launcher --help"
    Write-Output 'Existing configuration, plugins and scripts were preserved.'
} finally { Remove-Item -Recurse -Force $scratch }
