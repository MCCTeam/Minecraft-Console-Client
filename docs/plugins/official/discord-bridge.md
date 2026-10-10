# Discord bridge

Relays Minecraft and Discord messages and lets authorized Discord users run MCC commands.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `discord-bridge` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/DiscordBridge). It is not built into MCC. Its declared game/resource usage is `chat`, `network`, `commands`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install discord-bridge@official 2.0.2
/plugins enable discord-bridge
/plugins info discord-bridge
```

1. Check that `/plugins list` shows `discord-bridge` as loaded.
2. Open `plugins/userdata/discord-bridge/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload discord-bridge`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Token = "env:MCC_DISCORD_TOKEN"
OwnersIds = [123456789012345678]
```

Create a Discord bot and its token. Set `MCC_DISCORD_TOKEN` in the configurations `.env` file. Add a route with the commands below. A test message should arrive in that channel.

```text
/dscbridge channel add general <guild-id> <channel-id>
/dscbridge channel test general
/dscbridge status
/dscbridge channels
/dscbridge channel set general cmd on
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Token` | `""` | Discord bot token, or env:NAME to read it from the environment. Required; the plugin self-disables without it. Never commit a real token. |
| `GuildId` | `0` | Legacy guild id, used only when no routes are configured. |
| `ChannelId` | `0` | Legacy channel id, used only when no routes are configured. |
| `OwnersIds` | `[]` | Discord user ids allowed to relay into the game and run commands. Empty authorizes nobody. |
| `AllowDirectMessages` | `false` | Accept DMs from owners as if they came from a channel. |
| `DmRelayOut` | `false` | Relay game chat out to owner DMs, the same text the channels get. |
| `DeathEmbed` | `true` | Post a red embed with death coordinates when the client dies. |
| `DmOwnersOn` | `[]` | Game events that DM the owners instead of channels: whisper, teleport. |
| `CommandPrefix` | `"."` | Default command prefix for channels and DMs. One or two characters. |
| `Message_Send_Timeout` | `3` | Seconds a Discord send may take before the bridge abandons it. Below 1 falls back to 3. |
| `Allow_Other_Bot_Messages` | `false` | Relay messages posted by other bots into the game. Commands from bots are never run. |
| `Relay_All_Messages` | `false` | Relay every line, including server/system output the classifier could not categorize. |
| `Message_Aggregation_Interval` | `3.0` | Seconds to batch chat lines into one Discord message. 0 posts each line immediately. |
| `PrivateMessageFormat` | `"**[Private Message]** {username}: {message}"` | Message templates. Placeholders: {username} {message} {timestamp} |
| `PublicMessageFormat` | `"{username}: {message}"` | See the operation details below. |
| `TeleportRequestMessageFormat` | `"A new Teleport Request from **{username}**!"` | See the operation details below. |

## Write a route in TOML

The channel commands save routes for you. If you edit the file directly, use one `[[Routes]]` table per channel:

```toml
[[Routes]]
Name = "general"
GuildId = 111222333444555666
ChannelId = 777888999000111222
RelayOut = true
RelayIn = true
AllowCommands = false
OwnersIds = []
CommandPrefix = ""
```

Replace the numeric IDs with your Discord IDs. Empty route owners inherit the global `OwnersIds`. An empty route prefix inherits the global `CommandPrefix`. Keep global settings before the first `[[Routes]]` header.

## Requirements and limits

Needs network access, a bot token, Message Content intent, and channel permissions. Route commands remain off until explicitly enabled. Discord owner IDs authorize MCC control. Keep tokens out of screenshots and logs. The IDs below are examples.

## Detailed operation

### Talk to it

Type in a bridged channel and the game hears it. Start a line with the
channel prefix, usually a dot, and MCC runs the rest as a command. Each
channel can set its own prefix, see below:

```
Hello from Discord
.health
.chunk status
```

The answer comes back where you asked. Most commands print exactly what
the MCC console would show, inside a code block. Three commands answer
with custom renders: `man` sends the page itself, `health` a health
card, and `effects` an emoji list.

### Shape your channels

One channel rarely fits. A mirror channel wants chat both ways and no
commands. A control room wants commands and nothing else. Each bridged
channel, called a route, carries three switches: chat out, chat in, and
commands. Out sends game chat to Discord, in sends Discord chat to the
game, cmd runs commands. Set them per channel:

