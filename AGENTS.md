# AGENTS.md

## Project
- Minecraft Console Client (MCC) is a cross-platform text/TUI client for Minecraft Java Edition.
- Primary scope: connect to servers, send chat and commands, receive text, automate gameplay/admin tasks, and extend behavior through built-in bots or runtime C# scripts.
- Secondary scope: protocol/version adaptation tooling, docs site, legacy GUI wrapper, and debug tooling.

## Build / Run
- Init submodules first: `git submodule update --init --recursive`
- Build: `dotnet build MinecraftClient.sln -c Release`
- Publish (matches CI shape): `dotnet publish MinecraftClient.sln -f net8.0 -r <RID> --self-contained=true -c Release -p:UseAppHost=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=Embedded`
- Run from source: `dotnet run --project MinecraftClient -- --help`
- Docs: `cd docs && npm install && npm run docs:dev` or `npm run docs:build`
- Docker: `cd Docker && docker build -t minecraft-console-client:latest .`
- Tests: no dedicated test project is present in the main solution.
- Current state: the solution builds after submodule init, but `dotnet build` emits many analyzer and NuGet vulnerability warnings; treat them as real.

## Architecture
- `Program` bootstraps console I/O, TOML config, auth/session state, MC version selection, Forge detection, then creates `McClient`.
- `McClient` is the live session runtime: TCP client, selected protocol handler, Brigadier command dispatcher, loaded bots, world/inventory/entity state, queued chat, movement/pathing, reconnect flow.
- `Protocol/` is the network/auth boundary. `ProtocolHandler` maps Minecraft versions to protocol numbers and selects either `Protocol16Handler` (1.4.6-1.6.4) or `Protocol18Handler` (1.7.2+).
- `Scripting/ChatBot` is the extension boundary. Built-in bots and `/script` C# bots share the same event/tick API.
- Main runtime flow: console input -> internal Brigadier command or server chat; packets -> protocol handler -> `McClient` state update -> bot events; `OnUpdate()` (~10 Hz) drives bot ticks, delayed work, chat cooldowns, movement, and main-thread tasks.

## Technology Stack
- Main app: C#, .NET 8, nullable enabled.
- Command system: `Brigadier.NET`.
- Config: TOML via `Samboy063.Tomlet`.
- Runtime scripting: Roslyn (`Microsoft.CodeAnalysis.CSharp`) with in-memory compilation.
- Networking/auth: custom Minecraft protocol handlers, DNS SRV lookup (`DnsClient`), Forge/session/profile-key support.
- Integrations: `DSharpPlus`, `Telegram.Bot`, `MessagePack`, `Magick.NET`, `Sentry`.
- Docs site: VuePress 2 (`docs/package.json`).
- Tooling: Docker, GitHub Actions, Python 3.10+ scripts under `tools/` for palette/version generation.
- Legacy UI: `MinecraftClientGUI` is a separate .NET Framework 4.0 WinForms wrapper, not the main runtime.

## Version Support
Feature columns mean:
- Inventory: `/inventory` plus inventory/container bot APIs
- Movement: terrain handling, `/move`, and movement/pathing bots
- Entity: entity tracking and entity-driven bot events

| Minecraft | Protocol path | Inventory | Movement | Entity | Notes |
| --- | --- | --- | --- | --- | --- |
| 1.4.6-1.6.4 | `Protocol16Handler` | No | No | No | Core login/chat only |
| 1.7.2-1.7.10 | `Protocol18Handler` | No | Yes | No | Pre-1.8 special case |
| 1.8-1.9.4 | `Protocol18Handler` | Partial / docs conflict | Yes | Yes | Runtime gates allow 1.8+, but docs still warn inventory is unsupported through 1.9 |
| 1.10-1.12.2 | `Protocol18Handler` | Yes | Yes | Yes | Pre-flattening palettes |
| 1.13-1.19.2 | `Protocol18Handler` | Yes | Yes | Yes | Flattened block/item/entity palettes |
| 1.19.3-1.20.4 | `Protocol18Handler` | Yes | Yes | Yes | Newer chat/signing and palette splits |
| 1.20.6-1.21.4 | `Protocol18Handler` | Yes | Yes | Yes | Registry-driven world/attribute handling |
| 1.21.5-1.21.8 | `Protocol18Handler` | Yes | Yes | Yes | 1.21.7/1.21.8 reuse 1.21.6 block/entity palettes in code |
| 1.21.9-1.21.10 | `Protocol18Handler` | Yes | Yes | Yes | Latest coded support; version tools prefer server data reports since 1.21.9 |

