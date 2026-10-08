# Auto-respond

Matches chat rules and dispatches an MCC command for each matching rule.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `auto-respond` | `2.0.0` | `commands` |

The plugin lives in the separate [DMCBK-Plugins repository](https://github.com/MCCTeam/DMCBK-Plugins/tree/main/src/AutoRespond). It is not built into MCC. Its declared game/resource usage is `chat`, `commands`, `files`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

If a publisher already offers this release in a marketplace named `official`, enter:

```text
/plugins install auto-respond@official 2.0.0
/plugins enable auto-respond
/plugins info auto-respond
```

These commands depend on that publisher having uploaded working release assets. The repository catalogue alone does not prove that a downloadable release exists.

1. Check that `/plugins list` shows `auto-respond` as loaded.
2. Open `plugins/userdata/auto-respond/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload auto-respond`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Matches_File = "matches.ini"
Match_Colors = false
```

Create the rule below in `data/matches.ini`. Ask another player to send `ping`. The client sends `pong`.

Create this file at `plugins/userdata/auto-respond/data/matches.ini`:

```ini
[Match]
match=ping
action=send pong
cooldown=5
```

Each `[Match]` section describes one rule. `action` runs for public chat. `actionprivate` runs for recognized whispers. `actionother` runs for other server lines. `$u` expands to the sender name. Regex capture groups expand as `$1`, `$2`, and later group numbers.

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `false` | Controls the plugin behavior after activation. |
| `Matches_File` | `"matches.ini"` | Rules file, resolved inside this plugin's data/ folder. A sample is written on first run. |
| `Match_Colors` | `false` | Keep formatting codes in the text the rules match against. |

## Requirements and limits

The rules file uses its own INI grammar. It is not Beacon and not TOML. A rule can run privileged client commands. Use `ownersonly=true` for control rules. Do not create a rule that repeatedly matches its own responses.

## When it does not work

1. Enter `/plugins info auto-respond`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable auto-respond`. To remove its package while retaining user data, enter `/plugins uninstall auto-respond`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
