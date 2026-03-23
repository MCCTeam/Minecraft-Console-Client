# AGENTS.md

## Project
- Minecraft Console Client (MCC) is a cross-platform text/TUI client for Minecraft Java Edition.
- Primary scope: connect to servers, send chat and commands, receive text, automate gameplay/admin tasks, and extend behavior through built-in bots or runtime C# scripts.
- Secondary scope: protocol/version adaptation tooling, docs site, legacy GUI wrapper, and debug tooling.

## Build / Run
- Init submodules first: `git submodule update --init --recursive`
- Build: `dotnet build MinecraftClient.sln -c Release`
- Publish (matches CI shape): `dotnet publish MinecraftClient.sln -f net10.0 -r <RID> --self-contained=true -c Release -p:UseAppHost=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=Embedded`
- Run from source: `dotnet run --project MinecraftClient -- --help`
- Docs: `cd docs && npm install && npm run docs:dev` or `npm run docs:build`
- Docker: `cd Docker && docker build -t minecraft-console-client:latest .`
- Tests: no dedicated test project is present in the main solution.
- Current state: the solution builds after submodule init, but `dotnet build` emits many analyzer and NuGet vulnerability warnings; treat them as real.
- Server roots: `tools/` helpers look for server jars under `MinecraftOfficial/downloads/<version>/` by default, but also support an external root via the `MCC_SERVERS` environment variable.
- Multi-version testing: tmux-based local server sessions are shared state. Run cross-version test matrices sequentially unless you have explicit per-version isolation. A server logging `Done` does not guarantee immediate RCON availability; retry RCON setup commands.
- Automated test configs: for repeated or matrix test runs, prefer generating a temporary MCC config per run instead of reusing the repo-root `MinecraftClient.ini`, to avoid leaking state between runs.

## Architecture
- `Program` bootstraps console I/O, TOML config, auth/session state, MC version selection, Forge detection, then creates `McClient`.
- `McClient` is the live session runtime: TCP client, selected protocol handler, Brigadier command dispatcher, loaded bots, world/inventory/entity state, queued chat, movement/pathing, reconnect flow.
- `Protocol/` is the network/auth boundary. `ProtocolHandler` maps Minecraft versions to protocol numbers and selects either `Protocol16Handler` (1.4.6-1.6.4) or `Protocol18Handler` (1.7.2+).
- `Scripting/ChatBot` is the extension boundary. Built-in bots and `/script` C# bots share the same event/tick API.
- Main runtime flow: console input -> internal Brigadier command or server chat; packets -> protocol handler -> `McClient` state update -> bot events; `OnUpdate()` (20 TPS) drives bot ticks, delayed work, chat cooldowns, movement, and main-thread tasks.

## Technology Stack
- Main app: C#, .NET 10, nullable enabled.
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
| 1.21.9-1.21.10 | `Protocol18Handler` | Yes | Yes | Yes | Version tools prefer server data reports since 1.21.9 |
| 1.21.11 | `Protocol18Handler` | Yes | Yes | Yes | Own entity/item/metadata palettes; blocks reuse 1.21.9 palette |
| 26.1 | `Protocol18Handler` | Yes | Yes | Yes | Latest coded support; new Minecraft version naming scheme |

Notes:
- Declared code range is `1.4.6` to `26.1`.
- Human docs are stale in places and sometimes stop at older ranges; prefer code when docs and code disagree.
- Movement/pathing limits called out in docs still apply: no swimming, no knockback. The `Physics/` engine adds vanilla-accurate collision and movement but some edge cases remain.

## Module Map
### Core Runtime

