# Classic console and terminal UI

MCC has two interfaces. Both use the same account, server, script, plugin, and command services.

## Select a mode

In `console.toml`:

```toml
[General]
ConsoleMode = "classic"
```

Use `tui` for the full-screen interface. Restart MCC after changing the mode. A one-run override is also available:

```bash
./Mcc.Cli --console.General.ConsoleMode=tui
```

The TUI needs an interactive terminal. Use classic mode for redirected output, containers without an attached terminal, and file-driven operation.

## Classic console

The classic interface shows a scrolling log and an input line. Type a message or command, then press Enter. The line editor can show command suggestions and retain input history.

Useful settings in `[General]`:

| Key | Default | Meaning |
| --- | --- | --- |
| `DisplayChat` | `true` | Show incoming chat |
| `DisplayInput` | `true` | Show typed input |
| `EchoCommands` | `true` | Keep executed command lines in the log |
| `HistoryInputRecords` | `32` | Input history limit |
| `Timestamps` | `false` | Add times to displayed lines |
| `DisplayIconBanner` | `true` | Show the startup banner |
| `Glyphs` | `auto` | Choose emoji or ASCII status symbols |
| `ConsoleColorMode` | `vt100_24bit` | Terminal color mode |
| `ShowInventoryLayout` | `true` | Show an inventory layout with item lists |
| `ConsoleTitle` | `%username%@%serverip% - Minecraft Console Client` | Windows title template |

`[CommandSuggestion] Enable` controls completion. `MaxSuggestionWidth` defaults to 30. `MaxDisplayedSuggestions` defaults to 10. `UseBasicArrow=true` replaces arrows that your font cannot show.

The suggestion text, backgrounds, highlights, tooltip colors, and arrow color use `#rrggbb` values. They apply to the classic popup in 24-bit color mode.

## Fix colors or missing characters

Color choices are `vt100_24bit`, `vt100_8bit`, `vt100_4bit`, `legacy_4bit`, and `disable`.

If escape sequences appear as text, select `disable` or the legacy mode that your terminal supports. If symbols appear as empty boxes, select ASCII:

```toml
[General]
Glyphs = "ascii"
ConsoleColorMode = "disable"
```

`Glyphs=auto` uses terminal and encoding information. Forcing ASCII also changes status symbols and chunk-map characters.

## TUI layout

The TUI shows a log, status bar, and input line. The status bar can show health, food, experience, and effects. Set `ShowEffectNamesInTui=true` to show effect names instead of icons.

Open the main menu with `/mcc-menu` or `Ctrl+M`. The menu provides command help, scripts, plugins, marketplaces, server selection, and game browsers. A view that needs a live session explains that requirement when the client is offline.

## Open game and management views

| Command | View |
| --- | --- |
| `/help ui` | Searchable live command browser |
| `/man ui` | Manual browser |
| `/scripts ui` | Script Workbench and editor |
| `/plugins ui` | Installed-plugin manager |
| `/tab` | Live player list |
| `/scoreboard ui` | Movable scoreboard |
| `/inventory player open` | Player inventory |
| `/recipebook ui` | Recipe browser |
| `/entity ui` | Entity browser |
| `/achievement ui` | Advancement browser |
| `/chunk ui` | Loaded-chunk browser |
| `/dialog open` | Current server dialog |
| `/minimap on` | Movable minimap |
| `/map` | Minecraft filled-map view |

The filled-map command is `/map`, singular. It is separate from the Map plugin and the terrain minimap.

## Navigate workspaces

1. Use arrow keys to select an item.
2. Press Enter to open its details.
3. Press `Ctrl+F` to focus search.
4. Press F5 to refresh when the workspace supports it.
5. Press Escape to return from a detail page.
6. Press Escape again to close the workspace.

Wide terminals show list and detail panes together. Narrow terminals show a list, then a detail page. Each pane scrolls separately.

The command browser reads the live registry. Commands can appear or disappear after plugins or scripts change. **Insert command** copies syntax to the input for review. It does not run the command.

## Minimap preferences

```toml
[Minimap]
Enabled = false
Zoom = 1
Width = 40
Height = 20
Position = "top_right"
ShowPlayerNames = true
ShowHostileNames = true
ShowNeutralNames = true
ShowPassiveNames = true
RefreshInterval = 1000
CaveMode = false
```

The map needs terrain data. Entity labels also need tracked entity data. Zoom is blocks per pixel, from 1 to 16. Larger values show a wider area.

The refresh interval accepts 100 to 5000 milliseconds after clamping. Use the generated default when choosing a value for your installation.

Drag the minimap title bar to move it. `Alt+S` starts resizing. `Alt+R` expands or restores it. `/minimap position top_right` returns it to an anchor. Closing it disables the visible minimap until you open it again.

Use `/minimap names players off` to hide player labels. `/minimap cave auto` selects automatic cave behavior for this run.

## TUI scrollback and clear

`TuiLogScrollback` defaults to 3000 lines. A nonpositive value resolves to that default. This is separate from your terminal's own scrollback.

`/clear-console` clears the classic screen or the TUI log pane. It does not delete diagnostic files.

The TUI draws its own suggestion popup. The classic popup color settings do not apply to that popup.
