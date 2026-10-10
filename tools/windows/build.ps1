<#
.SYNOPSIS
Build the MCC solution and initialize ConsoleInteractive if needed.
.EXAMPLE
.\tools\windows\build.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$NoRestore,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$DotnetArguments = @()
)
. (Join-Path $PSScriptRoot 'Common.ps1')
Initialize-MccConsoleDependency
$arguments = @('build', (Join-Path $script:MccRepository 'Mcc.slnx'), '-c', $Configuration,
    '-p:ShouldUnsetParentConfigurationAndPlatform=false')
if ($NoRestore) { $arguments += '--no-restore' }
Invoke-MccDotnet -Arguments ($arguments + $DotnetArguments)