| Module | What It Owns | Important Files |
| --- | --- | --- |
| `MinecraftClient/` | Main `net10.0` runtime assembly and the best starting point. `Program.cs` owns startup, config load/writeback, CLI handling, auth/version selection, update/data-generation entrypoints, and restart/failure flow. `McClient.cs` owns the live session runtime: protocol handler ownership, command dispatch, bot lifecycle, world/inventory/entity state, queued chat, movement ticks, reconnect/disconnect logic, and the main-thread invoke queue. `Settings.cs` defines the TOML schema and runtime/internal overrides used across the app. | `Program.cs`, `McClient.cs`, `Settings.cs`, `ConsoleIO.cs`, `Command.cs`, `UpgradeHelper.cs`, `AutoTimeout.cs` |
| `MinecraftClient/Protocol/` | Network/auth/session boundary. `ProtocolHandler.cs` does DNS SRV lookup, server ping/version detection, MC-version to protocol mapping, and handler selection. `Protocol16.cs` and `Protocol18.cs` implement the packet flow for legacy and modern versions. `Protocol18Terrain.cs` decodes chunk sections/biomes into `World`. `DataTypes.cs` is the low-level reader/writer layer for VarInts, metadata, NBT-like structures, and packet fields. `Message/`, `ProfileKey/`, `Session/`, `Handlers/Forge/`, `Handlers/PacketPalettes/`, `Handlers/Packet/`, and `Handlers/StructuredComponents/` cover chat/signing, cached auth, Forge, packet IDs, packet-level parsing, and 1.20.6+ item components with versioned registries under `StructuredComponents/Registries/`. | `Protocol/ProtocolHandler.cs`, `Protocol/Handlers/Protocol16.cs`, `Protocol/Handlers/Protocol18.cs`, `Protocol/Handlers/Protocol18Terrain.cs`, `Protocol/Handlers/DataTypes.cs`, `Protocol/Message/ChatParser.cs`, `Protocol/MicrosoftAuthentication.cs`, `Protocol/MojangAPI.cs` |
| `MinecraftClient/Mapping/` | World model, terrain storage, movement logic, and versioned block/entity metadata. `World.cs` stores chunk columns, dimension data, and 1.20.6+ registry-derived dimension/attribute mappings. `Chunk*`, `Block.cs`, and `Location.cs` are the terrain primitives. `Movement.cs` contains step generation, gravity/on-ground checks, and path execution support. `Material.cs` plus `BlockPalettes/*.cs` map block-state IDs to MCC materials. `Entity.cs`, `EntityType.cs`, `EntityPalettes/*.cs`, `EntityMetadataPalette.cs`, and `EntityMetadataPalettes/*.cs` do the same for entities and metadata serializers. | `Mapping/World.cs`, `Mapping/ChunkColumn.cs`, `Mapping/Chunk.cs`, `Mapping/Block.cs`, `Mapping/Location.cs`, `Mapping/Movement.cs`, `Mapping/RaycastHelper.cs`, `Mapping/Material.cs`, `Mapping/Entity.cs`, `Mapping/EntityType.cs` |
| `MinecraftClient/Inventory/` | Inventory/container snapshots, item decoding, and versioned item registries. `Container.cs` models player inventories and server windows, including slot contents and container properties. `Item.cs` bridges older NBT-based items with 1.20.6+ structured components. `ItemType.cs` plus `ItemPalettes/*.cs` provide version-specific item ID mapping. Enchantment, effects, and villager-trade files add higher-level semantics on top of raw inventory data. | `Inventory/Container.cs`, `Inventory/ContainerType.cs`, `Inventory/Item.cs`, `Inventory/ItemMovingHelper.cs`, `Inventory/ItemType.cs`, `Inventory/ItemPalettes/*.cs`, `Inventory/EnchantmentMapping.cs`, `Inventory/VillagerTrade.cs` |
| `MinecraftClient/Physics/` | Vanilla-accurate per-tick physics engine. `PlayerPhysics.cs` mirrors vanilla `Entity.move()`, `LivingEntity.aiStep()/travel()`, and `Player.travel()` logic at 20 TPS, handling ground/air/water/lava/creative-fly travel, jumping, sprint-jump boost, climbing, sneak-edge-detection, friction, drag, gravity, slow-falling, and levitation. `CollisionDetector.cs` resolves full AABB collisions against the block world including step-up, mirroring vanilla axis-separated resolution. `BlockShapes.cs` maps block-state IDs to collision AABBs using PrismarineJS data from `BlockShapeData.json`. `Vec3d.cs` and `Aabb.cs` provide the geometric primitives. `MovementInput.cs` captures player input state. | `Physics/PlayerPhysics.cs`, `Physics/PhysicsConsts.cs`, `Physics/CollisionDetector.cs`, `Physics/BlockShapes.cs`, `Physics/BlockShapeData.json`, `Physics/Vec3d.cs`, `Physics/Aabb.cs`, `Physics/MovementInput.cs` |

### Commands And Extensions

