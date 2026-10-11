# MCC 2.0 Early Access Build 13

> [!IMPORTANT]
> This is an early access, preview build of the new upcoming Minecraft Console Client 2.0.
> Support is provided on our Discord server, do not open issues here

> [!IMPORTANT]
> The old configuration, scripts, and Chat Bots do not work in MCC 2.0 as of now. Everything is brand new.
> The release version will port configurations, but won't port ChatBots or scripts, for this I'll provide separate AI skills that you could use to convert old Scripts and Chat Bots into Beacon Scripts and Plugins.

> [!NOTE]
> This early-access build comes from [`feat/mcc-2.0`](https://github.com/MCCTeam/Minecraft-Console-Client/tree/feat/mcc-2.0/)
> **The documentation is located in the repo**: [here](https://github.com/MCCTeam/Minecraft-Console-Client/tree/feat/mcc-2.0/docs)
> You can use `/help` and `/man` for help and manuals for commands, features, and plugins.
> You can use `Mcc.Cli --help` for the help on the CLI usage and parameters

## Start MCC

1. Download the archive for your operating system and architecture from this release.
2. Extract the archive. It contains one MCC executable and the license file.
3. Run `Mcc.Cli.exe` on Windows or `./Mcc.Cli` on Linux and macOS.
4. Follow the login prompts. Use `--help` for command-line options, or `/help` and `/man` inside MCC.

MCC creates its configuration files on first startup. MCC 2.0 uses TOML configuration, Beacon scripts and DMCBK plugins; legacy INI files, scripts and ChatBots need migration.

## Changes

- Update DMCBK to `0.1.0-preview.8` and UMPK to `0.9.0-beta.7`.
- Beacon timer polling reuses dispatch plans and avoids allocations when no timer is due. Reloads and removals refresh the plans, while active ticks retain their snapshots across awaits.
- UMPK reduces packet-dispatch allocations and loads the requested protocol descriptor during ordinary version lookup, reducing memory use.
- Plugin authoring examples and the bundled source-plugin smoke test use the updated library versions.

## Downloads

| Platform | Archives |
| --- | --- |
| Windows | x86 (32-bit), x64, ARM64 |
| macOS | x64, ARM64 |
| Linux with glibc | x64, ARM32, ARM64 |
| Linux with musl, including Alpine | x64, ARM32, ARM64 |

Choose the Linux variant that matches your distribution. Windows ARM32, Linux x86 and 32-bit macOS are outside [.NET 10's supported architectures](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md).

`SHA256SUMS` contains the digest for every archive. The build uses DMCBK `0.1.0-preview.8` and UMPK `0.9.0-beta.7`.

For the MCC 2.0 Python installer, select this preview explicitly:

```sh
python3 tools/install_mcc.py --version v2.0.0-early-access.13
```
