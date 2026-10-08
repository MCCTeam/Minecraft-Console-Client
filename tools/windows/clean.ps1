<#
.SYNOPSIS
Clean MCC build output without deleting runtime data or release archives.
.EXAMPLE
.\tools\windows\clean.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$DotnetArguments = @()
)
. (Join-Path $PSScriptRoot 'Common.ps1')
Invoke-MccDotnet -Arguments (@('clean', (Join-Path $script:MccRepository 'Mcc.slnx'),
    '-c', $Configuration, '-p:ShouldUnsetParentConfigurationAndPlatform=false') + $DotnetArguments)
