# /achievement

List your advancements and how far through them you are.

## Syntax

```text
/achievement <list|locked|unlocked|ui>
```

Aliases: `/advancements`.

## Forms

| Form | Meaning |
| --- | --- |
| `/achievement list` | every advancement the server sent |
| `/achievement list <scope>` | only one branch: story, adventure, recipes/tools |
| `/achievement unlocked` | only completed ones |
| `/achievement locked` | only incomplete ones |
| `/achievement ui` | open the visual advancement browser |
| `/achievement unlocked <scope>` | List completed advancements in one branch. |
| `/achievement locked <scope>` | List incomplete advancements in one branch. |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/achievement list
/achievement list story
/achievement locked
/achievement ui
```

## Behavior and requirements

The alias is `/advancements`. These commands read advancement data received from the server. `unlocked` means completed. `locked` means incomplete.

A scope selects a branch such as `story`, `adventure`, or `recipes/tools`. The data can be unavailable on a protocol without structured advancement decoding. That result is not an empty completion history.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
