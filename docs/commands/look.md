# /look

Look at direction or coordinates.

## Syntax

```text
/look <x y z|yaw pitch|up|down|east|west|north|south>
```

## Forms

| Form | Meaning |
| --- | --- |
| `/look <direction>` | up down east west north south |
| `/look <yaw> <pitch>` | aim at exact angles |
| `/look <x> <y> <z>` | aim at a block |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/look north
/look 150 80 380
```

## Behavior and requirements

A direction rotates the view. Two numbers are yaw and pitch angles. Three coordinates aim at a position. This changes orientation, not player position.

Coordinates can be absolute, `~` relative, or a fully local `^` triple. Use `/move get` to inspect position before selecting a distant target.

Declared game features: Terrain. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
