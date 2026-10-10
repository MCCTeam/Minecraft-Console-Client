# Script scheduler

Runs MCC commands at login, a dialog event, a time of day, or a repeating interval.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `script-scheduler` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/ScriptScheduler). It is not built into MCC. Its declared game/resource usage is `commands`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install script-scheduler@official 2.0.2
/plugins enable script-scheduler
/plugins info script-scheduler
```

1. Check that `/plugins list` shows `script-scheduler` as loaded.
2. Open `plugins/userdata/script-scheduler/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload script-scheduler`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
[[TaskList]]
Task_Name = "Join greeting"
Trigger_On_First_Login = false
Trigger_On_Login = true
Trigger_On_Dialog = false
Action = "send Hello from MCC"
```

Reload the plugin and reconnect to your controlled server. Check that it sends the greeting once for that login.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `TaskList.Task_Name` | `"Task Name 1"` | See the operation details below. |
| `TaskList.Trigger_On_First_Login` | `false` | See the operation details below. |
| `TaskList.Trigger_On_Login` | `false` | See the operation details below. |
| `TaskList.Trigger_On_Dialog` | `false` | Fire when the server shows a dialog (1.21.6+), in configuration or in play. |
| `TaskList.Dialog_Match` | `""` | Only fire when the dialog title or one of its input keys contains this text; empty matches every dialog. |
| `TaskList.Action` | `"send /hello"` | See the operation details below. |
| `TaskList.Trigger_On_Times.Enable` | `true` | Fire at the times listed below. |
| `TaskList.Trigger_On_Times.Times` | `[ "14:00:00" ]` | Times of day, as "HH:mm:ss" strings. |
| `TaskList.Trigger_On_Interval.Enable` | `true` | Fire repeatedly, with a random delay between MinTime and MaxTime. |
| `TaskList.Trigger_On_Interval.MinTime` | `3.6` | See the operation details below. |
| `TaskList.Trigger_On_Interval.MaxTime` | `4.8` | See the operation details below. |
| `TaskList.Task_Name` | `"Task Name 2"` | See the operation details below. |
| `TaskList.Trigger_On_First_Login` | `false` | See the operation details below. |
| `TaskList.Trigger_On_Login` | `true` | See the operation details below. |
| `TaskList.Trigger_On_Dialog` | `false` | Fire when the server shows a dialog (1.21.6+), in configuration or in play. |
| `TaskList.Dialog_Match` | `""` | Only fire when the dialog title or one of its input keys contains this text; empty matches every dialog. |
| `TaskList.Action` | `"send /login pass"` | See the operation details below. |
| `TaskList.Trigger_On_Times.Enable` | `false` | See the operation details below. |
| `TaskList.Trigger_On_Times.Times` | `[ ]` | See the operation details below. |
| `TaskList.Trigger_On_Interval.Enable` | `false` | See the operation details below. |
| `TaskList.Trigger_On_Interval.MinTime` | `1.0` | See the operation details below. |
| `TaskList.Trigger_On_Interval.MaxTime` | `10.0` | See the operation details below. |

## Requirements and limits

Actions are MCC commands, not inline Beacon code. Use an appropriate script command to call a `.bcn` file. There is no disconnect trigger. Times use the machine local clock. A dialog set command can echo a password to logs. Prefer Beacon dialog APIs for credentials.

## Detailed operation

Use a Beacon script for confidential dialog answers. `dialog set` prints the staged value and can expose it in logs.

## When it does not work

1. Enter `/plugins info script-scheduler`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable script-scheduler`. To remove its package while retaining user data, enter `/plugins uninstall script-scheduler`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
