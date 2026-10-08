# /minimap

Control the TUI terrain minimap and its labels.

## Syntax

```text
/minimap [on|off] | minimap zoom [in|out|<1-16>] | minimap names [players|hostile|neutral|passive] [on|off] | minimap names [all_on|all_off] | minimap position [top_left|top_right|center|bottom_left|bottom_right] | minimap cave [auto|on|off]
```

## Forms

- `/minimap`: Toggle the minimap.
- `/minimap on|off`: Show or hide the minimap.
- `/minimap zoom [in|out|<1-16>]`: Report or change blocks per pixel.
- `/minimap names [players|hostile|neutral|passive] [on|off]`: Report or change name labels.
- `/minimap names all_on|all_off`: Show or hide every name category.
- `/minimap position [top_left|top_right|center|bottom_left|bottom_right]`: Report or change the initial anchor.
- `/minimap cave [auto|on|off]`: Report or change cave mode.

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/minimap on
/minimap zoom 2
/minimap names players off
/minimap cave auto
/minimap position top_right
/minimap off
```

## Behavior and requirements

This command exists in TUI mode. The bare command toggles the map. Bare subcommands report their current values instead of toggling them.

Zoom is blocks per pixel. `in` lowers the value. `out` raises it. Values clamp at 1 and 16 for step changes.

Position selects a starting anchor. Dragging the window then moves it freely for the current run. `cave auto` chooses automatic cave behavior. These commands change current UI state. Use `console.toml` for startup preferences.

Availability: TUI mode. These names are not registered by the classic host.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
