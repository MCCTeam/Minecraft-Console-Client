# /exit

Disconnect and stop MCC, optionally with a process exit code.

## Syntax

```text
/exit [code]
```

Aliases: `/quit`.

## Forms

| Form | Meaning |
| --- | --- |
| `/exit` | quit MCC |
| `/exit <code>` | quit with an exit code |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/exit
/exit 1
```

## Behavior and requirements

The alias is `/quit`. The command requests a clean disconnect and shutdown. An optional integer becomes the process exit code.

Use `/exit` in the default interactive slash mode. Bare `exit` is chat in that mode. File input has a separate special case for a bare exit line.

Clean shutdown allows MCC to close plugins, cancel scripts, and finish diagnostic packaging.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
