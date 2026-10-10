# Configuration and runtime folders

MCC generates commented TOML files when they are missing. A configuration folder is a directory that contains settings. It is not the executable folder itself.

## Folder layout

With `--configurations /home/user/mcc-data/configurations`, the usual layout is:

```text
mcc-data/
├── configurations/
│   ├── client.toml
│   ├── accounts.toml
│   ├── servers.toml
│   ├── console.toml
│   ├── .env
│   ├── marketplaces.toml
│   ├── beacon.toml
│   ├── beacon/
│   ├── cache/
│   └── logs/
├── scripts/
│   ├── welcome.bcn
│   └── data/
└── plugins/
```

MCC creates supporting folders and files when their feature needs them. `scripts` and `plugins` are siblings of the configuration folder. They do not normally live inside it.

| Path | Purpose |
| --- | --- |
| `client.toml` | Connection, game, chat, language, logs, diagnostics, and plugin runtime settings |
| `accounts.toml` | Account entries, cache settings, and proxy credentials |
| `servers.toml` | Named servers and the selected server |
| `console.toml` | Classic console and TUI preferences |
| `.env` | Optional environment values for settings that support `env:NAME` |
| `marketplaces.toml` | Added marketplaces and their update policies |
| `beacon.toml` | Beacon network policy and script defaults |
| `beacon/<id>.toml` | Persisted Beacon state |
| `beacon/<id>.settings.toml` | Per-script settings |
| `logs/` | Session diagnostic bundles |
| `../scripts/` | Beacon source files |
| `../plugins/` | Plugin packages, locks, cache, and user data |

Set the `MCC_PLUGINS` environment variable to use a different plugin root. Use an absolute path to avoid dependence on the process directory.

## Understand TOML

```toml
# This is a comment.
[Connection]
Host = "localhost"
Port = 25565
AutoConnect = false
```

A table name appears in square brackets. Each key receives one value. Strings need quotation marks. Numbers do not. Boolean values are `true` and `false`.

Use the generated spelling and table names. Put each table in the file specified by these pages. Avoid duplicate table definitions when you paste a fragment into an existing file.

## Change a setting

1. Stop MCC if the setting changes startup behavior.
2. Open the correct TOML file.
3. Find the existing key.
4. Change its value.
5. Save the file.
6. Start MCC with the same configuration folder.
7. Read any configuration warnings.

The loader can recover from malformed files with defaults. A successful process start does not prove that every setting was accepted.

For a temporary change, use a supported [startup override](../getting-started/arguments.md). Overrides do not edit files. Not every TOML key has an override binder.

## Connection

| Key in `[Connection]` | Default | Use |
| --- | --- | --- |
| `Host` | Empty | Endpoint fallback when no saved server overrides it |
| `Port` | `25565` | Default port |
| `Version` | `auto` | Protocol fallback |
| `SrvResolve` | `fast` | DNS SRV behavior |
| `TcpTimeout` | `30` | Connection timeout in seconds |
| `Brand` | `mcc` | Client brand sent to the server |
| `AutoConnect` | `true` | Connect at startup when an endpoint exists |

The selected `servers.toml` entry overrides endpoint defaults. Command-line arguments and dotted overrides can override the selected entry. See [servers](servers.md) for retries and saved entries.

## Gameplay

The default feature switches are:

```toml
[Gameplay]
Terrain = true
Inventory = true
Entity = true
Physics = true
Pathfinding = true
AutoRespawn = false
MoveHeadWhileWalking = true
MovementSpeed = 2
ShowEffectMessages = true
```

| Feature | Needed for |
| --- | --- |
| `Terrain` | Block and chunk data, minimap, world queries |
| `Inventory` | Containers, held items, books, recipes, inventory automation |
| `Entity` | Tracked entities and entity interactions |
| `Physics` | Player motion and collision behavior |
| `Pathfinding` | Navigation to a destination |

Physics needs terrain. Pathfinding needs physics. Configuration validation disables dependent features when their requirements are disabled.

For a chat-only client:

```toml
[Gameplay]
Terrain = false
Physics = false
Pathfinding = false
Inventory = false
Entity = false
```

Edit the existing table instead of adding a second `[Gameplay]` table. Reconnect or restart after changing composed session features. A running session does not automatically gain every module after a file reload.

## Chat

