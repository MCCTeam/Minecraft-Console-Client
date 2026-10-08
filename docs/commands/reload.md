# /reload

Reloads MCC settings.

## Syntax

```text
/reload
```

## Forms

| Form | Meaning |
| --- | --- |
| `/reload` | re-read client.toml without restarting |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/reload
```

## Behavior and requirements

This reloads portable configuration through the attached configuration storage. It updates command configuration, language, configured variables, and plugin instances. It then announces the configuration reload.

It does not reconstruct every live game module or reload host `console.toml`. Restart for a console mode change. Reconnect for changes that require new session composition.

Treat warnings as evidence that some values were rejected or need a restart. Reloading plugins removes their old registrations and creates new instances.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
