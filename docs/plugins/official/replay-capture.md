# Replay capture

Records protocol session data in a ReplayMod archive.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `replay-capture` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/ReplayCapture). It is not built into MCC. Its declared game/resource usage is `session`, `files`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install replay-capture@official 2.0.2
/plugins enable replay-capture
/plugins info replay-capture
```

1. Check that `/plugins list` shows `replay-capture` as loaded.
2. Open `plugins/userdata/replay-capture/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload replay-capture`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Backup_Interval = 300.0
```

Join a controlled server and move briefly. Run `/replay save` to write a snapshot. Run `/replay stop` to finish the recording. Open the archive in a compatible ReplayMod client.

```text
/replay save
/replay stop
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Backup_Interval` | `300.0` | Seconds between backup snapshots of the in-progress recording. -1 disables backups. |

## Requirements and limits

Recording uses disk space. A save command copies the recording without stopping it. A stop command finishes it. The ReplayMod viewer must support the recorded protocol version. Packets can include private session content.

## When it does not work

1. Enter `/plugins info replay-capture`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable replay-capture`. To remove its package while retaining user data, enter `/plugins uninstall replay-capture`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
