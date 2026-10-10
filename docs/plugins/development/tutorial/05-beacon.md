# Chapter 5: Let Beacon read the count

The plugin registers `journal_count` through `context.Beacon.Functions`. It returns a number and has no arguments.

The C# callback returns `Task<object?>` because the bridge can call asynchronous extension functions. This calculation completes immediately.

The function capability is `journal.read`. The plugin manifest requires the host module `beacon`. These names describe different requirements.

## Add the function to the entry

1. Replace `SessionJournal.cs` with this final version.
2. Add `beacon` to the manifest's `needs` list.
3. Add `function_help` to the English language table.
4. Build the author project.

```csharp
using System;
using System.Globalization;
using System.Threading.Tasks;
using DMCBK.Core.Commands;
using DMCBK.PluginSdk;
using Umpk.Commands;

public sealed class JournalSettings : IValidatablePluginSettings
{
    public int Increment { get; set; } = 1;
    public void Validate() => Increment = Math.Clamp(Increment, 1, 100);
}

public sealed class SessionJournal : IPlugin
{
    private readonly object _gate = new();
    private int _count;
    private JournalSettings _settings = new();

    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "session-journal";
        descriptor.Version = "1.0.0";
        descriptor.WithSettings<JournalSettings>();
    }

    public Task ActivateAsync(PluginContext context)
    {
        _settings = context.Settings.Load<JournalSettings>();
        if (context.Storage.TryGet("sessions", out string? saved))
            int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out _count);

        context.Commands.Register(new JournalCommand(context.Strings, ReadCount));
        context.Beacon.Functions.Register(new BeaconFunction(
            Name: "journal_count", Capability: "journal.read",
            Description: context.Strings.Get("journal.function_help"),
            Parameters: [], ParameterTypes: [], ReturnType: typeof(double),
            Invoke: call => Task.FromResult<object?>((double)ReadCount())));

        context.SessionStarted += (_, _) =>
        {
            lock (_gate)
            {
                _count += _settings.Increment;
                string value = _count.ToString(CultureInfo.InvariantCulture);
                context.Storage.Set("sessions", value);
                context.Storage.Save();
                context.Variables.Set("session_journal_sessions", value);
            }
        };
        return Task.CompletedTask;
    }

    private int ReadCount()
    {
        lock (_gate) return _count;
    }

    private sealed class JournalCommand(IPluginLocalization strings, Func<int> count) : CommandBase
    {
        public override string CmdName => "journal-count";
        public override string CmdDesc => strings.Get("journal.command_help");
        public override string CmdUsage => "journal-count";
        public override void Register(CommandBuilder<CommandContext> builder)
            => builder.Literal(CmdName, command => command.Executes(
                call => call.Source.Result.Ok(strings.Format("journal.count", count()))));
    }
}
```

The final manifest contains `needs = ["commands", "beacon"]`. The language table from Chapter 4 already includes `function_help`.

## Write the consumer script

Save this file as `read-count.bcn`:

```beacon
# beacon 1
# needs: journal.read
extern journal_count from "session-journal"
show journal_count()
```

`# needs` declares the extension capability used by the script. `extern` identifies the function and its provider plugin.

`show` produces local output. It does not send a server chat message. A script can read the saved count before the first connection.

## Run the script in MCC

1. Create `scripts/read-count.bcn` under your MCC runtime directory.
2. Copy the consumer script above into it.
3. Enter `/plugins validate /absolute/path/to/SessionJournal` at the MCC prompt.
4. Enter `/plugins load /absolute/path/to/SessionJournal`.
5. Enter `/plugins list`.
6. Check that `session-journal` reports a loaded state.
7. Enter `/scripts run read-count`.
8. Check the printed count.

MCC initializes its modules for you. You do not need to call the following builder APIs from the MCC prompt. If you changed the configured scripts directory, save the file there instead.

## Compose a custom DMCBK host

1. Attach Commands to the client builder.
2. Attach Beacon.
3. Attach Plugins.
4. Initialize Beacon before plugin activation.
5. Load the plugin.
6. Run the script.

Beacon initializes lazily. Call `client.Scripts.SetMuted(false)` before plugin activation when scripts need extensions before a session starts.

Without this initialization, registrations can remain pending until a session starts. A script can then report that its provider is missing.

## Test the boundary

The verification program executes this assertion after two sessions and a reload:

```beacon
# beacon 1
# needs: journal.read
extern journal_count from "session-journal"
assert(journal_count() is 2, "saved count")
show journal_count()
```

The callback returns a `double`. Beacon represents it as a number. Do not return `PluginContext`, a session object, or a private DTO.

Functions that perform I/O must honor `call.Cancellation`. The scheduler cancels work during reconnect and unload.

The [Beacon integration reference](../beacon-integration.md) also covers variables, custom events, and calls into exported script functions.

Next: [Load and test](06-load-and-test.md).


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
