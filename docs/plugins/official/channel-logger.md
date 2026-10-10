# Channel logger

Channel Logger is not included in the official Marketplace. This page documents the older standalone plugin.

Records inbound play-phase plugin-channel payloads to a log file.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `channel-logger` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/ChannelLogger). It is not built into MCC. Its declared game/resource usage is `files`, `channels`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

Import a compiled package from your chosen publisher with the [plugin management guide](../managing.md). After it loads, enter:

```text
/plugins enable channel-logger
/plugins info channel-logger
```

1. Check that `/plugins list` shows `channel-logger` as loaded.
2. Open `plugins/userdata/channel-logger/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload channel-logger`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Follow_Announced = true
Channels = ["bungeecord:main"]
Max_Bytes = 256
File = "channels.log"
```

Connect to a server that sends the configured channel. Check `data/channels.log` for the channel name, payload length, hexadecimal bytes, and text.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Set to false to stop logging without unloading the plugin. |
| `Follow_Announced` | `true` | Register every channel the server announces with minecraft:register, as it announces it. |
| `Channels` | `[ ]` | Channels to register whether or not the server announces them, as full identifiers such as "bungeecord:main". These are registered before the connect, so the first message on one is not missed. |
| `Max_Bytes` | `256` | How many bytes of each payload to write. The line still reports the full length. |
| `File` | `"channels.log"` | The file under the plugin's data/ folder to append to. |

## Requirements and limits

This records inbound play-phase messages. It does not record login/configuration traffic or outgoing messages. A channel that the server never sends produces no entry. Pre-1.13 non-namespaced channels do not fit this registration model.

## When it does not work

1. Enter `/plugins info channel-logger`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable channel-logger`. To remove its package while retaining user data, enter `/plugins uninstall channel-logger`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
