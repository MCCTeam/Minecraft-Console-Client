# Player list logger

Records online players and join/leave changes in a file.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `player-list-logger` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/PlayerListLogger). It is not built into MCC. Its declared game/resource usage is `files`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install player-list-logger@official 2.0.2
/plugins enable player-list-logger
/plugins info player-list-logger
```

1. Check that `/plugins list` shows `player-list-logger` as loaded.
2. Open `plugins/userdata/player-list-logger/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload player-list-logger`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
File = "playerlog.txt"
Delay = 60.0
```

Connect another player to the test server. Check `data/playerlog.txt` after the next logging interval.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `File` | `"playerlog.txt"` | Log file inside this plugin's data/ folder. |
| `Delay` | `60.0` | Seconds between two records. Clamped to at least 1. |

## Requirements and limits

The server player list determines what MCC can observe. The interval is seconds and clamps to at least one. This is not a server-wide record of players hidden from the client.

## When it does not work

1. Enter `/plugins info player-list-logger`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable player-list-logger`. To remove its package while retaining user data, enter `/plugins uninstall player-list-logger`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
