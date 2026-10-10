# /blockinfo

Report what the client believes is at a position.

## Syntax

```text
/blockinfo <x> <y> <z> [-s]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/blockinfo <x> <y> <z>` | what the CLIENT believes is there |
| `/blockinfo <x> <y> <z> -s` | also report the six neighbours |

## Flags

| Flag | Meaning |
| --- | --- |
| `-s` | include the blocks around the target |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/blockinfo 150 79 380
/blockinfo 150 79 380 -s
```

## Behavior and requirements

This reports the block state in MCC's world snapshot, not a fresh server query. Terrain must be enabled and the chunk must be available.

`-s` also reports the six adjacent positions. Unknown data must not be treated as air. Coordinate forms use absolute, `~` relative, or fully `^` local triples.

Declared game features: Terrain. Some forms also need a current game session or a compatible menu.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
