# /map

Open the received Minecraft filled-map view in the TUI.

## Syntax

```text
/map
```

## Forms

| Form | Meaning |
| --- | --- |
| `/map` | show the held map item |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/map
```

## Behavior and requirements

This command exists in TUI mode. It opens the received Minecraft filled-map image through the host's map controller. It needs map data delivered by the server.

The command is `/map`, singular. It is separate from `/minimap` and the Map plugin. A missing map is not a request to generate a terrain image.

Availability: TUI mode. These names are not registered by the classic host.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
