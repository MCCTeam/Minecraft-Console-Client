# Auto-attack

Attacks selected nearby entities at the configured cooldown.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-attack` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/AutoAttack). It is not built into MCC. Its declared game/resource usage is `entities`, `interaction`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install auto-attack@official 2.0.0
/plugins enable auto-attack
/plugins info auto-attack
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `auto-attack` as loaded.
2. Open `plugins/userdata/auto-attack/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-attack`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Mode = "single"
Priority = "distance"
Interaction = "Attack"
Attack_Range = 3.0
Only_In_Front = true
Attack_Hostile = true
Attack_Passive = false
List_Mode = "whitelist"
Entites_List = ["Zombie"]
```

On a controlled server, face a reachable zombie while holding an appropriate weapon. Check that it loses health.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Mode` | `"single"` | single = hit one priority target per cooldown, multi = hit every target in range. |
| `Priority` | `"distance"` | distance = closest first. health is accepted but falls back to distance on this core. |
| `Interaction` | `"Attack"` | Attack = left click, Interact = right click. |
| `Attack_Range` | `4.0` | Attack reach in blocks. Clamped to 1.0 - 4.0. |
| `Only_In_Front` | `true` | Only attack entities you can see: ahead of you and not behind blocks. Set to false to attack in every direction, through walls. |
| `Front_FOV` | `180.0` | Total view-cone width in degrees when Only_In_Front is true. 180 keeps the legacy hemisphere, smaller values ignore peripheral targets. Clamped to 1 - 180. |
| `Attack_Hostile` | `true` | Attack hostile mobs. |
| `Attack_Passive` | `false` | Attack passive mobs. |
| `List_Mode` | `"whitelist"` | whitelist = only the listed types, blacklist = every allowed type except the listed ones. |
| `Entites_List` | `[ "Zombie", "Cow" ]` | Entity types. The legacy enum spelling (Zombie), a bare id or a namespaced id all work. |
| `Cooldown_Time.Custom` | `false` | Use the values below instead of the vanilla attack-speed cooldown. |
| `Cooldown_Time.RandomMode` | `false` | Pick a random cooldown between Min and Max on every swing. |
| `Cooldown_Time.Min` | `1.5` | Minimum cooldown, seconds. |
| `Cooldown_Time.Max` | `2.5` | Maximum cooldown, seconds. |

## Requirements and limits

Requires entity handling. Terrain data improves sight checks. Preserve the exact setting name `Entites_List`, including its spelling. Health priority falls back to distance because this implementation does not track per-entity health. The server decides whether each attack lands.

## When it does not work

1. Enter `/plugins info auto-attack`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-attack`. To remove its package while retaining user data, enter `/plugins uninstall auto-attack`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
