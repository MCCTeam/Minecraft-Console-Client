# Follow player

Follows a tracked player while maintaining a minimum distance.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `follow-player` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/FollowPlayer). It is not built into MCC. Its declared game/resource usage is `movement`, `entities`, `terrain`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install follow-player@official 2.0.2
/plugins enable follow-player
/plugins info follow-player
```

1. Check that `/plugins list` shows `follow-player` as loaded.
2. Open `plugins/userdata/follow-player/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload follow-player`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Update_Limit = 1.5
Stop_At_Distance = 3.0
```

Connect two clients to a controlled server. Run `/follow start Alex` with the other player actual name. Walk slowly and check that MCC follows.

```text
/follow start <player> [-f]
/follow stop
```

`-f` selects direct physics movement instead of planner navigation. It does not provide a general unsafe-path option. Omit it for the first test.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Update_Limit` | `1.5` | Minimum seconds between two re-paths toward the followed player. |
| `Stop_At_Distance` | `3.0` | Stop moving once the followed player is closer than this many blocks. |

## Requirements and limits

Needs entities and terrain. Pathfinding improves route selection. Following acquires the movement lease. Stop it before another plugin needs movement. Teleports, unloaded terrain, and fast movement can prevent the client from following.

## When it does not work

1. Enter `/plugins info follow-player`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable follow-player`. To remove its package while retaining user data, enter `/plugins uninstall follow-player`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
