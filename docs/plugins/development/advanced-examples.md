# Advanced plugin examples

These examples follow the [chaptered plugin guide](tutorial/index.md). They show communication between plugins, complete Beacon integration, and session work with cancellation.

Use the complete [AdvancedExamples sample](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/AdvancedExamples/README.md) to execute the checks. Each C# block below identifies its source file. The sample includes the manifests and project files.

All projects target .NET 10 and DMCBK `0.1.0-preview.7`. Build the verifier in Release configuration. It uses pinned NuGet packages rather than DMCBK or MCC source references.

## Example 1: A service and typed messages between plugins

A service is an object that another plugin calls through an interface. A notification describes an event without asking for an answer. A request has a corresponding response.

This example offers the same price calculation through all three mechanisms. The result is always `12` for unit price `3` and count `4`. The repeated calculation makes the communication mechanisms easy to compare.

### Define the shared contract

Create a small class library named `Contracts`. It has no DMCBK dependency. Add these complete declarations to `Contracts.cs`:

```csharp
namespace Guide.Contracts;

public interface IPriceService
{
    double Subtotal(double unitPrice, double count);
}

public sealed record QuoteRequest(double UnitPrice, double Count);
public sealed record QuoteReply(double Total);
public sealed record OrderPriced(double Total);
```

`IPriceService` is the direct-call contract. `QuoteRequest` and `QuoteReply` are the request and response. `OrderPriced` is a notification. These types contain ordinary numbers and expose no provider implementation classes.

Both author projects reference this contract project while compiling. At runtime, only the provider package contains `Guide.Contracts.dll`. The consumer obtains the same assembly from the provider's exported context.

Two separate private copies can have identical names and still be different CLR types. A successful author build cannot detect that runtime identity error. The verifier loads separate plugin contexts to check it.

### Implement the provider

Create the provider class library. Reference `DMCBK.PluginSdk` and the contract project. Add this complete file as `Provider.cs`:

```csharp
using System.Threading.Tasks;
using DMCBK.PluginSdk;
using Guide.Contracts;

public sealed class PriceProvider : IPlugin
{
    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "guide-pricing";
        descriptor.Version = "1.0.0";
    }

    public Task ActivateAsync(PluginContext context)
    {
        context.Services.Register<IPriceService>(new PriceService());
        context.Messenger.RegisterResponder<QuoteRequest, QuoteReply>(request =>
        {
            double total = request.UnitPrice * request.Count;
            context.Messenger.Publish(new OrderPriced(total));
            return new QuoteReply(total);
        });
        return Task.CompletedTask;
    }

    private sealed class PriceService : IPriceService
    {
        public double Subtotal(double unitPrice, double count) => unitPrice * count;
    }
}
```

The provider registers one service instance. It also registers one responder. When the responder handles a quote, it publishes an `OrderPriced` notification before returning the reply.

These callbacks are synchronous. They contain a short calculation only. Do not put a blocking HTTP call or long database query into these callbacks.

Use this complete provider manifest:

```toml
schema-version = 2
id = "guide-pricing"
version = "1.0.0"
kind = "compiled"
target = "any"
entry = "Guide.Provider.dll"
framework = "net10.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
needs = ["commands"]
deps = ["Guide.Contracts.dll"]

[exports]
assemblies = ["Guide.Contracts.dll"]
```

`deps` makes the contract assembly available beside the entry DLL. `exports.assemblies` makes that assembly available to dependent plugins. Neither field installs another plugin.

### Implement the consumer

Create the consumer class library. Reference the SDK and the contract project for compilation. Add this complete file as `Consumer.cs`:

