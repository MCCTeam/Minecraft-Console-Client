# Auto-fishing

Casts a rod, detects a bite, reels the rod, and repeats.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-fishing` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/AutoFishing). It is not built into MCC. Its declared game/resource usage is `entities`, `inventory`, `interaction`, `movement`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install auto-fishing@official 2.0.2
/plugins enable auto-fishing
/plugins info auto-fishing
```

1. Check that `/plugins list` shows `auto-fishing` as loaded.
2. Open `plugins/userdata/auto-fishing/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-fishing`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Mainhand = true
Auto_Start = false
Cast_Delay = 0.4
Fishing_Timeout = 300.0
Enable_Velocity_Detection = true
Enable_Sound_Detection = true
```

Hold a fishing rod and face open water. Run `/autofishing start`. Run `/autofishing status` after several catches. Run `/autofishing stop` to finish.

```text
/autofishing start
/autofishing stop
/autofishing status
/autofishing status clear
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Antidespawn` | `false` | Recast when the bobber despawns instead of waiting forever. |
| `Mainhand` | `true` | Cast with the main hand. false casts with the off hand. |
| `Auto_Start` | `true` | Start fishing automatically after joining or respawning. |
| `Cast_Delay` | `0.4` | Seconds to wait before every cast. |
| `Fishing_Delay` | `3.0` | Seconds to wait after joining or respawning before the first cast. |
| `Fishing_Timeout` | `300.0` | Seconds without a bite before recasting. |
| `Durability_Limit` | `2.0` | Minimum remaining rod durability, out of 64. |
| `Auto_Rod_Switch` | `true` | Swap in a fresh rod from the inventory when the held one is worn out. |
| `Stationary_Threshold` | `0.001` | Horizontal bobber movement below this counts as stationary. |
| `Hook_Threshold` | `0.2` | Vertical bobber movement above this while stationary is a bite. |
| `Enable_Velocity_Detection` | `true` | Also detect the bite from the bobber velocity. |
| `Velocity_Hook_Threshold` | `-0.2` | Bobber Y velocity at or below this is a bite. Always negative. |
| `Enable_Sound_Detection` | `true` | Detect bites through splash sounds when the protocol supplies sound names. |
| `Sound_Distance` | `5.0` | Maximum splash distance from the tracked bobber, in blocks. |
| `Detection_Warmup` | `1.0` | Seconds after the bobber lands before bite detection arms. |
| `Log_Fish_Bobber` | `false` | Trace every bobber spawn, move and despawn. |
| `Enable_Move` | `false` | Walk the Movements list between casts. |
| `Movements.facing.yaw` | `12.34` | See the operation details below. |
| `Movements.facing.pitch` | `-23.45` | See the operation details below. |

## Requirements and limits

Needs entity and inventory data for full operation. Source code supports sound detection, although the bundled settings comments still call it inert. Some protocol versions do not provide sound names. The plugin then retains motion detection and logs a warning.

## When it does not work

1. Enter `/plugins info auto-fishing`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-fishing`. To remove its package while retaining user data, enter `/plugins uninstall auto-fishing`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
