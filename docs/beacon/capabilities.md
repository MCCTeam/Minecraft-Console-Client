# Beacon capabilities and limits

A capability is a named operation. A script declares required capabilities in `# needs:` and optional capabilities in `# wants:`.

```beacon
# beacon 1
# needs: chat.send
on chat as e when e.message is "!rules"
  whisper e.player "Read the server rules before building."
end on
```

The runtime checks declarations before loading. It examines handlers and functions even when those bodies do not run during loading.

## Built-in capabilities

| Capability | Operations | Additional prerequisite |
| --- | --- | --- |
| `chat.send` | `say`, `whisper` | Live chat session for delivery |
| `server.send` | `server` command statement | Live session and server permission |
| `mcc.run` | Internal command statement or expression | MCC's command dispatcher |
| `server.disconnect` | `disconnect` | An active session to leave |
| `inventory.read` | Inventory reads and recipe listing | Inventory tracking |
| `inventory.write` | Inventory changes and crafting | Inventory tracking and supported actions |
| `world.read` | Block, light, biome, and sign reads | Terrain tracking and loaded area |
| `world.search` | Block and sign searches | Terrain tracking and bounded search |
| `world.write` | Dig, place, use, and targeting reads | Terrain tracking and gameplay gates |
| `entity.read` | Entity reads and entity appearance events | Entity tracking |
| `entity.write` | Attack and interact | Entity tracking and supported actions |
| `movement` | Coordinate movement and following | Loaded movement data and action support |
| `dialog.read` | Dialog reads and dialog events | A supported server dialog |
| `dialog.write` | Dialog input and button actions | An active dialog |
| `fs.read` | File reads | Configured restricted data directory |
| `fs.write` | File writes | Configured restricted data directory |
| `net.fetch` | HTTP calls | HTTPS URL and destination allowlist |

Plugins can provide additional names, such as `shop.calculate`. Read that plugin's documentation for its meaning.

A declaration permits script use. It does not enable tracking, connect MCC, create a missing plugin, or grant server permissions.

`econ.read` is a reserved known capability. Its presence does not supply an economy service by itself.

## Required and optional providers

Use `# needs:` when the script cannot perform its purpose without a provider. Loading must fail when a required integration is unavailable.

Use `# wants:` when a missing integration has a useful fallback. Catch the call's failure and show a clear result.

```beacon
# beacon 1
# wants: shop.calculate
extern subtotal from "shop-tools"
try
  show subtotal(3, 4)
catch err
  show "Pricing is unavailable: {err.message}"
end try
```

Offline lint reports a warning until the provider exists. Do not remove a valid declaration just to hide that warning.

## Runtime limits

| Limit | Current behavior |
| --- | --- |
| Execution fuel | 100,000 units per dispatch. Function calls, loop work, and host actions spend fuel. |
| Wall-clock budget | Five seconds per dispatch, with scheduler-aware handling for yielding work. |
| Call depth | At most 256 nested calls. |
| Shared chat allowance | Eight messages per ten seconds across scripts in one runtime. Excess messages queue. |
| World action allowance | Eight mutations per ten seconds per script. Refusals report a retry delay. |
| Minimum task wait | 100 milliseconds. |
| File size | At most 1 MiB per restricted file. |
| Import/call lint closure | At most 32 files in one lint closure. |
| JSON nesting | At most 32 levels. |
| World search | Radius at most 32 blocks, at most 64 results. |
| Entity search | Radius at most 128 blocks, at most 64 result rows. |

Fuel exhaustion is not a normal recoverable game error. Keep calculations bounded instead of attempting an endless retry.

A queued chat request can outlive the event that created it. Limit repeated triggers before they create large amounts of work.

## Trust and external input

Only run scripts and plugins from sources you trust. A capability declaration describes operations. It is not a general isolation boundary for arbitrary C# plugin code.

A chat name is input, not proof of administrative authority. Add an explicit authorization rule before a script accepts remote management commands.

1. Use a server that you control for action tests.
2. Start with read-only behavior.
3. Add one action at a time.
4. Check the observed server result.
5. Catch session changes and missing targets.

[State and integrations](state-and-integrations.md) explains restricted files, network allowlists, and provider lifetimes.
