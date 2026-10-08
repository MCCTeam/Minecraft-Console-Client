<#
.SYNOPSIS
Create a local, self-contained MCC distribution. Nothing is uploaded.
.EXAMPLE
.\tools\windows\publish.ps1 -Rid win-x64 -OutputDirectory "$env:TEMP\mcc-publish"
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x86','win-x64','win-arm64','linux-x64','linux-arm64','linux-arm',
        'linux-musl-x64','linux-musl-arm64','linux-musl-arm','osx-x64','osx-arm64')]
    [string]$Rid = ('win-' + [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()),
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [string]$OutputDirectory,
    [switch]$NoRestore,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$DotnetArguments = @()
)
. (Join-Path $PSScriptRoot 'Common.ps1')
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $script:MccRepository "artifacts/publish/$Rid" }
$output = Resolve-MccPath $OutputDirectory
Initialize-MccConsoleDependency
$arguments = @('publish', (Join-Path $script:MccRepository 'src/Mcc.Cli/Mcc.Cli.csproj'),
    '-c', $Configuration, '-r', $Rid, '--self-contained', 'true', '-o', $output,
    '-p:UseAppHost=true', '-p:PublishSingleFile=false', '-p:DebugType=Embedded',
    '-p:ShouldUnsetParentConfigurationAndPlatform=false')
if ($NoRestore) { $arguments += '--no-restore' }
Invoke-MccDotnet -Arguments ($arguments + $DotnetArguments)
Write-Host "Local distribution: $output"
