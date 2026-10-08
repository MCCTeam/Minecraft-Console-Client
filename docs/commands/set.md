# /set

Set a custom %variable%.

## Syntax

```text
/set varname=value
```

## Forms

| Form | Meaning |
| --- | --- |
| `/set <name>=<value>` | set a %variable% usable in other commands |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/set home=150 80 380
```

## Behavior and requirements

Variables hold text values. A name uses letters, digits, and underscores. A dot or other unsupported character can truncate the stored name and cause collisions.

`/set` updates the current runtime store. It does not automatically save all runtime changes to `client.toml`. Use `[Variables]` for startup values.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
