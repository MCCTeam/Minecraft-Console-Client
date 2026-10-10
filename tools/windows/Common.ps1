# Shared MCC development helpers. Dot-source from the scripts in this directory.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:MccRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not (Test-Path (Join-Path $script:MccRepository 'Mcc.slnx'))) {
    throw 'Mcc.slnx was not found. Keep tools/windows inside the MCC repository.'
}

function Invoke-MccDotnet {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$WorkingDirectory = $script:MccRepository
    )
    $dotnet = (Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
    Push-Location $WorkingDirectory
    try {
        # Windows PowerShell 5.1 can turn redirected stderr into a terminating error.
        # Preserve the native exit code, then report failure through the helper.
        $previousErrorAction = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            & $dotnet @Arguments
            $exitCode = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = $previousErrorAction
        }
        if ($exitCode -ne 0) {
            throw "dotnet $($Arguments[0]) failed with exit code $exitCode."
        }
    } finally {
        Pop-Location
    }
}

function Initialize-MccConsoleDependency {
    $project = Join-Path $script:MccRepository 'ConsoleInteractive/ConsoleInteractive/ConsoleInteractive/ConsoleInteractive.csproj'
    if (Test-Path $project) { return }
    $git = (Get-Command git -CommandType Application -ErrorAction Stop).Source
    Push-Location $script:MccRepository
    try {
        & $git submodule update --init ConsoleInteractive
        if ($LASTEXITCODE -ne 0) { throw 'ConsoleInteractive initialization failed.' }
        if (-not (Test-Path $project)) { throw 'ConsoleInteractive project is still missing.' }
    } finally {
        Pop-Location
    }
}

function Resolve-MccPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    # Resolve relative paths before another helper changes the working directory.
    $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}