| Module | What It Owns | Important Files |
| --- | --- | --- |
| `MinecraftClient/Commands/` and `MinecraftClient/CommandHandler/` | Internal MCC command system built on Brigadier. Commands are discovered by reflection from `MinecraftClient.Commands` in `McClient.LoadCommands()`. Each file in `Commands/` registers one internal command. `ArgumentType/*.cs` provides typed Brigadier arguments and completion sources for accounts, bots, items, locations, scripts, inventories, and more. `Patch/*.cs` carries MCC-specific Brigadier extensions, and `CmdResult.cs` is the command execution result object. | `Command.cs`, `Commands/*.cs`, `CommandHandler/MccArguments.cs`, `CommandHandler/CmdResult.cs`, `CommandHandler/ArgumentType/*.cs`, `CommandHandler/Patch/*.cs` |
| `MinecraftClient/ChatBots/` | Built-in bots and bridges loaded from config through `McClient.RegisterBots()`. The folder mixes gameplay automation (`AutoAttack`, `AutoDig`, `AutoEat`, `AutoFishing`, `Farmer`), utility/logging bots (`ChatLog`, `PlayerListLogger`, `Alerts`), bridges (`DiscordBridge`, `TelegramBridge`, `RemoteControl`), and tooling like `ScriptScheduler`, `Map`, and `ReplayCapture`. | `ChatBots/AutoRelog.cs`, `ChatBots/Farmer.cs`, `ChatBots/FollowPlayer.cs`, `ChatBots/ItemsCollector.cs`, `ChatBots/Map.cs`, `ChatBots/RemoteControl.cs`, `ChatBots/ScriptScheduler.cs`, `ChatBots/DiscordBridge.cs`, `ChatBots/TelegramBridge.cs`, `ChatBots/ReplayCapture.cs` |
| `MinecraftClient/Scripting/` | Shared extension boundary for compiled bots and runtime C# scripts. `ChatBot.cs` is the main bot API and lifecycle surface. Built-in bots and `/script` bots use the same event model. `CSharpRunner.cs` parses `//MCCScript` files, compiles them with Roslyn, caches assemblies, and executes them through `CSharpAPI`. `DynamicRun/Builder/*` handles in-memory compilation/load-context plumbing, while `BotMovementLock.cs` coordinates movement ownership between automation pieces. | `Scripting/ChatBot.cs`, `Scripting/CSharpRunner.cs`, `Scripting/BotMovementLock.cs`, `Scripting/AssemblyResolver.cs`, `Scripting/DynamicRun/Builder/Compiler.cs`, `Scripting/DynamicRun/Builder/CompileRunner.cs` |
| `MinecraftClient/config/` | Sample runtime assets excluded from compilation. This is the examples/staging area for end-user scripts and standalone bots. `sample-script*.cs` shows supported `/script` patterns (basic, chatbot, world access, HTTP requests, tasks, PM forwarding, extended), while `config/ChatBots/*.cs` are copy/adapt examples rather than built-in bots. | `config/README.md`, `config/sample-script.cs`, `config/sample-script-with-chatbot.cs`, `config/sample-script-with-world-access.cs`, `config/sample-script-with-http-request.cs`, `config/sample-script-with-task.cs`, `config/ChatBots/*.cs` |
| `ConsoleInteractive/` | Required git submodule for richer line editing and console UI. MCC uses the submodule's `ConsoleReader`, `ConsoleWriter`, and suggestion UI from `ConsoleIO.cs` and `McClient.cs` when `BasicIO` is not enabled. | `ConsoleInteractive/README.md`, `ConsoleInteractive/ConsoleInteractive/ConsoleInteractive.sln` |

### Support And Tooling

