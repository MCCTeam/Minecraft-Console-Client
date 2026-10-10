# Farmer

Harvests supported crops, collects drops, replants, and optionally uses bone meal and storage.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `farmer` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/Farmer). It is not built into MCC. Its declared game/resource usage is `terrain`, `inventory`, `entities`, `physics`, `movement`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install farmer@official 2.0.2
/plugins enable farmer
/plugins info farmer
```

1. Check that `/plugins list` shows `farmer` as loaded.
2. Open `plugins/userdata/farmer/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload farmer`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Delay_Between_Tasks = 1.0
Verbose_Debug = false
```

Prepare a small wheat field and seeds. Run the area command below with your own field coordinates. Check harvesting and replanting, then stop it.

```text
/farmer start wheat area 100 63 200 110 67 210
/farmer storage seeds name Seeds
/farmer storage crops name Crops
/farmer storage bonemeal name Bonemeal
/farmer storage list
/farmer stop
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Delay_Between_Tasks` | `1.0` | Seconds to wait between farming cycles. Clamped to at least 1.0. |

## Requirements and limits

Needs terrain, inventory, entities, and physics. Pathfinding improves navigation when available. Nether wart does not use bone meal. Cane, bamboo, and cactus keep their bottom block. Use a bounded area before testing a large farm.

## Detailed operation

Farms one crop for you: harvests what's ripe, picks up the drops,
replants, and feeds the young ones bone meal. Then it loops until you
stop it. Use area mode. It farms exactly the box you give it, unlike
radius mode, which draws a circle around the bot and grabs eight blocks
above and below, whether your farm is there or not.

### Quick start

1. Stand near your farm and note two opposite corners of the field.
   Press F3 and read the XYZ numbers, or stand on each corner and read
   them from `/blockinfo`.
2. Tell Farmer the crop and the box:
   `/farmer start wheat area 100 63 200 132 70 240`. Corner order does
   not matter. Both corners count as inside.
3. Give it chests. Label a chest with a sign that says Seeds, Crops, or
   Bonemeal, or point at coordinates directly (see below). Without
   chests it still farms, but it carries everything and never restocks.
4. Walk away. It harvests, collects, replants, bone meals, and repeats.
5. `/farmer stop` ends the run.

### Commands

```
/farmer start <crop> area <x1> <y1> <z1> <x2> <y2> <z2>
/farmer start <crop> radius [1-150]
/farmer storage <bonemeal|seeds|crops> name <label>
/farmer storage <bonemeal|seeds|crops> position <x> <y> <z>
/farmer storage <bonemeal|seeds|crops> position clear
/farmer storage margin <blocks>
/farmer storage list
/farmer stop
```

Crop names complete as you type. `sugar_cane`, `nether_wart`,
`cocoa_beans`, `wheat_seeds`, and `melons_and_pumpkins` all work, and
short forms like `sugarcane` and `cocoa` do too. For melons and
pumpkins plant-side, use `melon`, `pumpkin`, or `melons_and_pumpkins`.
Radius defaults to 30 and tops out at 150. Boxes top out at 4,000,000
cells, so a typo cannot scan the whole world.

### Examples

Wheat field, exact box, chests by sign labels:

```
/farmer start wheat area 100 63 200 132 70 240
/farmer storage seeds name Seeds
/farmer storage crops name Crops
/farmer storage bonemeal name Bonemeal
/farmer storage list
```

Sugar cane along a wall, chest at fixed coordinates:

```
/farmer start sugar_cane area 100 63 200 100 70 260
/farmer storage crops position 120 64 220
```

Nether wart on soul sand (no bone meal needed, wart ignores it):

```
/farmer start nether_wart area 50 40 50 80 45 80
```

Cocoa on jungle logs:

```
/farmer start cocoa area 100 63 200 132 72 240
```

Done for the day: `/farmer stop`.

### Storage

Three chests cover everything: Seeds holds planting material, Crops
takes the harvest, Bonemeal feeds the bone meal passes. Farmer takes
normal chests, trapped chests, and barrels. Put a sign with the label
on any face of the chest, or give exact coordinates. Coordinates win
when both exist. `storage margin` sets how far past the farm it looks
for a labelled chest. `storage list` shows what it found.

Two fallbacks save complaining trips: wheat, beetroot, melon, and
pumpkin seeds fall back to the Crops chest when Seeds runs dry, and
carrot, potato, wart, cane, bamboo, cactus, and cocoa replant from
their own harvest. Farmer reports a missing or unreachable chest and
keeps farming.

### Crops

Wheat, beetroot, carrot, potato: harvested and replanted. Nether wart:
same, on soul sand, and it skips bone meal. Melon and pumpkin: the bot
picks the fruit and bone meals the stem, which stays planted. `melons_and_pumpkins`
farms both in one area. Sugar cane, bamboo, cactus: the second block
snaps off, the base stays, no replanting and no bone meal. Cocoa:
ripe pods only, replanted on the same jungle log face.

Beetroot needs Minecraft 1.9 or newer, bamboo 1.14 or newer. Older
servers refuse the command instead of farming air.

## When it does not work

1. Enter `/plugins info farmer`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable farmer`. To remove its package while retaining user data, enter `/plugins uninstall farmer`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
