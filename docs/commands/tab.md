# /tab

Show a vanilla-like tab list. In TUI mode, opens a live overlay.

## Syntax

```text
/tab
```

## Forms

| Form | Meaning |
| --- | --- |
| `/tab` | show the tab list; a live overlay in TUI mode |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/tab
```

## Behavior and requirements

This reads the server's tab list, including available names and formatting. In TUI mode it opens a live overlay. `[TabList] ShowTeams=true` adds team information to that view.

This is not the same as tracking entities in nearby chunks. A listed player need not be visible as an entity.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