```csharp
using System;
using System.Globalization;
using System.Runtime.Loader;
using System.Threading.Tasks;
using DMCBK.PluginSdk;
using Guide.Contracts;

public sealed class PriceConsumer : IPlugin
{
    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "guide-order";
        descriptor.Version = "1.0.0";
    }

    public Task ActivateAsync(PluginContext context)
    {
        context.Messenger.Subscribe<OrderPriced>(message =>
            context.Variables.Set("guide_order_notification", Format(message.Total)));
        if (!context.Services.TryGet<IPriceService>(out var service))
            throw new InvalidOperationException(context.Strings.Get("missing_service"));

        context.Variables.Set("guide_order_service", Format(service.Subtotal(3, 4)));
        if (!context.Messenger.TryRequest<QuoteRequest, QuoteReply>(
                new QuoteRequest(3, 4), out var reply) || reply is null)
            throw new InvalidOperationException(context.Strings.Get("missing_responder"));
        context.Variables.Set("guide_order_response", Format(reply.Total));
        // This assembly must belong to the provider's collectible context, not the host.
        var contractContext = AssemblyLoadContext.GetLoadContext(typeof(IPriceService).Assembly);
        context.Variables.Set("guide_order_exported_context",
            contractContext is { IsCollectible: true } ? "yes" : "no");
        return Task.CompletedTask;
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
```

The consumer subscribes before it sends the request. Otherwise, the notification from that request would arrive before the subscriber exists.

`TryGet` and `TryRequest` distinguish an absent provider from a usable one. Required dependencies make absence a loading error. Optional dependencies need an ordinary unavailable path instead.

The context check is part of this example's verification. It checks that the contract belongs to a collectible plugin context. The service and message checks then prove that the consumer uses a compatible identity.

Use this complete consumer manifest:

```toml
schema-version = 2
id = "guide-order"
version = "1.0.0"
kind = "compiled"
target = "any"
entry = "Guide.Consumer.dll"
framework = "net10.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
needs = ["commands"]

[requires]
guide-pricing = "^1.0.0"
```

The required provider loads before the consumer. Do not put `Guide.Contracts.dll` into the consumer archive. The verifier deliberately omits it.

For production authoring, you can distribute the contract definitions through a dedicated NuGet package. The runtime ownership rule remains the same. A NuGet reference supplies compile-time types. The manifest selects the runtime provider.

### Check results and cleanup

The verifier checks `guide_order_service`, `guide_order_response`, and `guide_order_notification`. Each value must be `12`. Use letters, digits, and underscores for client variable names. The store truncates a name at its first other character. Dots and hyphens can therefore cause unintended key collisions. It also checks the provider-owned context marker.

Registration handles can withdraw the service, responder, or subscription early. The host withdraws registrations when the plugin unloads. Do not keep another plugin's service reference after its provider becomes unavailable.

The example uses activation-local service references. It does not store a provider object in a static field or retain it for later sessions.

## Example 2: Plugin variables, custom events and script exports

A Beacon variable exposes a snapshot. A custom event supplies a notification. A script export lets C# call a function that the running script owns.

The `guide-bridge` plugin combines these mechanisms. Its internal `guide-workflow` command calls the script's exported calculation. It remembers the result and fires `workflow_ready`.

The complete [BridgeObserver.cs](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/AdvancedExamples/BridgeObserver/BridgeObserver.cs) also contains the session example explained below. Its activation code registers the snapshot and event before the command becomes usable.

This complete class contains both the Beacon bridge and session registrations. Add it as `BridgeObserver.cs` in the sample project. Its localized strings come from the included `lang/en.toml`.

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DMCBK.Core.Commands;
using DMCBK.PluginSdk;
using Umpk.Commands;
using Umpk.Protocol.Java;

