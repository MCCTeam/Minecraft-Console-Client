# /tps

Estimate server ticks per second.

## Syntax

```text
/tps
```

## Forms

| Form | Meaning |
| --- | --- |
| `/tps` | estimate the server's tick rate |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/tps
```

## Behavior and requirements

TPS means ticks per second. MCC estimates it from server time updates. The estimate can be inaccurate during lag or when the server changes tick rate.

Unknown means there is no usable recent sample. It does not mean the server runs at zero TPS. A paused server may stop sending samples.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