| You want | out | in | cmd |
| --- | --- | --- | --- |
| Everything in one channel | on | on | on |
| Chat mirror, no control | on | on | off |
| Announcements plus commands, chatter stays out | on | off | on |
| Listen only | off | on | off |
| Control room, commands only | off | off | on |

```
/dscbridge status
/dscbridge channels
/dscbridge channel add <name> <server-id> <channel-id>
/dscbridge channel remove <name>
/dscbridge channel set <name> out|in|cmd <on|off>
/dscbridge channel owners <name> <id...> | inherit
/dscbridge channel prefix <name> <char> | default
/dscbridge channel test <name>
```

A fresh route starts safe: chat flows, commands stay off until you say
otherwise. `test` posts one probe line and reports back, which is the
fastest way to check a channel id you pasted. It checks reachability
only, not the switches. `status` shows the gateway, the session, and
every route's last send. Every change saves `settings.toml` at once.
Owners and the prefix can differ per channel or inherit the global ones.

### Examples

Type these into the MCC console. A typical server ends up with three
routes: a mirror for everyone, a quiet announcements channel, and a
locked control room.

```
/dscbridge channel add mirror 111222333444555666 777888999000111222
/dscbridge channel set mirror cmd off
/dscbridge channel test mirror
```

The mirror relays chat both ways and runs nothing. Next, announcements:
game chat goes out, chatter stays out, but commands stay on so moderators
can steer the bot from there.

```
/dscbridge channel add announce 111222333444555666 888999000111222333
/dscbridge channel set announce in off
/dscbridge channel set announce cmd on
/dscbridge channel test announce
```

The control room runs commands and nothing else. Give it tighter owners
and a prefix that never collides with normal chat:

```
/dscbridge channel add control 111222333444555666 999000111222333444
/dscbridge channel set control out off
/dscbridge channel set control in off
/dscbridge channel set control cmd on
/dscbridge channel owners control 123456789012345678
/dscbridge channel prefix control \
/dscbridge channel test control
```

From Discord the same commands work with the route's prefix. In
#control with the backslash prefix, `/dscbridge status` becomes
`\dscbridge status`, and `.health` becomes `\health`.

Check the whole table any time:

```
/dscbridge channels
/dscbridge status
```

### DMs

```
/dscbridge dm on|off
/dscbridge dm messages on|off
/dscbridge dm <owner> <text>
```

`dm on` lets authorized owners send commands through direct messages. `dm messages on` also sends game chat to those owners.

Discord can reject a direct message because of the recipient's privacy settings or the bot's available contact context. Check the shared server and its direct-message settings. Send a message to the bot to check that communication works. Do not assume that a bot can always initiate a direct message.

Whispers and teleport requests can send separate owner notifications. Set `DmOwnersOn` to `["whisper", "teleport"]` for both. Watched whispers skip the channels entirely.

### What it posts on its own

Connect and disconnect notices go to every channel that takes game chat.
Death posts a red embed with block coordinates, unless `DeathEmbed` is
off. Gaining or losing an effect posts one line. Death and respawn move
effects around behind the scenes. The bridge stays silent for those, so
a dimension hop never spams the channel. Effects lasting a day or more
show as infinite, in MCC and here.

Map renders arrive as images. With no destination they fan out like
chat, except a render asked from Discord answers back where it was
asked. Name recipients in the Map plugin's `Discord_Target` instead:
`route:name` for a channel, `dm:owner-id` for a DM, comma-separated for
several. A named route gets the image even with its chat-out switch
off.

### Troubleshooting

The bot connects but hears nothing. The Message Content intent is off.
Flip it on in the portal and restart MCC.

If a direct message fails, check the recipient's privacy settings and shared-server access. Send a message to the bot to test the connection. An HTTP 403 means Discord refused the request. Read the provider error before changing bridge settings.

The probe never arrives. The channel id is wrong, or the bot cannot see
the channel. Check the id and the channel permissions.

The plugin disables itself at startup. `Token` is empty or the `env:`
link points nowhere. The log names the variable.

A channel answers "does not run commands". That route has commands off.
Run `/dscbridge channel set <name> cmd on`.

## When it does not work

1. Enter `/plugins info discord-bridge`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable discord-bridge`. To remove its package while retaining user data, enter `/plugins uninstall discord-bridge`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
