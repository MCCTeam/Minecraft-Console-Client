# Telegram bridge

Relays game chat to Telegram and accepts commands from authorized Telegram chats.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `telegram-bridge` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/TelegramBridge). It is not built into MCC. Its declared game/resource usage is `chat`, `network`, `commands`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install telegram-bridge@official 2.0.0
/plugins enable telegram-bridge
/plugins info telegram-bridge
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `telegram-bridge` as loaded.
2. Open `plugins/userdata/telegram-bridge/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload telegram-bridge`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Token = "env:MCC_TELEGRAM_TOKEN"
ChannelId = "2627844670"
Authorized_Chat_Ids = [2627844670]
```

Create a bot through BotFather. Set its token in the configurations `.env`. Send `.chatid` to the bot. Replace the example ID in both settings with that ID, then reload the plugin. Send `.health` from that chat.

```text
/tgbridge direction both
/tgbridge direction mc
/tgbridge direction telegram
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Token` | `""` | Bot token from @BotFather. Required; the plugin self-disables without it. |
| `ChannelId` | `""` | Chat or channel id the game chat is relayed to. Send '.chatid' to the bot to learn it. |
| `Authorized_Chat_Ids` | `[]` | Chat ids allowed to relay into the game and run commands. Empty authorizes nobody. |
| `Message_Send_Timeout` | `3` | Seconds a Telegram send may take before it is abandoned. Below 1 falls back to 3. |
| `PrivateMessageFormat` | `"*(Private Message)* {username}: {message}"` | Message templates. Placeholders: {username} {message} {timestamp} |
| `PublicMessageFormat` | `"{username}: {message}"` | See the operation details below. |
| `TeleportRequestMessageFormat` | `"A new Teleport Request from **{username}**!"` | See the operation details below. |

## Requirements and limits

An empty authorized chat list permits no control. The bridge blocks quit/exit commands. A group chat ID authorizes that chat rather than an individual sender. Do not authorize a public group unless everyone there may control MCC.

## Detailed operation

### Talk to it

Type a plain line and the game hears it as chat. Start a line with a
dot and MCC runs the rest as a command. The answer comes back as a
reply to your message.

```
Hello from Telegram
.health
.chunk status
.chatid
```

Three inputs get special handling. The bridge ignores `/start`, so the
Telegram Start button stays quiet. `.chatid` reports the id of the
current chat. The bridge refuses a command with `quit` or `exit` in
it. You cannot stop the client from chat.

Commands run as internal MCC commands with your Telegram chat as the
source. Most answers print what the MCC console would print, with the
Minecraft color codes removed. Plain chat lines lose their codes on the
way out for the same reason. If MCC has no session open, the bridge
drops chat lines and commands still run where they make sense.

### Control the direction

```
/tgbridge direction <both|mc|telegram>
```

`both` relays each way. `mc` relays Telegram into Minecraft only and
sends nothing out. `telegram` relays Minecraft into Telegram only and
ignores chat coming back, but dot commands still work. The setting
lasts until you change it or reload the plugin. It resets to `both`
on reload.

### What it posts on its own

Connect and disconnect notices go to the configured chat. Public chat,
whispers, and teleport requests use your message templates below.
Other server lines arrive verbatim.

Map renders arrive as documents. Turn on `Send_Rendered_To_Telegram`
in the Map plugin and its renders go to this same chat. The Map page
has the details.

### Troubleshooting

The plugin disables itself at startup. `Token` is empty, still the
placeholder, or the `env:` link points to a variable that is not set.
The log names the cause.

Nothing arrives in Telegram. `ChannelId` is empty or wrong. Send
`.chatid` again and compare the numbers.

Telegram messages never reach the game. The chat id is missing from
`Authorized_Chat_Ids`, or the direction is `telegram`. Add the id and
set the direction to `both`.

The probe command answers with refusal. Same cause. Only listed chats
may drive the bridge.

Sends time out. `Message_Send_Timeout` is too low for your link, or
Telegram is slow. Raise it by a second or two.

## When it does not work

1. Enter `/plugins info telegram-bridge`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable telegram-bridge`. To remove its package while retaining user data, enter `/plugins uninstall telegram-bridge`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
