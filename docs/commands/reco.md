# /reco

Restart and reconnect to the server.

## Syntax

```text
/reco [account]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/reco` | reconnect to the current server |
| `/reco <account>` | reconnect as a different account |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/reco
/reco Alt
```

## Behavior and requirements

The bare command reconnects to the selected endpoint. An account argument uses that saved account label. The command remains available from the idle/offline prompt when an endpoint exists.

Reconnecting ends the current game session. Plugins and scripts receive the lifecycle changes. Temporary entity IDs and open container IDs should not be reused.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
