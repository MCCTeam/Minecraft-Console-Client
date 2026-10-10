# Discord rich presence

Displays selected session information on your Discord profile.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `discord-rpc` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/DiscordRpc). It is not built into MCC. Its declared game/resource usage is `session`, `network`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install discord-rpc@official 2.0.2
/plugins enable discord-rpc
/plugins info discord-rpc
```

1. Check that `/plugins list` shows `discord-rpc` as loaded.
2. Open `plugins/userdata/discord-rpc/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload discord-rpc`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
ApplicationId = "YOUR_DISCORD_APPLICATION_ID"
ShowCoordinates = false
ShowServerAddress = false
PresenceDetails = "Playing Minecraft"
PresenceState = "{dimension}"
```

Keep the Discord desktop app running on the same machine. Join a server. Check your activity profile after one update interval.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `ApplicationId` | `"1485448268023337052"` | Discord Application ID from https://discord.com/developers/applications. Required. |
| `PresenceDetails` | `"Playing on {server_host}:{server_port}"` | First presence line. Placeholders: {server_host} {server_port} {username} {health} {max_health} {food} {dimension} {gamemode} {x} {y} {z} {player_count} {protocol} |
| `PresenceState` | `"{dimension} - HP: {health}/{max_health}"` | Second presence line. Same placeholders as PresenceDetails. |
| `LargeImageKey` | `"mcc_icon"` | Large image asset key uploaded to the Discord application. Empty disables the image. |
| `LargeImageText` | `"Minecraft Console Client"` | Large image hover text. Placeholders are expanded. |
| `SmallImageKey` | `""` | Small image asset key. Empty disables the small image. |
| `SmallImageText` | `""` | Small image hover text. Placeholders are expanded. |
| `ShowServerAddress` | `true` | Publish the server address. When false, {server_host} shows Hidden and {server_port} shows ****. |
| `ShowCoordinates` | `true` | Publish the player coordinates. When false, {x} {y} {z} show ?. |
| `ShowHealth` | `true` | Publish health and food. When false, {health} {max_health} {food} show ?. |
| `ShowDimension` | `true` | Publish the dimension. When false, {dimension} shows Hidden. |
| `ShowGamemode` | `true` | Publish the game mode. When false, {gamemode} shows Hidden. |
| `ShowElapsedTime` | `true` | Show the elapsed session time on the presence. |
| `ShowPlayerCount` | `true` | Publish the online player count as a Discord party size. |
| `UpdateIntervalSeconds` | `10` | Seconds between presence refreshes. Below 1 falls back to 10. |

## Requirements and limits

Requires a valid application ID and local Discord IPC. A browser-only Discord session cannot provide that connection. This does not use a bot token. Review privacy settings before sharing coordinates or a private server address.

## Detailed operation

### Make it yours

Two lines make the presence. Both accept placeholders:

```
PresenceDetails = "Playing on {server_host}:{server_port}"
PresenceState = "{dimension} - HP: {health}/{max_health}"
```

| Placeholder | Shows |
| --- | --- |
| `{server_host}` | server address |
| `{server_port}` | server port |
| `{username}` | your game name |
| `{health}` | current health |
| `{max_health}` | full health |
| `{food}` | food level |
| `{dimension}` | overworld, nether, or end |
| `{gamemode}` | survival, creative, and friends |
| `{x}` `{y}` `{z}` | your coordinates |
| `{player_count}` | players online |
| `{protocol}` | game version number |

A hunter might write `"{username} at {x}, {y}, {z} in {dimension}"`.
A minimalist writes `"{server_host}"` and nothing else.

Pictures come from Art Assets. Open your application, Rich Presence,
Art Assets, and upload an image of at least 512 by 512 pixels. Name it
`mcc_icon` and set `LargeImageKey` to the same name. The names must
match exactly. `LargeImageText` is the hover text. Leave `SmallImageKey`
empty until you want a second badge.

### Privacy switches

Each `Show` setting masks one placeholder group when false:

| Off | Masks |
| --- | --- |
| `ShowServerAddress` | address reads Hidden, port reads **** |
| `ShowCoordinates` | coordinates read ? |
| `ShowHealth` | health and food read ? |
| `ShowDimension` | dimension reads Hidden |
| `ShowGamemode` | game mode reads Hidden |

`ShowElapsedTime` adds a session timer. `ShowPlayerCount` shares the
online count as a party size. `UpdateIntervalSeconds` sets the refresh
rate and defaults to 10.

### Troubleshooting

Nothing shows on the profile. Check three things in order: the Discord
desktop app runs on the same machine, the Application ID matches, and
Discord shares your activity. That last one hides under User Settings,
Activity Privacy. Set your status to Online. Check the activity privacy setting if the presence is hidden.

Text shows but the picture does not. The asset name and `LargeImageKey`
differ, or the upload is still processing. Set the key empty, reload,
and check the text alone. Then fix the name.

The plugin disables itself at startup. `ApplicationId` is empty. The log
says so.

## When it does not work

1. Enter `/plugins info discord-rpc`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable discord-rpc`. To remove its package while retaining user data, enter `/plugins uninstall discord-rpc`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
