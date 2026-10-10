# /dropitem

Drop a selected stack or named items.

## Syntax

```text
/dropitem [<item>] [<count>]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/dropitem` | drop the selected stack |
| `/dropitem <count>` | drop that many from the selected slot |
| `/dropitem <item> [count]` | drop items by name |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/dropitem
/dropitem cobblestone 64
```

## Behavior and requirements

The bare form drops the selected stack. A count drops that many items from the selected slot. An item selector can find matching items in the player inventory or open container.

Check inventory contents before dropping. The items leave the player's control when the server accepts the action. Numeric arguments are counts, not item IDs.

Declared game features: Inventory. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
