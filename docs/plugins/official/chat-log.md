# Chat log

Writes selected chat and internal messages to a file.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `chat-log` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/ChatLog). It is not built into MCC. Its declared game/resource usage is `chat`, `files`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install chat-log@official 2.0.2
/plugins enable chat-log
/plugins info chat-log
```

1. Check that `/plugins list` shows `chat-log` as loaded.
2. Open `plugins/userdata/chat-log/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload chat-log`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Add_DateTime = true
Log_File = "chatlog-%username%-%serverip%.txt"
Filter = "messages"
```

Send a test chat line. Check the plugin data directory for a timestamped entry.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Add_DateTime` | `true` | Prepend a yyyy-MM-dd HH:mm:ss timestamp to every logged line. |
| `Log_File` | `"chatlog-%username%-%serverip%.txt"` | Log file inside this plugin's data/ folder. %username% and %serverip% are expanded. |
| `Filter` | `"messages"` | all, messages, chat, private_chat or internal_msg. |

## Requirements and limits

`Filter` selects `all`, `messages`, `chat`, `private_chat`, or `internal_msg`. Treat recorded private messages as private data. Relative log paths belong under this plugin data directory.

## When it does not work

1. Enter `/plugins info chat-log`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable chat-log`. To remove its package while retaining user data, enter `/plugins uninstall chat-log`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