public sealed class BridgeObserver : IPlugin
{
    private double _lastTotal;
    private Task _worker = Task.CompletedTask;

    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "guide-bridge";
        descriptor.Version = "1.0.0";
    }

    public Task ActivateAsync(PluginContext context)
    {
        context.Beacon.Variables.Register(new BeaconVariable(
            "workflow", "workflow.read", context.Strings.Get("snapshot_help"),
            _ => new Dictionary<string, object?> { ["total"] = Volatile.Read(ref _lastTotal) }));
        context.Beacon.RegisterEvent("workflow_ready", ["total"],
            context.Strings.Get("event_help"), capability: "workflow.read");
        context.Commands.Register(new WorkflowCommand(context, total => Volatile.Write(ref _lastTotal, total)));

        context.SessionStarted += (_, args) =>
        {
            ISessionScope session = args.Session;
            int ticks = 0;
            session.Scheduler.OnTick(() =>
                context.Variables.Set("guide_bridge_ticks", (++ticks).ToString(CultureInfo.InvariantCulture)));
            session.Scheduler.Delay(2, () => context.Variables.Set("guide_bridge_delayed", "yes"));
            session.ObservePackets((in PacketFrame frame) =>
            {
                if (!frame.IsClientbound) return;
                byte[] payload = frame.CopyPayload();
                session.Scheduler.Post(() =>
                    context.Variables.Set("guide_bridge_packet_bytes", payload.Length.ToString(CultureInfo.InvariantCulture)));
            });
            _worker = session.Scheduler.RunOffLoop(async () =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, session.Detached).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (session.Detached.IsCancellationRequested)
                {
                    context.Variables.Set("guide_bridge_canceled", "yes");
                }
            }, session.Detached);
        };
        return Task.CompletedTask;
    }

    public async Task DeactivateAsync(CancellationToken ct)
        => await _worker.WaitAsync(ct).ConfigureAwait(false);

    private sealed class WorkflowCommand(PluginContext context, Action<double> remember) : CommandBase
    {
        public override string CmdName => "guide-workflow";
        public override string CmdDesc => context.Strings.Get("command_help");
        public override string CmdUsage => "guide-workflow";
        public override void Register(CommandBuilder<CommandContext> builder)
            => builder.Literal(CmdName, command => command.Executes(async call =>
            {
                object? value = await context.Beacon.CallFunctionAsync(
                    "workflow", "subtotal", [3.0, 4.0], call.Source.Cancellation).ConfigureAwait(false);
                double total = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                remember(total);
                var dispatched = await context.Beacon.FireEventAsync("workflow_ready",
                    new Dictionary<string, object?> { ["total"] = total }, call.Source.Cancellation).ConfigureAwait(false);
                if (dispatched.Handlers.Any(handler => handler.Result is { Success: false }))
                    throw new InvalidOperationException(context.Strings.Get("event_failed"));
                return call.Source.Result.Ok(context.Strings.Format("result", total));
            }));
    }
}
```

### Load the script

The host must attach Commands and Beacon before Plugins. It must initialize the lazy Beacon engine before plugin activation. Use `client.Scripts.SetMuted(true)` when script chat should remain muted during an offline check.

The plugin manifest declares `needs = ["commands", "beacon"]`. The script declares `workflow.read`. Host module requirements and script capabilities are different contracts.

After loading the plugin, run this complete `workflow.bcn` file as script ID `workflow`:

```beacon
# beacon 1
# needs: workflow.read
export function subtotal(price, count)
  return price * count
end function
on workflow_ready as e
  assert(e.total is 12, "custom event total")
  assert(workflow.total is 12, "plugin variable snapshot")
  show "Workflow complete: {e.total}"