```toml
[Chat]
MessageCooldown = 1.0
MaxChatMessageLength = 0
PrivateMessageCommand = "tell"
```

`MessageCooldown` controls outgoing chat pacing. A zero maximum length lets protocol defaults determine the limit. `PrivateMessageCommand` is the server's private-message command name.

`[Chat.Format]` contains built-in parsing and optional regular expressions for server-specific public messages, private messages, and teleport requests. Enable `UserDefined` only when you supply the matching expressions.

`[Chat.Signature]` controls secure-profile login and the display of signed, modified, unverified, insecure, or system messages. These settings change handling and presentation. They do not make an unverified message trustworthy.

## Server-visible client settings

```toml
[ClientSettings]
Enabled = true
Locale = "en_US"
RenderDistance = 16
Difficulty = "peaceful"
ChatMode = "enabled"
ChatColors = true
MainHand = "left"
```

These settings announce preferences to the server. They do not change the server's world difficulty or guarantee a render distance. `[ClientSettings.Skin]` controls skin-part flags such as `Cape`, `Hat`, and `Jacket`.

`Locale` is the server-visible Minecraft locale. `[Localization] Language` is MCC's interface language. They are separate settings.

## Language and resource packs

```toml
[Localization]
Language = "auto"
LoadResourcePackTranslations = true
ResourcePackPolicy = "accept"
LoadForgeModTranslations = false
AutoDiscoverForgeModTranslationSources = true
ForgeModTranslationPath = ""
```

Resource-pack policy accepts `accept`, `decline`, or `prompt`. A required pack can affect whether the server allows you to play. MCC can read translation content without becoming a graphical Minecraft client.

Read [languages](languages.md) for MCC translation fallback and `/lang`.

## Logs and diagnostics

`[Logging]` controls message classes, filters, and optional text-file output. Important defaults include:

```toml
[Logging]
DebugMessages = false
PacketDebugMessages = false
ChatMessages = true
InfoMessages = true
WarningMessages = true
ErrorMessages = true
FilterMode = "disable"
LogToFile = false
LogFile = "console-log.txt"
PrependTimestamp = false
SaveColorCodes = false
```

A relative `LogFile` belongs beneath the configuration folder. `ChatFilterRegex` and `DebugFilterRegex` apply according to `FilterMode`, which accepts `disable`, `blacklist`, and `whitelist`.

Diagnostic defaults are:

```toml
[Diagnostics]
Enabled = true
CapturePackets = true
MaxCaptureMegabytes = 20
KeepSessions = 5
```

Packet recording has a hard ceiling of 20 MiB and 20 minutes per diagnostic session. Read [diagnostics](../troubleshooting/diagnostics.md) before sharing a bundle.

## Plugins, permissions, and variables

```toml
[Plugins]
CrashThreshold = 10
CrashWindowSeconds = 60

[Permissions]
CommandPrefix = "slash"
BotOwners = ["Player1", "Player2"]

[Variables]
home = "150 80 380"
guide_label = "Local test"
```

The plugin crash budget protects continued client operation after repeated plugin faults. It is not a security sandbox.

`CommandPrefix` accepts `slash`, `backslash`, or `none`. In `none` mode, ordinary input is treated as an MCC command. Use `/send` according to that mode's prefix rules to send chat.

Client variables hold strings. Commands expand `%home%`. Use letters, digits, and underscores in variable names. A dot is not a safe separator in this store.

## Secrets and .env

MCC reads `.env` from the configuration folder before loading plugin settings. Existing process environment values take precedence. The loader accepts assignments and optional `export` prefixes. It does not execute a shell script.

Example `.env`:

```dotenv
DISCORD_TOKEN="replace-with-your-token"
```

For a plugin setting that supports environment resolution:

```toml
Token = "env:DISCORD_TOKEN"
```

Not every TOML string is an environment-reference field. Follow the plugin's settings guide. Keep `.env`, account caches, plugin data, and diagnostic bundles outside public version control.

## Reload limits

`/reload` rereads portable configuration and invokes plugin configuration hooks. It refreshes command configuration and supported runtime settings. It does not reread every host setting in `console.toml`.

Restart MCC for a console mode change. Reconnect for session module changes. Reload plugin settings through their supported workflow. [The reload reference](../commands/reload.md) explains these boundaries.
