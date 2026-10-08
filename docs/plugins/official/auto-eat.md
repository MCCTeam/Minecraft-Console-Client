# Auto-eat

Uses food when the player food level falls to the configured threshold.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-eat` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/AutoEat). It is not built into MCC. Its declared game/resource usage is `inventory`, `interaction`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install auto-eat@official 2.0.0
/plugins enable auto-eat
/plugins info auto-eat
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `auto-eat` as loaded.
2. Open `plugins/userdata/auto-eat/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-eat`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Threshold = 6
```

Place ordinary food in the inventory. On a survival test server, reduce food below the threshold. Check that the client eats and its food level rises.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Threshold` | `6` | Eat when food (0-20) is at or below this. Clamped to 0-20. |

## Requirements and limits

Needs inventory state and usable food. Food level is 0 to 20, not a percentage. The plugin does not create food or bypass the server rules.

## When it does not work

1. Enter `/plugins info auto-eat`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-eat`. To remove its package while retaining user data, enter `/plugins uninstall auto-eat`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
