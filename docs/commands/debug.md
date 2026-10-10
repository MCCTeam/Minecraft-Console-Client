# /debug

Toggle debug messages.

## Syntax

```text
/debug [on|off|state]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/debug state` | dump the client state |
| `/debug on` | turn debug messages on |
| `/debug off` | turn them off |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/debug state
/debug on
```

## Behavior and requirements

`on` and `off` control runtime debug messages. `state` prints status and feature information, useful after a connection failure or disabled-feature result.

This is separate from raw diagnostic packet capture. Debug output can reveal private operational details.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