end on
```

The function export has no game dependency. The custom event handler checks the supplied total and the new snapshot. Its local output is `Workflow complete: 12`.

Loading registers this handler. Loading alone does not call the exported function or fire the event. The verifier dispatches `guide-workflow` afterward.

### Follow the complete call

1. Dispatch the internal `guide-workflow` command.
2. Await `CallFunctionAsync("workflow", "subtotal", [3.0, 4.0], cancellation)`.
3. Store the returned total.
4. Fire `workflow_ready` with the total field.
5. Check each event handler result.
6. Return the localized command result.

The command result is `Workflow total: 12`. The script handler's assertions must also pass. Finally, the verifier runs another script that reads `workflow.total` and checks `12`.

The snapshot uses a new string-keyed dictionary for each read. The dictionary contains a number, not the plugin's internal object. The event uses the same simple boundary kinds.

The command passes its cancellation token to the exported call and event dispatch. For a session-bound producer, pass that session's `Detached` token instead. Missing exports or mismatched arguments produce `BeaconCallException`.

## Example 3: Observe packets and schedule session work

The same bridge plugin subscribes inside `SessionStarted`. It receives that event's fresh scope. It does not retain the scope for another session.

Its session handler registers an `OnTick` callback, a two-tick `Delay`, and a raw packet observer. It also starts an off-loop wait that uses `session.Detached`.

| Operation | Thread or owner | Example check |
| --- | --- | --- |
| `OnTick` | Session loop and session scope | Tick count reaches at least two |
| `Delay(2, callback)` | Session loop and session scope | Delayed marker becomes `yes` |
| `ObservePackets` | Connection read/send path and session scope | Copied clientbound payload reaches posted work |
| `Scheduler.Post` | Session loop | Packet length marker updates |
| `RunOffLoop` | Off-loop task with explicit token | Detach cancels the pending wait |

### Copy a packet before retaining it

`PacketFrame` is a ref struct. Its payload can point to a reusable buffer. A callback cannot retain the frame itself.

The example calls `CopyPayload()` inside the observer. It then posts a closure that owns the copied byte array. This demonstrates the correct boundary for later work.

Do not copy every packet for ordinary counters. Read `PayloadLength` directly when you only need a byte count. This sample copies every clientbound payload to demonstrate retention safely. A recorder needs explicit memory limits and a bounded queue.

Keep the observer short. Never wait for an HTTP request or write a large file in the connection callback. A slow callback can delay packet processing.

### Use cancellation deliberately

The off-loop task waits with `session.Detached`. It catches cancellation only when that token canceled. It records a marker and returns.

`DeactivateAsync` awaits the owned worker with the deactivation token. The runtime detaches the session before deactivation, so the wait can finish. Do not start an unowned task and assume unload stops it.

The verifier ends its modeled session and waits for the cancellation marker. It then unloads the plugin. Dispatching `guide-workflow` must no longer succeed.

The task is deliberately simple. Replace the infinite wait with your bounded I/O or computation. Pass the detach token to the real operation and await the worker during cleanup.

## Build and run the complete sample

1. Open a terminal in `samples/PluginAuthoring/AdvancedExamples`.
2. Configure the package feed if the preview is not published.
3. Run the verifier.

```sh
dotnet run --project Verify/Verify.csproj -c Release
```

Expected output:

```text
PASS exported contracts, services, request/response and notifications
PASS script export, custom event and variable snapshot
PASS session scheduler, packet observer, cancellation and command cleanup
```

The verifier stages only the intended plugin DLLs, resources and provider contract. It excludes host-provided DMCBK and UMPK assemblies. It reads the actual sample manifests and script file.

The protocol session uses in-memory transport. The check does not need an account or external server. It proves these callbacks and packet observations for the modeled exchange. It does not prove live movement, inventory transactions, native loading, or same-client reconnect.

## Diagnose an advanced example

| Symptom | Check |
| --- | --- |
| Service unavailable | Provider loaded state, contract identity, and required range |
| Contract mismatch | A private duplicate contract DLL or missing provider export |
| Notification missing | Subscribe before publishing and check dependency communication |
| Script provider unavailable | Beacon initialization and plugin loaded state |
| Custom event handler absent | Event declaration, script header, and running script ID |
| Export missing | Running provider script and exported function name |
| Packet work sees invalid bytes | Copy payload inside the observer before retaining it |
| Cleanup never finishes | Token propagation and the owned worker's completion |

Return to the [plugin index](index.md).


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
