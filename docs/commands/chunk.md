# /chunk

Inspect loaded chunks or open the TUI chunk browser.

## Syntax

```text
/chunk status [chunkX chunkZ|locationX locationY locationZ] | chunk ui
```

## Forms

| Form | Meaning |
| --- | --- |
| `/chunk status` | map the chunks loaded around you |
| `/chunk status <x> <y> <z>` | map around a position |
| `/chunk status <chunkX> <chunkZ>` | map around a chunk |
| `/chunk ui` | open the live chunk map |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/chunk status
/chunk status 150 80 380
/chunk ui
```

## Behavior and requirements

`status` shows the chunks loaded around the player, with an optional position marker. A two-number selector is a chunk coordinate pair. A three-number selector is a block position.

`ui` opens the live browser in TUI mode. A bare `/chunk` opens that browser when possible or shows usage.

Diagnostic forms `_setloading <chunkX> <chunkZ>`, `_setloaded <chunkX> <chunkZ>`, and `_delete <chunkX> <chunkZ>` also accept a coordinate triple. They alter local diagnostic state. They do not load, save, or delete server-world chunks.

Declared game features: Terrain. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
