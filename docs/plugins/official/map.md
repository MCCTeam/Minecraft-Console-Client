# Maps

Collects Minecraft map updates, presents map images, and optionally exports them.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `map` | `2.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/Map). It is not built into MCC. Its declared game/resource usage is `files`, `chat`. These declarations describe usage, not permission boundaries.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install map@official 2.0.2
/plugins enable map
/plugins info map
```

1. Check that `/plugins list` shows `map` as loaded.
2. Open `plugins/userdata/map/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload map`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
Render_In_Console = true
Save_To_File = true
Auto_Render_On_Update = false
Delete_All_On_Unload = false
```

Hold an in-game map. Run `/maps list`, then `/maps render 5` with an ID from the list. Check the console image or TUI pane and the exported file.

```text
/maps list
/maps render <id>
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `Render_In_Console` | `true` | Print the map to the console as coloured half-blocks. |
| `Save_To_File` | `false` | Write the map to a .bmp in this plugin's data/Rendered_Maps folder. |
| `Auto_Render_On_Update` | `false` | Render on every map update instead of only on 'maps render <id>'. |
| `Delete_All_On_Unload` | `true` | Clear previously rendered map files when the plugin starts (and on unload). |
| `Notify_On_First_Update` | `true` | Log a line the first time each map id is received. |
| `Rasize_Rendered_Image` | `false` | Resize the saved image to Resize_To x Resize_To pixels (legacy spelling kept). |
| `Resize_To` | `512` | Edge length in pixels for the resize. Must be positive; falls back to 128. |
| `Send_Rendered_To_Discord` | `false` | Hand the rendered image to the bridge plugins (they must be loaded and connected). |
| `Discord_Target` | `""` | Where the Discord render goes: route:name and dm:owner-id entries, comma-separated. Empty means every channel that takes game chat. |
| `Send_Rendered_To_Telegram` | `false` | See the operation details below. |

## Requirements and limits

The server must send a map before it appears. Terminal color/font support affects console rendering. `Rasize_Rendered_Image` is the actual setting spelling. Keep `Delete_All_On_Unload = false` when you want exported images to remain.

## Detailed operation

Collects the map images the server sends and draws them in the console.

### Commands

Run `/help maps` for the full syntax and worked examples.

## When it does not work

1. Enter `/plugins info map`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable map`. To remove its package while retaining user data, enter `/plugins uninstall map`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
