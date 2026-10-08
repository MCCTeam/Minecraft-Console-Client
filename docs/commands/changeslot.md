# /changeslot

Select a hotbar slot, by number or by the item in it.

## Syntax

```text
/changeslot <1-9>|<item>
```

## Forms

| Form | Meaning |
| --- | --- |
| `/changeslot <1-9>` | select a hotbar slot by number |
| `/changeslot <item>` | select the slot holding an item |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/changeslot 3
/changeslot diamond_pickaxe
```

## Behavior and requirements

Numbers are 1 through 9, matching the player's hotbar labels. They are not inventory window slot IDs.

An item selector searches the hotbar only. If several hotbar slots contain that item, repeating the command cycles to the next match. An item in the backpack cannot become the held slot until you move it into the hotbar.

Declared game features: Inventory. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
