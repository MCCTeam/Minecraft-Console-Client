# Windows development scripts

We recommend WSL for the shared Bash development workflow. These PowerShell scripts also support a native Windows checkout.

Install [Git](https://git-scm.com/downloads/) and the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) version selected by `global.json`.

Run these examples from the repository root:

```powershell
.\tools\windows\build.ps1
.\tools\windows\test.ps1
.\tools\windows\run.ps1 -ClientArguments @('--help')
.\tools\windows\run.ps1 -Tui
.\tools\windows\publish.ps1 -Rid win-x64
.\tools\windows\clean.ps1
```

| Script | Behavior |
| --- | --- |
| `build.ps1` | Builds the solution. Initializes ConsoleInteractive if missing. |
| `test.ps1` | Builds and runs CLI tests. Accepts `-Filter`, `-NoBuild` and `-NoRestore`. |
| `publish.ps1` | Creates a complete local distribution under `artifacts/publish/<RID>/`, or `-OutputDirectory`. |
| `run.ps1` | Runs the built DLL from a temporary directory. Accepts `-Build`, `-Tui`, `-RunDirectory` and `-ClientArguments`. |
| `clean.ps1` | Cleans build output without removing account or plugin data. |
| `Common.ps1` | Shared path, dependency and exit-code handling. Dot-source it only from another helper. |

All scripts accept `-Configuration Debug` or `Release`. Build, test, publish and clean accept extra options through `-DotnetArguments`.

```powershell
.\tools\windows\test.ps1 -Filter 'FullyQualifiedName~LocalizationTests'
.\tools\windows\build.ps1 -DotnetArguments @('-m:1')
.\tools\windows\publish.ps1 -Rid win-arm64 -OutputDirectory "$env:TEMP\mcc-arm64"
```

To keep runtime files in a stable directory:

```powershell
.\tools\windows\run.ps1 -RunDirectory "$env:LOCALAPPDATA\MCC\development"
```

Use absolute configuration and script paths with `-ClientArguments`. The run helper changes its working directory and preserves the first argument for Beacon `run`, `lint` and `format` modes.

```powershell
.\tools\windows\run.ps1 -ClientArguments @('lint', 'C:\mcc-data\scripts\hello.bcn')
```

Scripts propagate command failures and restore the caller's working directory. A publish operation does not upload or create a release.

See the [Windows development guide](../../docs/contributing/windows.md) for WSL setup and native PowerShell usage.
