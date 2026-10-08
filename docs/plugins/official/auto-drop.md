# Auto-drop

Drops item types selected by an inclusion or exclusion list.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-drop` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/AutoDrop). It is not built into MCC. Its declared game/resource usage is `inventory`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install auto-drop@official 2.0.0
/plugins enable auto-drop
/plugins info auto-drop
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `auto-drop` as loaded.
2. Open `plugins/userdata/auto-drop/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-drop`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Mode = "include"
Items = ["Dirt"]
```

Use an inventory without valuable items. Put dirt and cobblestone in it. Check that dirt drops and cobblestone remains.

```text
/autodrop on
/autodrop off
/autodrop add <item>
/autodrop remove <item>
/autodrop list
/autodrop mode <include|exclude|everything>
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Mode` | `"include"` | include = drop the listed items, exclude = keep only them, everything = drop it all. |
| `Items` | `[ "Cobblestone", "Dirt" ]` | Item names. The legacy enum spelling (Cobblestone), a bare id or a namespaced id all work. |

## Requirements and limits

Needs inventory handling. `include` drops listed types. `exclude` keeps listed types and drops the others. `everything` drops all supported inventory items. Check the mode before enabling it.

## When it does not work

1. Enter `/plugins info auto-drop`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-drop`. To remove its package while retaining user data, enter `/plugins uninstall auto-drop`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
