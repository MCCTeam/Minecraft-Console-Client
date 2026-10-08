# /recipebook

List unlocked recipes, or place one into the open menu.

## Syntax

```text
/recipebook list | recipebook craft|craftall <recipe> | recipebook ui
```

## Forms

| Form | Meaning |
| --- | --- |
| `/recipebook list` | list the recipes you have unlocked |
| `/recipebook craft <recipe>` | place one recipe into the open menu |
| `/recipebook craftall <recipe>` | place as many as fit |
| `/recipebook ui` | open the visual recipe browser |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/recipebook list
/recipebook craft minecraft:torch
/recipebook ui
```

## Behavior and requirements

`list` shows recipes the server reports as unlocked. The accepted selector can depend on the protocol's named or numeric recipe representation.

`craft` places one recipe's ingredients in the open compatible menu. `craftall` asks for as many as fit. The operation does not automatically collect the result. Open the required workstation before placement.

Declared game features: Inventory. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
