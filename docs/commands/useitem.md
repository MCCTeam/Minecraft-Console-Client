# /useitem

Use the item in your hand, optionally on a specific block.

## Syntax

```text
/useitem [mainhand|offhand] | useitem [x] [y] [z] [mainhand|offhand]
```

## Forms

- `/useitem`: use what you are holding
- `/useitem mainhand`: use the main-hand item
- `/useitem offhand`: use the off-hand item
- `/useitem <x> <y> <z>`: use it on a block
- `/useitem <x> <y> <z> mainhand|offhand`: Use the selected hand at a block.

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/useitem
/useitem offhand
```

## Behavior and requirements

Without an explicit hand, the command can prefer offhand food over a non-food main hand. Otherwise it can use the item on a looked-at block when terrain raycasting finds one.

Supply `mainhand` or `offhand` to choose a hand. Supply coordinates for a specific block. The command's request is not confirmation that the server consumed an item or changed a block.

Declared game features: Inventory. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
