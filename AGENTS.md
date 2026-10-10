# AGENTS.md

## Project and ownership

MCC 2.0 is a cross-platform console and terminal UI client for Minecraft Java Edition. It hosts DMCBK, which uses UMPK for the protocol and game engine.

- Active solution: `Mcc.slnx`.
- Host implementation: `src/Mcc.Cli/`. Host tests: `src/Mcc.Cli.Tests/`.
- DMCBK packages: `0.1.0-preview.7`. UMPK packages: `0.9.0-beta.6`.
- Restore libraries through NuGet. Do not add sibling project references or copy DMCBK, UMPK or official plugin implementations into this repository.
- The legacy client, GUI, debug tools, Docker setup and old skills were removed. Current Docker files and skills target MCC 2.0.
- MCC code uses MIT. Preserve external dependencies' licenses and attribution.

## Local skills

Initialize the shared skills with `git submodule update --init MCC-Skills`.

| Skill | Use for |
| --- | --- |
| `.skills/beacon-scripting/SKILL.md` | Beacon syntax, examples and script checks |
| `.skills/dmcbk-plugin-authoring/SKILL.md` | Plugin contracts, lifecycle, packaging and tests |
| `.skills/dmcbk-marketplace-authoring/SKILL.md` | Schema-2 catalogues, assets and release procedures |
| `.skills/csharp-best-practices/SKILL.md` | C# implementation, naming and async review |
| `.skills/csharp-solid-principles/SKILL.md` | Design, refactoring and API boundaries |
| `.skills/dotnet-performance-profiling-and-optimization/SKILL.md` | Measured performance diagnosis |
| `.skills/dotnet-security-review/SKILL.md` | Security and dependency review |
| `.skills/asd-ste100/SKILL.md` | Clear procedures, diagnostics and agent instructions |
| `.skills/humanizer/SKILL.md` | Natural, direct prose |

Seven `.skills` entries link to the pinned [MCC Skills](https://github.com/MCCTeam/MCC-Skills) submodule under `MCC-Skills/skills`. ASD-STE100 and Humanizer remain local. `.claude/skills`, `.agents/skills`, `.codex/skill` and `.codex/skills` link to `.skills`.

Read the relevant `SKILL.md` before applying a skill. Resolve relative references from its directory. Edit shared skills in MCC Skills rather than creating another local copy.

## Build, run and test

Use .NET SDK `10.0.401` from `global.json`.

```bash
git submodule update --init ConsoleInteractive MCC-Skills
source tools/mcc-env.sh
mcc-build
mcc-test
python3 tools/localization/generate_strings.py --check
mcc-publish --rid linux-x64
```

Windows development should use WSL when practical. Native PowerShell helpers live in `tools/windows/`: build, test, run, publish and clean. Read that directory's README. These helpers restore the caller's directory and propagate failures.

`mcc-publish` creates a local distribution without uploading it. The development helper uses `PublishSingleFile=false`. Release builds use `PublishSingleFile=true` with `IncludeNativeLibrariesForSelfExtract=true` and `IncludeAllContentForSelfExtract=true` so source plugins can access extracted compilation references. Keep trimming disabled for plugin hosts.

`mcc-run` starts the new CLI. `mcc-tui` starts its TUI. Both use a temporary working directory by default. Set `MCC_RUN_ROOT` to select a persistent directory. Pass absolute paths for configurations and scripts because these helpers change the working directory.

```bash
mcc-run --configurations /absolute/path/to/configurations
mcc-tui --configurations /absolute/path/to/configurations
mcc-run --help
```

Set `MCC_BUILD_MODE=tmpfs` to route build output outside the checkout. Use `NuGet.Local.Config` only for unpublished packages in `artifacts/packages`; set `RestoreConfigFile` to its absolute path. Use a fresh `NUGET_PACKAGES` cache when replacing a package at the same version.

`.github/workflows/mcc.yml` builds and tests the CLI on Linux, Windows and macOS. Documentation and artifact-only translation validation use docs.yml and translations.yml. Neither workflow creates commits or pushes translations.

## Architecture and source layout

Feature folders and namespaces must match under `Mcc.Cli`; the enclosing `src` folder is not part of the namespace. See [src/README.md](src/README.md) for architecture and [src/Mcc.Cli/README.md](src/Mcc.Cli/README.md) for the implementation map.

| Path | Responsibility |
| --- | --- |
| `Program.cs`, `Startup/` | Composition, arguments, help, guided login and idle startup. |
| `Hosting/` | Host state, lifetime and classic DMCBK adapters. |
| `Configuration/` | Console settings, TOML overrides and environment files. |
| `Input/`, `Commands/`, `Logging/` | File input, host commands and logging. |
| `Presentation/` | Console chat, ANSI, glyphs, maps, status and Markdown. |
| `BeaconTooling/`, `Plugins/` | Beacon CLI adapters, plugin prompts and validation. |
| `Diagnostics/` | Session reports and packet capture. |
| `Localization/`, `Resources/` | MCC-owned translations and accessors. |
| `Tui/` | Terminal UI adapters, presentation and feature views. |
| `ConsoleInteractive/` | External submodule. Do not modify its source. |

DMCBK owns client lifecycle, commands, typed configuration, Beacon, plugins and marketplace transactions. UMPK owns protocol support, world data, physics and pathfinding. Official plugins belong to [Marketplace](https://github.com/MCCTeam/Marketplace).

## Runtime and localization

- Configuration uses `client.toml`, `accounts.toml`, `servers.toml` and `console.toml`.
- Keep console and TUI settings in MCC and portable settings in DMCBK.
- Do not generate runtime configuration or authentication caches in the repository root.
- Keep credentials, `.env`, installed plugins and artifacts out of Git.
- Beacon scripts use `.bcn`. Marketplace indexes, manifests and locks require schema 2.
- Automated runs use `MCC_FILE_INPUT=1` and an absolute `MCC_INPUT_FILE`. Do not use a synthetic terminal against public servers.
- Use temporary configurations for each test. Shared server matrices run sequentially unless ports and storage are isolated.
- All new user-visible text must use resources: `Localization/Resources/MccStrings.resx` or `Resources/TextResources.resx`.
- Regenerate accessors with `python3 tools/localization/generate_strings.py`; verify with `--check`. Do not edit generated code or depend on removed client resources.

## Engineering rules

- Use nullable-aware C# and the configured language version. Prefer `Try*` APIs for expected failures.
- Use explicit imports and keep host adapters independent of protocol internals.
- Route classic output through `HostConsole` to keep the input prompt synchronized.
- Use DMCBK command registration and typed presentation values. Keep terminal formatting in MCC.
- Dispose subscriptions, timers and plugin resources on session end or shutdown.
- Update tests and documentation when behavior changes; run the CLI tests before completion.
- Do not modify `ConsoleInteractive/` or add library source dependencies.
- The documentation describes MCC 2.0. Check source and examples before changing a guide. Run tools/check_docs.py and the documentation build.
- Read the relevant skill in `.skills` before use. See [agent skills](docs/contributing/agent-skills.md) for setup and revision updates.
- Use Humanizer for prose and ASD-STE100 principles for instructions. Keep translated code and placeholders unchanged.
- Container files are Dockerfile and compose.yml. Do not put runtime secrets in images.
- Run tools/test_installers.py when changing installers or release packaging.
- Do not use em dashes unless the user requests them.
- Do not commit or push until the user gives explicit approval.
