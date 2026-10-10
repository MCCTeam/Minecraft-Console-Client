# /bed

Used to sleep in or leave a bed.

## Syntax

```text
/bed leave|sleep <x> <y> <z>|sleep <radius>
```

## Forms

| Form | Meaning |
| --- | --- |
| `/bed sleep <x> <y> <z>` | sleep in the bed at a position |
| `/bed sleep <radius>` | find a bed within a radius and sleep |
| `/bed leave` | get out of bed |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/bed sleep 150 79 380
/bed sleep 8
/bed leave
```

## Behavior and requirements

Use coordinates for a known bed, or supply a search radius for a nearby bed. A radius search requires terrain data. Server time, nearby threats, reach, and server rules can prevent sleep.

`bed leave` requests that the player leave the bed. It does not change the world's time itself.

Declared game features: Terrain. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
