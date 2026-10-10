# /connect

Connect to the specified server.

## Syntax

```text
/connect <server> [account]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/connect <server>` | a name from servers.toml, or host[:port] |
| `/connect <server> <account>` | connect as a specific account |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/connect localhost
/connect play.example.net:25566 Alt
```

## Behavior and requirements

The first value can be a saved name from `servers.toml` or a direct address. The optional account is a label from `accounts.toml`.

A direct address defaults to port 25565. Server selection can require authentication before joining. This command replaces the current connection. It does not save a new server entry.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
