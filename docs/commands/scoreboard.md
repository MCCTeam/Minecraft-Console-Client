# /scoreboard

Show scoreboard objectives and their scores.

## Syntax

```text
/scoreboard [ui|<objective>] [--all]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/scoreboard` | list objectives with entry counts |
| `/scoreboard ui` | open the live movable scoreboard window |
| `/scoreboard <objective>` | show the top scores for one objective |
| `/scoreboard <objective> --all` | show every score for one objective |

## Flags

| Flag | Meaning |
| --- | --- |
| `--all` | show every score instead of the top 15 |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/scoreboard
/scoreboard ui
/scoreboard kills
/scoreboard kills --all
```

## Behavior and requirements

The bare command lists objectives and entry counts. A named objective shows leading scores. `--all` includes all scores for that objective.

`ui` opens a movable live scoreboard in the TUI. The available snapshot can include all objectives without identifying the exact sidebar selection used by a graphical client.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
