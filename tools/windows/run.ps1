<#
.SYNOPSIS
Run the built MCC CLI outside the source checkout, with optional TUI mode.
.EXAMPLE
.\tools\windows\run.ps1 -Build -ClientArguments @('--help')
.EXAMPLE
.\tools\windows\run.ps1 -Tui -RunDirectory "$env:LOCALAPPDATA\MCC\development"
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$Build,
    [switch]$Tui,
    [string]$RunDirectory = (Join-Path ([IO.Path]::GetTempPath()) 'mcc-2.0-development'),
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$ClientArguments = @()
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$runtime = Resolve-MccPath $RunDirectory
if ($runtime.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) -eq $script:MccRepository) {
    throw 'Select a runtime directory outside the repository root.'
}
if ($Build) { & (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration }
$buildRoot = [Environment]::GetEnvironmentVariable('MCC_BUILD_ROOT')
$client = if ($buildRoot) {
    Join-Path $buildRoot "Mcc.Cli/bin/$Configuration/net10.0/Mcc.Cli.dll"
} else {
    Join-Path $script:MccRepository "src/Mcc.Cli/bin/$Configuration/net10.0/Mcc.Cli.dll"
}
if (-not (Test-Path $client)) { throw 'Build MCC first, or use run.ps1 -Build.' }
if ($Tui -and $ClientArguments.Count -gt 0 -and $ClientArguments[0] -in @('run','lint','format','--validate-plugin','--validate-marketplace')) {
    throw 'TUI mode is for an interactive client, not an offline tool command.'
}
$arguments = @($client) + $ClientArguments
if ($Tui) { $arguments += '--console.General.ConsoleMode=tui' }
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
Invoke-MccDotnet -Arguments $arguments -WorkingDirectory $runtime
