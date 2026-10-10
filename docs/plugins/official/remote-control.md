# Remote control

Accepts MCC commands sent in private messages by configured bot owners.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `remote-control` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/RemoteControl). It is not built into MCC. Its declared game/resource usage is `chat`, `commands`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install remote-control@official 2.0.2
/plugins enable remote-control
/plugins info remote-control
```

1. Check that `/plugins list` shows `remote-control` as loaded.
2. Open `plugins/userdata/remote-control/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload remote-control`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
AutoTpaccept = false
AutoTpaccept_Everyone = false
```

Add your trusted player name to `permissions.bot-owners` in `client.toml`. Send the client a private message containing `health`. Check the response and command result.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `AutoTpaccept` | `true` | Automatically accept teleport requests (still restricted to bot owners by default). |
| `AutoTpaccept_Everyone` | `false` | Accept teleport requests from ANY player, not only from the bot owners. |

## Requirements and limits

Any authorized owner can run MCC commands with the client privileges. Chat classification must recognize the server whisper format. `AutoTpaccept_Everyone = true` removes the owner restriction for teleport requests. Leave it false for private control.

## When it does not work

1. Enter `/plugins info remote-control`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable remote-control`. To remove its package while retaining user data, enter `/plugins uninstall remote-control`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
