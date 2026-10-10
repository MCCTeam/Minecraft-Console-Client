# Build MCC on Windows

We recommend Windows Subsystem for Linux (WSL) for development. It lets you use the same Bash helpers as Linux developers.

Native PowerShell development is also supported. Choose one workflow for each checkout so its generated files remain separate.

## Recommended: WSL

### Install WSL

1. Open PowerShell as Administrator.
2. Run the WSL installation command.

   ```powershell
   wsl --install
   ```

3. Restart Windows if the installer requests it.
4. Open the Ubuntu terminal.
5. Complete the Linux username and password prompts.

See Microsoft's [WSL installation instructions](https://learn.microsoft.com/en-us/windows/wsl/install) for supported Windows versions and other Linux distributions.

### Prepare the Linux tools

Install [Git](https://git-scm.com/downloads/linux) and the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) inside WSL. A Windows SDK installation does not replace the Linux SDK.

MCC selects SDK `10.0.401` through `global.json`. Follow the [.NET Ubuntu installation guide](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install) for your Ubuntu version.

Check the tools inside the Ubuntu terminal:

```bash
git --version
dotnet --version
```

Keep the checkout in WSL's Linux filesystem, such as `~/projects/`. This follows Microsoft's [filesystem guidance](https://learn.microsoft.com/en-us/windows/wsl/filesystems).

Use a separate Windows checkout if you also build natively in PowerShell. Do not share one `bin/` or `obj/` tree between those environments.

### Build and run

Clone the `feat/mcc-2.0` branch with both submodules:

```bash
mkdir -p ~/projects
cd ~/projects
git clone --branch feat/mcc-2.0 --recurse-submodules https://github.com/MCCTeam/Minecraft-Console-Client.git
cd Minecraft-Console-Client
source tools/mcc-env.sh
mcc-build
mcc-test
mcc-run --help
```

To keep your runtime files:

```bash
export MCC_RUN_ROOT="$HOME/mcc-data"
mcc-run
```

The client running here is a Linux application inside WSL. To create a native Windows distribution, publish a Windows target:

```bash
mcc-publish --rid win-x64 -- -o /tmp/mcc-windows
```

Copy the complete output folder to Windows before running `Mcc.Cli.exe` from PowerShell. A cross-build alone does not prove native execution works.

## Native PowerShell alternative

Install [Git for Windows](https://git-scm.com/downloads/win) and the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) on Windows.

Before cloning with native Git, prepare [symbolic-link support](agent-skills.md#use-skills-in-this-checkout) for the skill discovery folders.

Open PowerShell in your Windows checkout. The helpers under `tools/windows/` locate the repository through their own paths.

### Build and test

```powershell
.\tools\windows\build.ps1
.\tools\windows\test.ps1
```

The build helper initializes ConsoleInteractive if its project is missing. Both helpers restore NuGet packages normally.

To select a test group:

```powershell
.\tools\windows\test.ps1 -Filter 'FullyQualifiedName~LocalizationTests'
```

Use `-NoRestore` after a successful restore. Use `-NoBuild` for tests only after building the same configuration.

### Run the classic client or TUI

```powershell
.\tools\windows\run.ps1 -ClientArguments @('--help')
.\tools\windows\run.ps1 -RunDirectory "$env:LOCALAPPDATA\MCC\development"
.\tools\windows\run.ps1 -Tui -RunDirectory "$env:LOCALAPPDATA\MCC\development"
```

The run helper uses a temporary directory when you omit `-RunDirectory`. It never creates default runtime files in the source root.

To use a particular configuration folder:

```powershell
.\tools\windows\run.ps1 -ClientArguments @('--configurations', 'C:\mcc-data\configurations')
```

Paths in `-ClientArguments` should be absolute because the helper changes its working directory. Array elements preserve paths with spaces.

Offline Beacon modes keep their first argument:

```powershell
.\tools\windows\run.ps1 -ClientArguments @('lint', 'C:\mcc-data\scripts\hello.bcn')
.\tools\windows\run.ps1 -ClientArguments @('run', 'C:\mcc-data\scripts\hello.bcn')
```

`run.ps1 -Build` builds before launching. Select `-Configuration Debug` consistently on build, test and run helpers for a Debug session.

### Publish and clean

```powershell
.\tools\windows\publish.ps1 -Rid win-x64
.\tools\windows\publish.ps1 -Rid win-arm64 -OutputDirectory "$env:TEMP\mcc-arm64"
.\tools\windows\clean.ps1
```

Default publish output is `artifacts/publish/<RID>/`. It retains managed assemblies for source plugins. Nothing is uploaded.

`clean.ps1` removes build output. It does not delete accounts, plugin data or release archives.

### If .NET reports a CET startup failure

Older Windows installations can fail before the compiler or MCC starts. The error can say:

```text
Your Windows doesn't fully support CET. Please install all available Windows updates.
```

CET is hardware-enforced stack protection. .NET enables CET compatibility for executable hosts by default. See [Microsoft's CET compatibility notes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/interop/9.0/cet-support).

1. Open Windows Settings.
2. Open Windows Update.
3. Install the available Windows updates.
4. Restart if Windows requests it.
5. Run the build or client again.

This startup failure does not indicate invalid PowerShell syntax. Disabling the shared compiler server alone does not fix it.

### If PowerShell blocks a script

Check your current policy:

```powershell
Get-ExecutionPolicy -List
```

Follow Microsoft's [PowerShell execution-policy guidance](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies). Follow your organization's policy on managed computers. The MCC helpers do not change it.

An error from Git or .NET stops the helper. Read that tool's output, correct the cause, and run the helper again.

[Installation](../getting-started/installation.md) · [Your first session](../getting-started/first-session.md)
