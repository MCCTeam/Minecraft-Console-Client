# Command reference

Type these commands inside MCC. Examples use the default `/` prefix. Startup options such as `--auth` belong in the operating system terminal. See [startup arguments](../getting-started/arguments.md).

Angle brackets identify a required value. Square brackets identify an optional value. The symbol `|` separates choices. Do not type the brackets or the separator.

MCC handles recognized names locally. Use `//name` to send `/name` to the server when the names overlap. Unknown slash commands can reach the server. See [input routing](../client/chat.md).

This reference covers all 47 active built-in, Beacon, and host command names. Plugins and scripts can register additional commands. Use `/help` for the registry in your running client.

## Connection

| Command | Purpose |
| --- | --- |
| [`/connect`](connect.md) | Connect to the specified server. |
| [`/reco`](reco.md) | Restart and reconnect to the server. |
| [`/servers`](servers.md) | Save servers to servers.toml for /connect. |
| [`/reload`](reload.md) | Reloads MCC settings. |
| [`/lang`](lang.md) | Show or change the MCC interface language. |
| [`/exit`](exit.md) | Disconnect and stop MCC, optionally with a process exit code. |

## Help and console

| Command | Purpose |
| --- | --- |
| [`/help`](help.md) | Show what a command does and how to type it. |
| [`/man`](man.md) | Read the MCC manual: guides, not syntax. |
| [`/mcc-menu`](mcc-menu.md) | Open the main TUI management menu. |
| [`/clear-console`](clear-console.md) | Clear the classic screen or the TUI log pane. |
| [`/console-chat`](console-chat.md) | Show or hide incoming chat in the current interface. |
| [`/log`](log.md) | Log some text to the console. |
| [`/send`](send.md) | Send a chat message or command. |
| [`/set`](set.md) | Set a custom %variable%. |
| [`/debug`](debug.md) | Toggle debug messages. |

## Player and server status

| Command | Purpose |
| --- | --- |
| [`/health`](health.md) | Display Health and Food saturation. |
| [`/effects`](effects.md) | List your currently active effects. |
| [`/list`](list.md) | Get the player list. |
| [`/tab`](tab.md) | Show a vanilla-like tab list. In TUI mode, opens a live overlay. |
| [`/tps`](tps.md) | Estimate server ticks per second. |
| [`/teams`](teams.md) | List all scoreboard teams and their members. |
| [`/scoreboard`](scoreboard.md) | Show scoreboard objectives and their scores. |
| [`/achievement`](achievement.md) | List your advancements and how far through them you are. |
| [`/respawn`](respawn.md) | Use this to respawn if you are dead. |

## Movement and world

| Command | Purpose |
| --- | --- |
| [`/move`](move.md) | Walk, or path to coordinates. |
| [`/look`](look.md) | Look at direction or coordinates. |
| [`/sneak`](sneak.md) | Toggles sneaking. |
| [`/animation`](animation.md) | Swing your arm. |
| [`/blockinfo`](blockinfo.md) | Report what the client believes is at a position. |
| [`/chunk`](chunk.md) | Inspect loaded chunks or open the TUI chunk browser. |
| [`/dig`](dig.md) | Attempt to break a block. |
| [`/useblock`](useblock.md) | Request a block interaction with the selected hand. |
| [`/useitem`](useitem.md) | Use the item in your hand, optionally on a specific block. |
| [`/entity`](entity.md) | List nearby entities, or attack/use one. |
| [`/bed`](bed.md) | Used to sleep in or leave a bed. |
| [`/minimap`](minimap.md) | Control the TUI terrain minimap and its labels. |
| [`/map`](map.md) | Open the received Minecraft filled-map view in the TUI. |

## Inventory and menus

| Command | Purpose |
| --- | --- |
| [`/inventory`](inventory.md) | View and click your inventory or an open container. |
| [`/changeslot`](changeslot.md) | Select a hotbar slot, by number or by the item in it. |
| [`/dropitem`](dropitem.md) | Drop a selected stack or named items. |
| [`/enchant`](enchant.md) | Click an option in an open enchanting table. |
| [`/nameitem`](nameitem.md) | Rename the item in an open anvil. |
| [`/recipebook`](recipebook.md) | List unlocked recipes, or place one into the open menu. |
| [`/book`](book.md) | Read, write, edit, and sign the book held in your main hand. |
| [`/dialog`](dialog.md) | View and interact with the current server custom dialog. |

## Scripts and plugins

| Command | Purpose |
| --- | --- |
| [`/scripts`](scripts.md) | Manage Beacon scripts. |
| [`/plugins`](plugins.md) | List, load, enable, disable and reload plugins. |

## Aliases

| Alias | Command |
| --- | --- |
| `/quit` | [`exit`](exit.md) |
| `/cc` | [`clear-console`](clear-console.md) |
| `/advancements` | [`achievement`](achievement.md) |
| `/plugins market ...` | [`plugins marketplace ...`](plugins.md) |
| `/inventory p ...` | [`inventory player ...`](inventory.md) |
| `/inventory c ...` | [`inventory container ...`](inventory.md) |

## Removed commands

`/script` and `/upgrade` remain recognized notices. They do not provide their old functionality. Use `/scripts` for Beacon and the plugin workflow for C# extensions. Install a new MCC distribution to update the application.

## Shared argument rules

Coordinate triples can use absolute values, `~` world-relative offsets, or a fully `^` local triple. A local triple must not mix caret and ordinary coordinates. Chunk coordinate pairs require plain integers.

Item and entity selectors accept resource identifiers such as `minecraft:stone` and `minecraft:zombie`. Many command nodes accept short names in the default Minecraft namespace. Suggestions can require loaded session registries.

Quote a single string argument that contains spaces. Greedy text arguments consume the remaining line. Follow a command's syntax rather than putting quotes around an entire multi-value coordinate triple.

Each command can expose an `_help` branch used by the help system. Prefer `/help <command>` for readable help.

## Preconditions

Game commands need a live connection. World, inventory, entity, physics, or navigation operations also need their corresponding features. A command's declared feature list is an initial guide. Some subcommands impose additional checks.

Views marked `ui`, plus `map` and `minimap`, need the TUI or an equivalent capable host. A valid packet request still depends on server acceptance.

[Return to the documentation home](../index.md).
