# Install MCC

MCC runs on Windows, Linux, and macOS. Use a terminal that supports UTF-8. The classic console is a scrolling text interface. The TUI is a full-screen terminal interface.

## Choose an installation method

| Method | Use it when |
| --- | --- |
| Published distribution | You want to run an available MCC 2.0 release without building it |
| Source build | You want the development code or no MCC 2.0 release exists yet |
| [Docker Compose](../deployment/docker.md) | You want a container with persistent configuration and plugin data |

MCC 2.0 is still in development. Check the release description before downloading an archive. Older MCC archives contain a different client. The new installer requires an MCC 2.0 release asset and cannot install an older release.

## Run a published distribution

1. Open the [MCC releases page](https://github.com/MCCTeam/Minecraft-Console-Client/releases).
2. Select a release that explicitly contains MCC 2.0.
3. Download the archive for your operating system and architecture.
4. Optional, but recommended: [check the download checksum](#optional-check-the-download-checksum).
5. Extract the complete archive into a writable folder.
6. Open a terminal in that folder.
7. Print the startup help.

Release archives use `Mcc-<tag>-<RID>.tar.gz` on Unix and `Mcc-<tag>-<RID>.zip` on Windows. A RID is a .NET runtime identifier, such as `linux-x64` or `win-arm64`.

Linux or macOS:

```bash
chmod +x ./Mcc.Cli
./Mcc.Cli --help
```

Windows PowerShell:

```powershell
.\Mcc.Cli.exe --help
```

The installer creates an `mcc` launcher. Extracted archives use `Mcc.Cli` directly. Keep the archive's DLLs beside the executable. MCC needs accessible managed assemblies to compile source plugins. Copy the complete distribution when you move it.

A self-contained distribution includes the .NET runtime. A framework-dependent build requires the [.NET 10 and ASP.NET Core runtimes](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

## Optional: check the download checksum

This check is optional, but recommended. It checks that your download matches the release archive. The installer performs this check automatically.

Download `SHA256SUMS` from the same release. Open it in a text editor. Find the row for your downloaded archive. Compare its 64-character hexadecimal value with the command output below.

Linux:

```bash
sha256sum Mcc-v2.0.0-linux-x64.tar.gz
```

macOS:

```bash
shasum -a 256 Mcc-v2.0.0-osx-arm64.tar.gz
```

Windows PowerShell:

```powershell
Get-FileHash -Algorithm SHA256 .\Mcc-v2.0.0-win-x64.zip
```

These filenames illustrate the release format. Replace the version and target with your actual archive name. Check that every hexadecimal character matches. Letter case does not matter.

Do not extract an archive with a different checksum. Download it again from the same release and check it again.

## Build the development branch

The commands below require a published `feat/mcc-2.0` branch. Until it is published, use the provided local checkout instead of cloning and switching.

You need [Git](https://git-scm.com/downloads/) and the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) version `10.0.401`, as selected by `global.json`. MCC restores DMCBK and UMPK through NuGet. You do not need their source repositories.

On Windows, we recommend [WSL for development](../contributing/windows.md#recommended-wsl). Install Git and the SDK inside WSL for that workflow. Native PowerShell remains an alternative, with helpers under `tools/windows/`.

Run the checkout commands in your chosen development terminal:

```bash
git clone https://github.com/MCCTeam/Minecraft-Console-Client.git
cd Minecraft-Console-Client
git switch feat/mcc-2.0
git submodule update --init ConsoleInteractive
```

Linux, macOS, or Windows with WSL:

```bash
source tools/mcc-env.sh
mcc-build
mcc-run --help
mcc-run --configurations "$HOME/mcc-data/configurations"
```

The helper starts MCC in a temporary runtime folder by default. An absolute configuration path gives you persistent settings without creating them in the source checkout.

Native Windows PowerShell:

```powershell
.\tools\windows\build.ps1
.\tools\windows\test.ps1
.\tools\windows\run.ps1 -ClientArguments @('--help')
.\tools\windows\run.ps1 -RunDirectory "$env:LOCALAPPDATA\MCC\development"
```

The run helper keeps configuration outside the source checkout. Add `-Tui` for the terminal UI. See [Windows development](../contributing/windows.md#native-powershell-alternative) for publishing, cleaning and script options.

## Publish your own distribution

```bash
source tools/mcc-env.sh
mcc-publish --rid linux-x64 -- -o /tmp/mcc-published
```

For native Windows PowerShell, use:

```powershell
.\tools\windows\publish.ps1 -Rid win-x64 -OutputDirectory "$env:TEMP\mcc-published"
```

Choose the supported target for your system. Common targets include `win-x64`, `win-x86`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-musl-x64`, `osx-x64`, and `osx-arm64`.

The helper creates a self-contained distribution with `PublishSingleFile=false`. The files remain accessible for source plugin compilation. This operation does not publish anything online.

[Start your first session](first-session.md).