Notes:
- Declared code range is `1.4.6` to `1.21.10`.
- Human docs are stale in places and sometimes stop at older ranges; prefer code when docs and code disagree.
- Movement/pathing limits called out in docs still apply: no swimming, no jumping, no knockback, slab support is partial.

## Module Map
- `MinecraftClient/`: main `net8.0` runtime project.
- `MinecraftClient/Protocol/`: protocol selection, auth/session flows, packet I/O, Forge/profile-key support.
- `MinecraftClient/Mapping/`: world state, movement/pathfinding, block/entity/material palettes.
- `MinecraftClient/Inventory/`: containers, items, enchantments, inventory helpers, item palettes.
- `MinecraftClient/Commands/` and `MinecraftClient/CommandHandler/`: internal MCC commands plus Brigadier argument types/patches.
- `MinecraftClient/ChatBots/`: built-in automation bots, bridges, script scheduler, replay/map/item helpers.
- `MinecraftClient/Scripting/`: `ChatBot` API, runtime C# compilation, movement lock helpers.
- `MinecraftClient/config/`: sample scripts and example bots; excluded from compilation.
- `ConsoleInteractive/`: required git submodule for richer console input/output.
- `docs/`: VuePress documentation site.
- `tools/`: Python scripts for version adaptation and palette generation.
- `DebugTools/`: packet/proxy debugging utilities.
- `MinecraftClientGUI/`: legacy Windows GUI wrapper around the console app.

## Engineering Guidance

### DO
- Keep startup/config/auth logic in `Program` and connection runtime logic in `McClient` or `Protocol/*`.
- Update version support holistically: protocol constants, version mapping, packet palette, block palette, item palette, entity palette, metadata palette, and routing switches.
- Use `tools/` and authoritative server data reports when adapting to new Minecraft versions, especially 1.21.9+.
- Guard optional subsystems with `GetTerrainEnabled()`, `GetInventoryEnabled()`, and `GetEntityHandlingEnabled()` before using them.
- For built-in bots, wire all pieces together: bot class, `Settings.ChatBotConfigHealper`, and `McClient.RegisterBots()`.
- Keep `Initialize()` for setup/prereq checks and `AfterGameJoined()` for sending chat or commands.
- Normalize inbound chat with `GetVerbatim()` before `IsChatMessage()` / `IsPrivateMessage()`.
- Clean up commands, plugin channels, threads, timers, and movement locks in `OnUnload()`.
- Prefer nullable-aware code, pattern matching, `ArgumentNullException.ThrowIfNull`, `Try*` APIs for expected failures, and `InvokeOnMainThread()` for cross-thread state changes.
- Use modern C# only when it fits the current target: the repo builds as `net8.0` with default language version.

### DON'T
- Don't update only `MCVer2ProtocolVersion()` or only one palette file when adding a new Minecraft version.
- Don't send chat in `Initialize()`.
- Don't mutate inventory snapshots and expect server-side effects; use handler APIs/window actions.
- Don't bypass Brigadier with ad hoc command parsing.
- Don't start background workers when `Update()` or delayed tasks are sufficient; if you must, stop them on unload/disconnect.
- Don't leave movement locks, plugin channels, or dispatcher registrations behind.
- Don't trust older docs over current code for supported versions or feature gates.
