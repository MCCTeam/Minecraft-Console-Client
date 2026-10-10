# Items collector

Walks towards nearby dropped items to collect them.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `items-collector` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/ItemsCollector). It is not built into MCC. Its declared game/resource usage is `movement`, `entities`, `terrain`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install items-collector@official 2.0.2
/plugins enable items-collector
/plugins info items-collector
```

1. Check that `/plugins list` shows `items-collector` as loaded.
2. Open `plugins/userdata/items-collector/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload items-collector`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Collect_All_Item_Types = true
Collection_Radius = 6.0
Always_Return_To_Start = true
```

Drop an ordinary item nearby on a controlled server. Run `/itemscollector start`. Check that the player reaches it and the inventory receives it.

```text
/itemscollector start
/itemscollector stop
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Collect_All_Item_Types` | `true` | Collect every dropped item type. Leave true: the whitelist is inert on this core. |
| `Items_Whitelist` | `[ "Diamond", "NetheriteIngot" ]` | Item whitelist. Not evaluated on this core because dropped-item contents are not decoded. |
| `Delay_Between_Tasks` | `300` | Milliseconds between collection cycles. Clamped to at least 100. |
| `Collection_Radius` | `30.0` | How far to look for dropped items, in blocks. |
| `Always_Return_To_Start` | `true` | Walk back to the starting position when nothing is in range. |
| `Prioritize_Clusters` | `false` | Order targets around the densest cluster instead of nearest first. |

## Requirements and limits

Needs entities, terrain, and movement/pathfinding. The item whitelist is inert in this implementation. Turning off `Collect_All_Item_Types` logs a warning and still collects all types. Do not use that switch to filter valuable or unwanted items.

## When it does not work

1. Enter `/plugins info items-collector`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable items-collector`. To remove its package while retaining user data, enter `/plugins uninstall items-collector`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