| Module | What It Owns | Important Files |
| --- | --- | --- |
| `MinecraftClient/Logger/`, `MinecraftClient/Proxy/`, `MinecraftClient/Crypto/`, `MinecraftClient/Resources/`, `MinecraftClient/WinAPI/` | Support subsystems under the main app. Logging supports console/file output plus regex filtering. `ProxyHandler.cs` routes update/login/in-game traffic through HTTP or SOCKS proxies. `Crypto/` implements the stream ciphers needed for online-mode protocol encryption. `Resources/` contains UI strings, generated translation accessors, config help text, icons, and embedded Minecraft asset data. `WinAPI/` contains small Windows-only console helpers. | `Logger/FilteredLogger.cs`, `Logger/FileLogLogger.cs`, `Proxy/ProxyHandler.cs`, `Crypto/CryptoHandler.cs`, `Crypto/AesCfb8Stream.cs`, `Resources/Translations/Translations.resx`, `Resources/ConfigComments/ConfigComments.resx`, `Resources/en_us.json`, `WinAPI/ConsoleIcon.cs` |
| `docs/` | VuePress documentation site. `.vuepress/config.ts` sets bundler, theme, plugins, and redirects. `.vuepress/configs/**` holds locale and nav wiring. `guide/*.md` contains the user-facing install, usage, bot, and scripting docs. | `docs/.vuepress/config.ts`, `docs/.vuepress/configs/**`, `docs/guide/README.md`, `docs/guide/configuration.md`, `docs/guide/chat-bots.md`, `docs/guide/creating-bots.md`, `docs/guide/creating-text-script.md`, `docs/guide/ai-assisted-development.md` |
| `tools/` | Python helpers for Minecraft version adaptation and palette generation. `README.md` is the authoritative workflow. `diff_registries.py` compares versions and validates decompiled data against server reports. The `gen_*` scripts emit the versioned palette source files consumed by `Protocol/`, `Mapping/`, `Inventory/`, and `Physics/`. | `tools/README.md`, `tools/diff_registries.py`, `tools/gen_block_palette.py`, `tools/gen_item_palette.py`, `tools/gen_entity_palette.py`, `tools/gen_entity_metadata_palette.py`, `tools/gen_block_shapes.py`, `tools/gen_command_argument_registry.py` |
| `DebugTools/` | Standalone packet/proxy debugging utilities for inspecting traffic and compression behavior outside the main client runtime. | `DebugTools/MinecraftClientProxy/Program.cs`, `DebugTools/MinecraftClientProxy/PacketProxy.cs`, `DebugTools/MinecraftClientProxy/ZlibUtils.cs` |
| `MinecraftClientGUI/` | Legacy Windows GUI wrapper around the console app. WinForms shell that launches and communicates with the console executable; not part of the main `net10.0` runtime path. | `MinecraftClientGUI/Program.cs`, `MinecraftClientGUI/Form1.cs`, `MinecraftClientGUI/Form1.Designer.cs`, `MinecraftClientGUI/MinecraftClient.cs` |

## Engineering Guidance

Read `docs/guide/ai-assisted-development.md` before starting development work on MCC. It documents the full build-run-test loop, local server harness, repository tools, and standard workflows.

### DO
- Keep startup/config/auth logic in `Program` and connection runtime logic in `McClient` or `Protocol/*`.
- Update version support holistically: protocol constants, version mapping, packet palette, block palette, item palette, entity palette, metadata palette, and routing switches.
- Use `tools/` and authoritative server data reports when adapting to new Minecraft versions.
- Guard optional subsystems with `GetTerrainEnabled()`, `GetInventoryEnabled()`, and `GetEntityHandlingEnabled()` before using them.
- For built-in bots, wire all pieces together: bot class, `Settings.ChatBotConfigHealper`, and `McClient.RegisterBots()`.
- Keep `Initialize()` for setup/prereq checks and `AfterGameJoined()` for sending chat or commands.
- Normalize inbound chat with `GetVerbatim()` before `IsChatMessage()` / `IsPrivateMessage()`.
- Clean up commands, plugin channels, threads, timers, and movement locks in `OnUnload()`.
- Prefer nullable-aware code, pattern matching, `ArgumentNullException.ThrowIfNull`, `Try*` APIs for expected failures, and `InvokeOnMainThread()` for cross-thread state changes.
- Use modern C# 14 features.
- Use provided skills proactively depending on the context, read their descriptions to determine when to use them.

### DON'T
- Don't update only `MCVer2ProtocolVersion()` or only one palette file when adding a new Minecraft version.
- Don't send chat in `Initialize()`.
- Don't mutate inventory snapshots and expect server-side effects; use handler APIs/window actions.
- Don't bypass Brigadier with ad hoc command parsing.
- Never modify `ConsoleInteractive/`; treat it as an external required submodule.
- Don't start background workers when `Update()` or delayed tasks are sufficient; if you must, stop them on unload/disconnect.
- Don't leave movement locks, plugin channels, or dispatcher registrations behind.
- Don't trust older docs over current code for supported versions or feature gates. When AGENTS.md, skills, and older docs disagree, prefer current code and current tool behavior, then update the stale source.
- Never use "—" ("em dash"), unless specifically being instructed to do so!
