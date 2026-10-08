# State and integrations

Choose storage according to its lifetime. A local variable, shared value and saved value have different uses.

## Settings and saved state

```beacon
# beacon 1
# setting interval = 30 ; seconds between reports
set reports to saved("reports") or 0
set reports to reports + 1
save "reports" to reports
show "Report {reports}, interval {settings.interval}"
```

Header settings declare scalar defaults and comments. The host overlays user values from `configurations/beacon/<id>.settings.toml`.

`saved(key)` and `save key to value` preserve state under the configuration's Beacon directory. Settings describe user choices. Saved values describe runtime progress.

1. Use settings for editable options.
2. Use saved state for values that must survive restart.
3. Keep temporary calculations in local variables.

`shared` is RAM-only state across scripts in one client. Use namespaced keys, such as `shared["shop.count"]`.

`lock shared` and `end lock` delimit a serialized read-modify-write block. Without that block, two handlers can read the same counter before either writes it.

## Import a library

1. Create `scripts/lib/math.bcn` with this content.

```beacon
# beacon 1
function subtotal(price, count)
  return price * count
end function
```

2. Create `scripts/order.bcn` with this content.

```beacon
# beacon 1
import "lib/math.bcn" as math
show math.subtotal(3, 4)
```

Imports resolve relative to the importing file. Circular imports fail with a diagnostic. Lint includes imported capability requirements.

Libraries contribute functions and top-level constants. Their event and command blocks do not register in the importing script.

## Export script functions

```beacon
# beacon 1
export function subtotal(price, count)
  return price * count
end function
export set prices to {bread: 3, apple: 5}
```

If this script runs as `shop`, another script can use `call "shop.subtotal"(3, 4)` or `call "shop.prices"()`.

The target must be loaded and export the requested name. Calls use the caller's fuel budget. Missing exports and argument mismatches raise catchable errors.

## Add commands

```beacon
# beacon 1
# desc: Calculate a fixed order total.
# example: /order-total
command "/order-total"
  show 3 * 4
end command
```

A command pattern needs its leading slash. `<item>` declares one string argument. The client dispatcher also accepts quoted values with spaces. The body reads it with `arg("item")`.

Commands register with the internal dispatcher and withdraw when the script stops. They do not create server commands.

## Use plugin functions and variables

An `extern` declaration names the provider plugin ID. The [Beacon plugin example](../plugins/development/beacon-integration.md) offers `subtotal` from `shop-tools`.

A plugin variable namespace exposes a read-only map. Every read obtains a snapshot. Scripts cannot write into the plugin's object graph.

Only the six Beacon value kinds cross the bridge. C# services and session objects cannot cross directly.

1. Declare the function's capability in `# needs:`.
2. Declare the function with `extern name from "plugin-id"`.
3. Catch errors when an optional provider can disappear.

## Files and network

`file_read` and `file_write` require `fs.read` and `fs.write`. Paths remain inside the host's `scripts/data` directory.

`http_get` and `http_post` require `net.fetch`. The host's `beacon.toml` must also allow the destination host:

```toml
[Net]
AllowedHosts = ["api.example.org"]
```

The runtime allows only HTTPS requests. A script capability does not bypass the network allowlist.

1. Declare only the permissions that your script uses.
2. Keep network calls out of high-frequency handlers.
3. Catch network and parsing failures separately when their recovery differs.

Next: [MCC operations and testing](hosting-and-testing.md).

## Keep settings separate from progress

A report interval is a setting because the user chooses it. A completed-report count belongs in saved state because the script updates it. Mixing these uses makes edits and resets difficult to understand.

A settings overlay does not need to contain every declared default. Omitted keys use the script's defaults. Incorrect kinds produce diagnostics rather than a useful replacement value.

Saved state belongs to the script ID and configuration root. Keep that ID stable when the same script should retain progress. A separate engine or root does not share that state automatically.

## Imports and running providers

An imported library is part of the caller's program. It is useful for calculations and constants. A running provider owns exports that other scripts can call.

Use an import for a small reusable helper. Use an export when several scripts need one provider's running state. Handle provider absence because the user can stop or reload it.

Optional dependencies need both a declaration and a failure path. `# wants:` permits loading without the provider. It does not turn a missing function into a successful call.

## File and network boundaries

File helpers address the shared `scripts/data` directory beside the configured source folder. They do not grant general access to the host filesystem. Absolute paths, parent traversal, and symlink escapes are not a way to reach account files.

All scripts in that engine use this file area. Use a directory or filename prefix for your own data. The runtime limits each file to 1 MiB. Saved state remains separate and belongs to a script ID.

Network configuration uses bare host names. `api.example.org` is a host. `https://api.example.org/path` is a URL and does not belong in `AllowedHosts`.

Do not treat an HTTP response as correct data until parsing and field checks pass. A successful request can still return text that is not the expected JSON document.

## Choose a failure response

For an optional price provider, show a local unavailable message. For a required provider, fail loading clearly. For a temporary network problem, wait before a bounded retry.

Do not save a secret response into general state merely to simplify debugging. Inspect only the fields needed by the script.

For a complete persistent counter exercise, read [Chapter 6](guide/06-state.md). For commands and providers, read [Chapter 7](guide/07-integrations.md).

## Share a value with MCC commands

`vars.beacon` connects scripts to MCC's string variable store. A write such as `set vars.beacon.coins to 5` creates the command variable `%beacon_coins%`.

```beacon
# beacon 1
set vars.beacon.coins to 5
show vars.beacon.coins
```

Run this file in normal MCC. The offline runner does not provide the command variable store.

Reads from this bridge are text. Convert with `number(...)` before numeric calculations. These values differ from saved state and shared Beacon maps.

## Read a plugin snapshot

A plugin can register a read-only variable namespace. Its documentation must give the namespace name, fields, and capability.

1. Install and enable the provider plugin.
2. Declare its capability in the script header.
3. Read its documented fields.
4. Check optional values before calculating from them.

The script cannot change that snapshot to modify the plugin. Use a documented plugin function for an action.

## Respond to a plugin event

A plugin can register a custom event with named fields. Use `on <event-name> as e` and read the fields the provider documents.

Custom event names can produce offline lint warnings because the provider is absent. `--strict` escalates unknown-event and provider warnings.

A script does not become portable merely because it passes syntax checks. State its provider requirements beside the installation instructions.

## Keep integrations available

Required capabilities belong in `# needs:`. Optional capabilities belong in `# wants:` with a failure path.

An `extern` declaration binds a function name to one plugin ID. A plugin reload can temporarily remove that function.

A `call "script.export"(...)` expression targets a running script. Stopping that provider removes its exports.

When distributing several files, include imported libraries and provider scripts. Document their load order. Do not rely on another user's unrelated files.
