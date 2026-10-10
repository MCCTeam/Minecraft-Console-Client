# Chapter 4: Add a command and translated text

The sample adds `journal-count`. It returns local output through the command result. It does not send chat to the server.

The nested `JournalCommand` inherits `CommandBase`. Its command tree contains one literal node. The executor calls `Result.Ok` with the current count.

`Func<int>` lets the command read the count when it runs. A number captured during activation would become stale after the next session.

## Add the command to the entry

1. Replace `SessionJournal.cs` with this complete version.
2. Create the resource files described below.
3. Build the author project.

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

## Supply text resources

Create `lang/en.toml` with these keys:

```toml
[journal]
command_help = "Show the saved session count."
function_help = "Read the saved session count."
count = "Sessions: {0}"
```

The TOML table creates dotted keys such as `journal.count`. The command uses `Strings.Format` to substitute the count for `{0}`.

Keep message keys stable. Translators can update text without changing code. A missing key appears as the key itself.

The host selects a culture. Lookup checks that culture, its parents, and English. A partial translation can use English for missing messages.

## Add the manual

1. Create `man/en/session-journal.md` from the [sample manual](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/SessionJournal/man/en/session-journal.md).
2. Explain what the plugin counts.
3. List its command and setting.
4. Explain its Beacon function.
5. Add `man = ["session-journal"]` to the manifest.

The sample manual follows those steps. The runtime registers its topic while the plugin is loaded.

## Execute the command

A host invokes `client.Commands.DispatchAsync("journal-count")`. This API takes the internal command text without a UI prefix.

The host can print `result.Message`, render it in a window, or return it from an API. The plugin does not select a console renderer.

Before the first session, the result is `Sessions: 0`. After one default session, the result is `Sessions: 1`.

In the MCC prompt, enter `/journal-count`. Before the first connection, it reports `Sessions: 0`. After one successful connection with default settings, it reports `Sessions: 1`.

The leading `/` is MCC input syntax. Keep `journal-count` without that prefix in the C# `DispatchAsync` call.

For arguments, extend the Brigadier tree with typed nodes. This preserves parsing and completion. See [lifecycle and commands](../lifecycle-and-commands.md).

Next: [Beacon integration](05-beacon.md).

For optional advanced verification, use the DMCBK sample checkout from [Chapter 1](01-project.md#optional-advanced-verification):

```sh
dotnet run --project samples/PluginAuthoring/VerifyStages -- samples/PluginAuthoring/TutorialStages/Stage04 4
```

To test your own package, replace the sample folder argument with its absolute path. Keep the stage number unchanged.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
