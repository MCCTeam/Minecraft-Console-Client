# /console-chat

Show or hide incoming chat in the current interface.

## Syntax

```text
/console-chat [on|off]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/console-chat` | show whether chat is visible |
| `/console-chat on` | show incoming chat |
| `/console-chat off` | hide it |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/console-chat off
```

## Behavior and requirements

The bare command reports whether incoming chat is visible. `on` and `off` change that display state for this run.

The client still receives messages. Scripts and plugins can still process them. Set `console.toml [General] DisplayChat` for a persistent preference.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
