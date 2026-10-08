<#
.SYNOPSIS
Build and run the MCC CLI tests.
.EXAMPLE
.\tools\windows\test.ps1 -Filter 'FullyQualifiedName~LocalizationTests'
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$NoBuild,
    [switch]$NoRestore,
    [string]$Filter,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$DotnetArguments = @()
)
. (Join-Path $PSScriptRoot 'Common.ps1')
Initialize-MccConsoleDependency
$arguments = @('test', (Join-Path $script:MccRepository 'src/Mcc.Cli.Tests/Mcc.Cli.Tests.csproj'),
    '-c', $Configuration, '-p:ShouldUnsetParentConfigurationAndPlatform=false')
if ($NoBuild) { $arguments += '--no-build' }
if ($NoRestore) { $arguments += '--no-restore' }
if ($Filter) { $arguments += @('--filter', $Filter) }
Invoke-MccDotnet -Arguments ($arguments + $DotnetArguments)
