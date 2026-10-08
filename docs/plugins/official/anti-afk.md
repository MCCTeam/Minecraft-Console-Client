# Anti-AFK

Sends a periodic action so a server can observe activity from the client.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `anti-afk` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/AntiAFK). It is not built into MCC. Its declared game/resource usage is `chat`, `movement`, `terrain`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install anti-afk@official 2.0.0
/plugins enable anti-afk
/plugins info anti-afk
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `anti-afk` as loaded.
2. Open `plugins/userdata/anti-afk/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload anti-afk`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Command = ""
Use_Sneak = true
Use_Terrain_Handling = false
[Delay]
min = 60.0
max = 60.0
```

Wait for one interval. The player changes its sneak state.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Command` | `"/ping"` | Command or chat line sent to stay non-idle. Leave empty to send nothing. |
| `Use_Sneak` | `false` | Toggle sneak on each action. |
| `Use_Terrain_Handling` | `false` | Walk to a random nearby spot instead of only sending the command. Needs the terrain feature. |
| `Walk_Range` | `5` | Maximum X/Z offset of the random walk goal, in blocks. Must be positive. |
| `Walk_Retries` | `20` | How many walk goals to try before falling back to sending the command. |
| `Delay.min` | `60.0` | See the operation details below. |
| `Delay.max` | `60.0` | See the operation details below. |

## Requirements and limits

A server decides which actions count as activity. This cannot guarantee that the server keeps the connection. The bundled `/ping` command works only on servers that provide it. Terrain walking needs terrain data and can conflict with other movement automation.

## When it does not work

1. Enter `/plugins info anti-afk`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable anti-afk`. To remove its package while retaining user data, enter `/plugins uninstall anti-afk`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
