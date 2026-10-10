# Official plugin reference

Official plugins live in [Marketplace](https://github.com/MCCTeam/Marketplace). MCC supplies the host. Install only the plugins that your task needs.

Each page explains its package ID, installation, settings, commands, a first test, and known limits. Package IDs differ from the C# source folder names.

MCC registers this marketplace as `official` by default. Enter `/plugins search <text> in official` to find a plugin and `/plugins install <id>@official` to install it. See [marketplace usage](../marketplaces/using.md) for refresh, updates and removal.

| Plugin | Package ID | Purpose |
| --- | --- | --- |
| [Alerts](official/alerts.md) | `alerts` | Watches chat and game events, then sends notifications to the destinations that you select. |
| [Anti-AFK](official/anti-afk.md) | `anti-afk` | Sends a periodic action so a server can observe activity from the client. |
| [Auto-attack](official/auto-attack.md) | `auto-attack` | Attacks selected nearby entities at the configured cooldown. |
| [Auto-craft](official/auto-craft.md) | `auto-craft` | Crafts queued items and transfers ingredients and results through registered storage. |
| [Auto-dig](official/auto-dig.md) | `auto-dig` | Breaks a selected block or a configured list of block positions. |
| [Auto-drop](official/auto-drop.md) | `auto-drop` | Drops item types selected by an inclusion or exclusion list. |
| [Auto-eat](official/auto-eat.md) | `auto-eat` | Uses food when the player food level falls to the configured threshold. |
| [Auto-fishing](official/auto-fishing.md) | `auto-fishing` | Casts a rod, detects a bite, reels the rod, and repeats. |
| [Auto-relog](official/auto-relog.md) | `auto-relog` | Attempts a new connection after selected unexpected disconnects. |
| [Auto-respond](official/auto-respond.md) | `auto-respond` | Matches chat rules and dispatches an MCC command for each matching rule. |
| [Chat log](official/chat-log.md) | `chat-log` | Writes selected chat and internal messages to a file. |
| [Discord bridge](official/discord-bridge.md) | `discord-bridge` | Relays Minecraft and Discord messages and lets authorized Discord users run MCC commands. |
| [Discord rich presence](official/discord-rpc.md) | `discord-rpc` | Displays selected session information on your Discord profile. |
| [Farmer](official/farmer.md) | `farmer` | Harvests supported crops, collects drops, replants, and optionally uses bone meal and storage. |
| [Follow player](official/follow-player.md) | `follow-player` | Follows a tracked player while maintaining a minimum distance. |
| [Items collector](official/items-collector.md) | `items-collector` | Walks towards nearby dropped items to collect them. |
| [LLM core](official/llm-core.md) | `llm-core` | Provides model providers, budgets, skills, and remote tools to other plugins. |
| [Maps](official/map.md) | `map` | Collects Minecraft map updates, presents map images, and optionally exports them. |
| [MCP server](official/mcp-server.md) | `mcp-server` | Exposes session queries and controlled game actions over a local Model Context Protocol HTTP endpoint. |
| [Player list logger](official/player-list-logger.md) | `player-list-logger` | Records online players and join/leave changes in a file. |
| [Remote control](official/remote-control.md) | `remote-control` | Accepts MCC commands sent in private messages by configured bot owners. |
| [Replay capture](official/replay-capture.md) | `replay-capture` | Records protocol session data in a ReplayMod archive. |
| [Script scheduler](official/script-scheduler.md) | `script-scheduler` | Runs MCC commands at login, a dialog event, a time of day, or a repeating interval. |
| [Telegram bridge](official/telegram-bridge.md) | `telegram-bridge` | Relays game chat to Telegram and accepts commands from authorized Telegram chats. |

The developer plugin `llm-test` is outside this user reference. Channel Logger, Velocity Forwarding, Human Motion and Test Bot are not included in this marketplace.

## Choose plugins that work together

For basic automation, start with one plugin. Add others after the first plugin behaves correctly. FollowPlayer, ItemsCollector, Farmer, and walking AntiAFK can need the same movement lease. Stop one task before another task needs movement.

Map and Alerts can send through DiscordBridge or TelegramBridge when those optional providers are installed and compatible. Alerts can use LlmCore for smart matching. Those optional providers do not install automatically.

Keep your game data features enabled when a plugin needs them. A manifest capability such as `commands` is different from terrain or inventory handling in the game session.

[Manage plugins](managing.md) · [Use a marketplace](../marketplaces/using.md) · [Make a plugin](development/index.md)
