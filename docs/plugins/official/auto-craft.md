# Auto-craft

Crafts queued items and transfers ingredients and results through registered storage.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-craft` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/AutoCraft). It is not built into MCC. Its declared game/resource usage is `inventory`, `interaction`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install auto-craft@official 2.0.0
/plugins enable auto-craft
/plugins info auto-craft
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `auto-craft` as loaded.
2. Open `plugins/userdata/auto-craft/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-craft`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
BenchRadius = 8
OnFailure = "wait"
```

Label an input chest `Main` and an output chest `Finished`. Put coal and sticks in `Main`. Run the commands below. Check that the output contains torches.

```text
/autocraft add input chest "Main"
/autocraft add output chest "Finished"
/autocraft craft 64 torches
/autocraft status
/autocraft stop
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `BenchRadius` | `8` | How far around the bot to look for a crafting table (2-32). |
| `Bench` | `""` | Pinned bench as "x y z". Empty means the bot finds one itself. |
| `OnFailure` | `"abort"` | abort = stop when a material runs out, wait = park until it comes back. |

## Requirements and limits

Needs inventory handling, reachable storage, and a crafting table for 3x3 recipes. Keep other movement plugins stopped during the test. A protected or unreachable container can prevent progress.

## Detailed operation

Crafts items from your chests at a bench the bot finds itself. You register chests once, queue what you want in chat, and it pulls ingredients, crafts, and delivers the results.

There is no order file. Everything runs through `/autocraft`.

### The 60-second setup

Put a sign on a chest that reads `Main`, drop coal and sticks in it, put a sign reading `Finished` on a second chest, then:

```
/autocraft add input chest "Main"
/autocraft add output chest "Finished"
/autocraft craft 64 torches
```

The bot binds the nearest crafting table within 8 blocks, crafts in batches, and moves torches to `Finished`. Small 2x2 recipes skip the bench and use pockets.

### Chests

```
/autocraft add <input|output> <chest|barrel|shulker> <"sign"|x y z> [item]
/autocraft remove <input|output> <chest|barrel|shulker> <"sign"|x y z>
/autocraft chests
```

A sign name tracks the chest even after a rebuild, so prefer labels for anything permanent. Coordinates pin one block. A shulker has to be placed to count; one riding in inventory does not.

Leave off the item and the chest is shared: anything the recipe needs can come out of it. Name one and the chest is filtered to it:

```
/autocraft add input chest "Main"
/autocraft add input barrel 12 64 -30 iron_ingot
/autocraft add output shulker "Torches" torch
```

For one job the bot reads filtered chests first, then shared ones, nearest first. A locked, full, mined, or unreachable chest gets skipped for a while, not argued with. Withdrawals can split across chests, so 20 coal from one and 12 from the next still counts as one batch.

### Crafting

```
/autocraft craft 64 torches
/autocraft craft 2s "Stone Bricks"
/autocraft keep 128 torches
/autocraft queue
/autocraft cancel 2
/autocraft status
/autocraft stop
```

Counts accept stacks: `2s` is 128. `craft` runs once and stops. `keep` holds stock at the target: it pauses with `stock is full` when your pockets hold enough and picks back up when you take some. `status` always answers in one line what the bot does, what it waits on, and the fix.

Names are forgiving: case, spaces, underscores, and hyphens do not matter, and aliases like `sticks` or `workbench` work. A miss suggests: `No recipe called "toch". Did you mean: Torch?`

### Recipes

```
/autocraft recipes
/autocraft recipes rail
/autocraft info torches
/autocraft show "Steel Gear"
```

`recipes` lists everything known, `info` and `show` print the grid as plain ASCII that survives any console:

```
Torch x4  [any]
+---+---+
|coa|   |
+---+---+
|sti|   |
+---+---+
needs: coal x1, stick x1
```

The header carries yield per craft and station. `show` is the same grid; check `status` and `chests` before a big run to confirm supply.

### Bench

Table recipes need a 3x3 grid, so the bot scans around itself (default 8 blocks, cap 12 candidates per attempt), walks to the closest table it can stand next to, and binds the first one that actually opens. A bench that fails stays ignored for a minute while the next gets tried.

```
/autocraft bench radius 12
/autocraft bench status
/autocraft bench set 121 64 -31
/autocraft bench auto
/autocraft bench rescan
```

`set` pins one bench for laggy or protected areas. `auto` goes back to scanning. If nothing is in range the job parks with `Missing crafting bench` and resumes on its own when a table shows up, usually within seconds. The same file holds `BenchRadius` and `Bench` if you would rather set them in `settings.toml`.

### Custom recipes

Servers invent items, so `settings.toml` takes them:

```toml
[[CustomRecipes]]
Name = "Copper Coil"
Type = "table"
Result = "copper_coil"
Count = 2
Aliases = [ "coil" ]
Slots = [ "copper_ingot", "", "copper_ingot", "copper_ingot", "iron_ingot", "copper_ingot", "copper_ingot", "", "copper_ingot" ]
```

`Type` is `player`, `table`, or `any` (fits 2x2, crafted in pockets). A custom with the same name as a built-in wins. A bad entry fails that entry with a message, never the whole file.

### When it parks instead of dying

With `OnFailure = "wait"` a missing ingredient parks the job and rechecks every 30 seconds: `Waiting for iron: checked "Bulk", still need 16. Add more or run autocraft stop.` A full output parks the same way instead of dropping anything on the ground. `abort` stops the queue outright. `/autocraft stop` clears everything; the grid goes back to pockets first.

## When it does not work

1. Enter `/plugins info auto-craft`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-craft`. To remove its package while retaining user data, enter `/plugins uninstall auto-craft`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
