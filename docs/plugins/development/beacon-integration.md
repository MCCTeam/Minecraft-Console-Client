# Extend Beacon from a plugin

The Beacon bridge supports functions, read-only variable namespaces, custom events and calls into exported script functions.

The following plugin exposes a pure calculation. It can be tested without a game session.

## Register a function

```csharp
using System;
using System.Threading.Tasks;
using DMCBK.PluginSdk;

public sealed class ShopTools : IPlugin
{
    public void Configure(PluginDescriptor descriptor) => descriptor.Id = "shop-tools";

    public Task ActivateAsync(PluginContext context)
    {
        context.Beacon.Functions.Register(new BeaconFunction(
            Name: "subtotal",
            Capability: "shop.calculate",
            Description: context.Strings.Get("shop.subtotal_help"),
            Parameters: ["price", "count"],
            ParameterTypes: [typeof(double), typeof(double)],
            ReturnType: typeof(double),
            Invoke: call => Task.FromResult<object?>(
                call.RequireNumber(0) * call.RequireNumber(1))));
        return Task.CompletedTask;
    }
}
```

Add `shop.subtotal_help = "Calculate an order subtotal."` to `lang/en.toml`.

1. Declare `needs = ["commands", "beacon"]` in the plugin manifest.
2. Initialize the Beacon runtime before loading the plugin.
3. Load the plugin before loading scripts that require its capability.
4. Run this script.

```beacon
# beacon 1
# needs: shop.calculate
extern subtotal from "shop-tools"
assert(subtotal(3, 4) is 12, "plugin subtotal")
show subtotal(3, 4)
```

## Initialize the host runtime

Beacon initializes its runtime lazily in this preview. Composing the module alone does not create the engine.

Initialize it before plugin activation when scripts must call plugin functions before a session starts:

```csharp
using DMCBK.Core;
using DMCBK.PluginSdk;

await using Client client = new ClientBuilder()
    .UseServer("localhost")
    .UseUsername("ShopBot")
    .UseCommands()
    .UseBeacon()
    .UsePlugins(new PluginOptions(Path.GetFullPath("plugin-data"))
    {
        DevelopmentFolders = [Path.GetFullPath("shop-tools")]
    })
    .Build();

client.Scripts.SetMuted(false);
var runtime = client.GetModule<PluginHost>();
await runtime.LoadAllAsync();
if (!runtime.List().Single().Loaded)
    throw new InvalidOperationException("The plugin did not load.");
```

`SetMuted(false)` initializes the runtime while allowing script chat. Use `SetMuted(true)` when the host should hold script chat.

Without early initialization, plugin registrations can remain pending before the first session. A pre-session script call can then report a missing provider.

The function declares argument names, CLR types and a return type. Signature validation rejects values that cannot cross the bridge.

| Beacon kind | C# boundary value |
| --- | --- |
| Text | `string` |
| Number | Numeric values, represented as Beacon numbers |
| Yes/no | `bool` |
| List | Lists of crossable `object` values |
| Map | String-keyed dictionaries of crossable `object` values |
| None | `null` |

Do not return a session scope, service instance or private DTO. Convert it to a map or list first.

## Function lifetime and cancellation

Functions run on the Beacon scheduler. They must be reentrant and respect `call.Cancellation`.

1. Keep calculation functions free of blocking I/O.
2. Pass the supplied cancellation token into asynchronous operations.
3. Return data for script-side movement rather than steering from an extension callback.

Registration handles withdraw functions early. Plugin unload and reload also withdraw owned registrations.

## Variable namespaces

`context.Beacon.Variables.Register(new BeaconVariable(...))` provides a named namespace, capability, help text and snapshot callback.

A script reads it as a map, such as `coins.balance`. The snapshot callback runs inline. Keep it fast and do not block.

Return fresh crossable values. Do not expose a mutable settings or session object.

## Custom events

`RegisterEvent(name, fields, description, suppressible, capability)` declares an event. `FireEventAsync(name, values, detached)` dispatches it.

1. Give each event a stable name and field schema.
2. Supply only crossable values.
3. Pass the current session's `Detached` token for session-bound events.

A canceled session token prevents dispatch into an ended session. Scripts consume custom events with ordinary `on` blocks.

## Call an exported script function

`context.Beacon.CallFunctionAsync(scriptId, function, args, cancellation)` awaits a function declared with `export function`.

The script must be running. Missing scripts, missing exports and argument mismatches raise `BeaconCallException`.

For example, a running `shop` script can export `subtotal(price, count)`. A plugin calls it with script ID `shop`, function `subtotal` and arguments `[3.0, 4.0]`.

A host without Beacon supplies an inactive bridge. If your plugin requires this integration, declare the `beacon` capability rather than silently depending on it.

Return to the [plugin index](index.md).

## Separate host and script capabilities

A plugin that requires Beacon declares `beacon` in its manifest's `needs`. Its function can expose a different capability, such as `shop.calculate`. A script declares that capability in its header.

The host module permits registration. The script capability permits use. The script's `extern` statement selects the provider and function by name.

The [Session Journal chapter](tutorial/05-beacon.md) follows the complete chain from plugin registration to an executable script assertion.

## Design extension values

Use a number for a calculation, text for a label, and a map for a structured snapshot. Copy mutable state into the returned map. A caller should not receive the plugin's internal object.

For example, a shop snapshot can contain `balance` and `currency` fields. Return simple values under string keys. Keep the field names stable across compatible releases.

Use `RequireNumber`, `RequireText`, and `RequireYesNo` for arguments. Invalid kinds then produce a clear boundary failure instead of an accidental cast exception.

## Event ownership

Declare an event before firing it. Its field names form the script contract. Supply values for that declared schema.

Use the session detach token for a game event. Use plugin-lifetime cancellation for work that remains meaningful while disconnected.

A custom event can run many scripts. Keep the producer independent of the scripts' implementation. Handle an unsuccessful dispatch result without repeating the same external action accidentally.

## Diagnose an extension call

| Symptom | Likely check |
| --- | --- |
| Provider missing before connection | Lazy Beacon initialization before activation |
| Function unavailable | Plugin loaded state and function registration |
| Capability missing | Script header and declared function capability |
| Argument kind rejected | Signature and argument values |
| Return value rejected | Unsupported CLR object crossing the boundary |
| Function disappears after reload | Expected withdrawal and new registration |

Keep extension callbacks short. A long synchronous callback blocks the scheduler even if its method returns a Task.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
