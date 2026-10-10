# Auto-dig

Breaks a selected block or a configured list of block positions.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-dig` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/AutoDig). It is not built into MCC. Its declared game/resource usage is `terrain`, `inventory`, `interaction`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install auto-dig@official 2.0.2
/plugins enable auto-dig
/plugins info auto-dig
```

1. Check that `/plugins list` shows `auto-dig` as loaded.
2. Open `plugins/userdata/auto-dig/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-dig`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Mode = "lookat"
Auto_Tool_Switch = false
List_Type = "whitelist"
Blocks = ["Cobblestone"]
```

Face a nearby cobblestone block on a controlled server. Run `/autodig start`. Check the block change, then run `/autodig stop`.

```text
/autodig start
/autodig stop
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Auto_Tool_Switch` | `false` | Switch to the tool recommended for the block before digging (needs inventory handling). |
| `Apply_Efficiency_Enchantments` | `true` | Kept for compatibility. The new dig action has no client-side mining timer, so this is inert. |
| `Apply_Haste_Effects` | `true` | Kept for compatibility. The new dig action has no client-side mining timer, so this is inert. |
| `Durability_Limit` | `2` | Never pick a tool with less remaining durability than this. 0 disables the check. |
| `Drop_Low_Durability_Tools` | `false` | Drop the worn-out tool after switching away from it. |
| `Mode` | `"lookat"` | lookat = dig the crosshair block, fixedpos = dig Locations, both = crosshair limited to Locations. |
| `Location_Order` | `"distance"` | distance = nearest position first, index = config order. |
| `Auto_Start_Delay` | `3.0` | Seconds to wait after joining or respawning before the first dig. |
| `Dig_Timeout` | `60.0` | Seconds before a dig that never completes re-arms the state machine. |
| `Log_Block_Dig` | `true` | Log every dig and every reason for not digging. |
| `List_Type` | `"whitelist"` | whitelist = only the listed blocks, blacklist = everything except the listed blocks. |
| `Blocks` | `[ "Cobblestone", "Stone" ]` | Block names. The legacy enum spelling (Cobblestone), a bare id or a namespaced id all work. |
| `Locations.x` | `123.5` | See the operation details below. |
| `Locations.y` | `64.0` | See the operation details below. |
| `Locations.z` | `234.5` | See the operation details below. |
| `Locations.x` | `124.5` | See the operation details below. |
| `Locations.y` | `63.0` | See the operation details below. |
| `Locations.z` | `235.5` | See the operation details below. |

## Requirements and limits

Needs terrain handling. Tool switching also needs inventory handling. Efficiency and haste switches are inert in this plugin, because it does not own a separate mining timer. The server controls the actual break behavior.

## When it does not work

1. Enter `/plugins info auto-dig`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-dig`. To remove its package while retaining user data, enter `/plugins uninstall auto-dig`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
