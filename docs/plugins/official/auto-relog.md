# Auto-relog

Attempts a new connection after selected unexpected disconnects.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-relog` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/AutoRelog). It is not built into MCC. Its declared game/resource usage is `session`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install auto-relog@official 2.0.0
/plugins enable auto-relog
/plugins info auto-relog
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `auto-relog` as loaded.
2. Open `plugins/userdata/auto-relog/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-relog`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Retries = 3
Ignore_Kick_Message = false
Kick_Messages = ["Server is restarting"]
[Delay]
min = 3.0
max = 3.0
```

On a controlled server, disconnect the client with a matching reason. Check that it retries after the delay.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Retries` | `3` | Rejoin attempts before giving up. -1 means unlimited. The budget resets after a stable join. |
| `Ignore_Kick_Message` | `false` | Rejoin on any unexpected disconnect, ignoring Kick_Messages. |
| `Kick_Messages` | `[ "Connection has been lost", "Server is restarting", "Server is full", "Too Many people" ]` | Case-insensitive substrings of the kick message that trigger a rejoin. |
| `Delay.min` | `3.0` | See the operation details below. |
| `Delay.max` | `3.0` | See the operation details below. |

## Requirements and limits

The core reconnect supervisor runs first. This plugin waits while that supervisor reconnects. `Retries = -1` means unlimited attempts. A rejected login or changed account can still prevent a connection.

## When it does not work

1. Enter `/plugins info auto-relog`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-relog`. To remove its package while retaining user data, enter `/plugins uninstall auto-relog`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
