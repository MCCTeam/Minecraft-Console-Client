# /useblock

Request a block interaction with the selected hand.

## Syntax

```text
/useblock <x> <y> <z> [mainhand|offhand]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/useblock <x> <y> <z>` | right-click a block: chests, levers, doors |
| `/useblock <x> <y> <z> mainhand` | right-click with the main hand |
| `/useblock <x> <y> <z> offhand` | right-click with the off hand |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/useblock 150 79 380
/useblock 150 79 380 offhand
```

## Behavior and requirements

This requests a right-click interaction at a block. Depending on the block and held item, it can open a menu, activate a lever, use a door, or place a block.

The default hand is the main hand. The server decides the effect. A successful request does not prove that the intended world change occurred.

Declared game features: Terrain. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
