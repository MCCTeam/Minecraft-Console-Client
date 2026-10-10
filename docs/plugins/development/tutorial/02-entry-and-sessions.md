# Chapter 2: Understand the entry class and sessions

Start with the entry from Chapter 1. Add a field and a session callback.

`Configure` declares the plugin ID and version. It does not connect to a server. The manifest supplies package identity and compatibility. Keep its ID and version consistent with the code.

`ActivateAsync` receives the context. It can run before a session exists. This version attaches one session callback.

## Replace the entry file

1. Replace `SessionJournal.cs` with this version.
2. Build the author project.
3. Run the stage verification command below.

```csharp
using System.Globalization;
using System.Threading.Tasks;
using DMCBK.PluginSdk;

public sealed class SessionJournal : IPlugin
{
    private int _count;

    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "session-journal";
        descriptor.Version = "1.0.0";
    }

    public Task ActivateAsync(PluginContext context)
    {
        context.SessionStarted += (_, _) =>
        {
            _count++;
            context.Variables.Set("session_journal_sessions",
                _count.ToString(CultureInfo.InvariantCulture));
        };
        return Task.CompletedTask;
    }
}
```

```sh
dotnet run --project samples/PluginAuthoring/VerifyStages -- samples/PluginAuthoring/TutorialStages/Stage02 2
```

The variable becomes `1` during a test session. This version stores its count only in memory. Chapter 3 makes the count persistent.

## Count session starts

The sample subscribes to `context.SessionStarted`. The callback runs when a play session starts. A reconnect creates another session and invokes the callback again.

The plugin remains loaded between sessions. Its `_count` field therefore survives reconnect within the same process. This version resets after reload. Chapter 3 adds a storage file for persistence.

A packet callback can run on a different thread from a command or script callback. Later versions add a lock when commands and Beacon also read the count.

Do not add blocking network calls to this lock. Later versions write a small file once per session. A frequent event should queue or batch storage writes.

## Select the correct lifetime

| Work | Register it through |
| --- | --- |
| A command available while disconnected | `context.Commands` |
| A command valid only in one session | `args.Session.Commands` in a session callback |
| A packet observer for one session | `args.Session.ObservePackets` |
| A script extension owned by this plugin | `context.Beacon` |
| Work canceled at session end | The session's `Detached` token |
| A socket or task created by the plugin | Your own field and deactivation cleanup |

The event argument provides the current session. Do not keep a session from an earlier connection. Check `context.CurrentSession` before an action that can occur while disconnected.

## Why the sample has no deactivation method

The sample creates no background task, network connection, or external event subscription. The runtime owns the plugin context and its session lifecycle.

A plugin that creates an `HttpClient`, timer, task, or external subscription must release it in `DeactivateAsync`. A collectible assembly context cannot release resources still referenced by that work.

The [lifecycle reference](../lifecycle-and-commands.md) explains pre-connect hooks, session creation, configuration reload, and shutdown order.

Next: [Settings and storage](03-settings-and-storage.md).

To test your own package, replace the sample folder argument with its absolute path. Keep the stage number unchanged.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
