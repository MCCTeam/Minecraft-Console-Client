# /send

Send a chat message or command.

## Syntax

```text
/send <text>
```

## Forms

| Form | Meaning |
| --- | --- |
| `/send <text>` | send chat, or a server command starting with / |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/send hello everyone
/send /help
```

## Behavior and requirements

This sends text directly through the chat/game command path. A leading slash in the text makes it a server command. It can reach a server command that MCC otherwise intercepts.

The command requires a connection. It is subject to chat pacing, protocol limits, and server acceptance.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
