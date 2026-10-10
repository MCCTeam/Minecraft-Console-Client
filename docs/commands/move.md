# /move

Walk, or path to coordinates.

## Syntax

```text
/move <on|off|get|up|down|east|west|north|south|center|x y z|gravity [on|off]> [-f]
```

## Forms

- `/move`: Report the current position.
- `/move <x> <y> <z> [-f]`: Navigate to coordinates.
- `/move <up|down|east|west|north|south> [-f]`: Move one directional step.
- `/move on|off`: Enable or disable terrain, physics, and pathfinding.
- `/move center`: Move to the center of the current block.
- `/move get`: Report the current position.
- `/move gravity [on|off]`: Report or change client gravity.

## Flags

| Flag | Meaning |
| --- | --- |
| `-f` | force unsafe moves: falling, fire, lava |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/move 150 80 380
/move north -f
/move gravity off
```

## Behavior and requirements

Coordinate movement uses navigation through known terrain. Direction movement takes one step. It does not teleport. A blocked destination can end at a reachable nearby position and report failure to reach the exact target.

`on` and `off` enable or disable terrain, physics, and pathfinding together. They are feature controls, not continuous forward walking. Enabling those session modules can require reconnecting.

`gravity off` changes the client's gravity setting. It does not grant flight on the server. `-f` permits unsafe movement through hazards such as falls, fire, and lava. It does not let the player occupy a solid block.

Coordinate triples support absolute values, `~` offsets, and fully local `^` coordinates. A movement lease held by another automation can prevent navigation.

Declared game features: Terrain, Physics, Pathfinding. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
