# MCC 2.0 Early Access Build 12

This early-access build comes from `feat/mcc-2.0`. Source and [documentation](https://github.com/MCCTeam/Minecraft-Console-Client/tree/feat/mcc-2.0/docs) are available in the repository.

## Start MCC

1. Download the archive for your operating system and architecture from this release.
2. Extract all files into one folder. Keep the DLLs and language folders beside the executable.
3. Run `Mcc.Cli.exe` on Windows or `./Mcc.Cli` on Linux and macOS.
4. Follow the login prompts. Use `--help` for command-line options, or `/help` and `/man` inside MCC.

The .NET runtime is included. MCC creates its configuration files on first startup. MCC 2.0 uses TOML configuration, Beacon scripts and DMCBK plugins; legacy INI files, scripts and ChatBots need migration.

## Changes

- The official [Marketplace](https://github.com/MCCTeam/Marketplace) is registered by default, with 25 published plugins. Try `/plugins search fishing in official` and `/plugins install auto-fishing@official`. Automatic updates start off, and existing marketplace choices are preserved.
- Offline servers can request encryption without requiring session-server authentication, including servers using OfflineEncryptor.
- ARM32 uses a managed AES implementation when the platform cannot provide the required encryption mode.
- Microsoft authentication refreshes cached tokens automatically.
- Plugin loading fixes keep private AI SDK dependencies with their plugins and share the host's ASP.NET Core assemblies correctly. The MCP server and LLM plugins load in a clean MCC distribution.
- The documentation now covers MCC 2.0 configuration, commands, Beacon, plugins and marketplaces.

## Downloads and validation

| Platform | Archives |
| --- | --- |
| Windows | x86 (32-bit), x64, ARM64 |
| macOS | x64, ARM64 |
| Linux with glibc | x64, ARM32, ARM64 |
| Linux with musl, including Alpine | x64, ARM32, ARM64 |

Choose the Linux variant that matches your distribution. Windows ARM32, Linux x86 and 32-bit macOS are outside [.NET 10's supported architectures](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md).

Release CI runs all 918 CLI tests and checks extracted archive startup on seven native targets. Linux ARM32 and the three musl targets are cross-built; this does not establish execution on those systems. Documentation, localization and installer checks also pass before publication. `SHA256SUMS` contains the digest for every archive.

The published Marketplace was tested in clean-built MCC for all 25 plugin installations, dependency resolution, restart, lifecycle and data preservation. A separate offline Minecraft 1.21.11 run verified ChatLog callbacks and MCP authentication. This does not claim live gameplay coverage for every plugin.

The build uses DMCBK `0.1.0-preview.7`, UMPK `0.9.0-beta.6` and .NET SDK `10.0.401`. Complete distribution archives preserve the assemblies needed for source-plugin compilation.

For the MCC 2.0 Python installer, select this preview explicitly:

```sh
python3 tools/install_mcc.py --version v2.0.0-early-access.12
```
