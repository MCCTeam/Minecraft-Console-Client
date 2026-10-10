# /servers

Save servers to servers.toml for /connect.

## Syntax

```text
/servers add <name> <host[:port]>
```

## Forms

| Form | Meaning |
| --- | --- |
| `/servers add <name> <host[:port]>` | save a server to servers.toml and select it |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/servers add Home localhost:25565
/servers add Hypixel mc.hypixel.net
```

## Behavior and requirements

This writes a normal server entry to `servers.toml` and selects it. An existing name is overwritten. The default port is 25565.

Saving a server does not connect immediately. Follow it with `/connect <name>`. The command needs an attached configuration folder.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
