# /dig

Attempt to break a block.

## Syntax

```text
/dig <x> <y> <z>
```

## Forms

| Form | Meaning |
| --- | --- |
| `/dig` | break the block you are looking at |
| `/dig <x> <y> <z>` | break a specific block |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/dig
/dig 150 79 380
```

## Behavior and requirements

The bare command targets the looked-at block. The coordinate form targets a specific block. Terrain data, reach, game mode, selected tool, and protection rules affect success.

MCC attempts a real server interaction. It does not directly edit the local world snapshot to claim that the block broke.

Declared game features: Terrain. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
