# Chapter 3: Add settings and persistent storage

Settings answer “What does the user want?” Stored data answers “What happened?” Keep the two separate.

The sample's `Increment` setting controls the amount added at each session start. The stored `sessions` value records the result.

## Add settings and storage to the entry

1. Replace `SessionJournal.cs` with this complete version.
2. Create the resource files described below.
3. Build the author project.

```csharp
using System;
using System.Globalization;
using System.Threading.Tasks;
using DMCBK.PluginSdk;

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

}
```

## Declare and validate settings

`JournalSettings` has a public parameterless constructor. Its `Validate` method limits `Increment` to the range 1 through 100.

`descriptor.WithSettings<JournalSettings>()` declares the type. `context.Settings.Load<JournalSettings>()` reads the user's values and runs validation.

The package defaults file is **`defaults/settings.toml`**:

```toml
Increment = 1
```

The user settings file is `userdata/session-journal/settings.toml` under the host's plugin root. User values overlay the package defaults.

1. Load the plugin once.
2. Open the generated user settings file.
3. Set `Increment = 2`.
4. Reload the plugin.
5. Start another session.
6. Check that the count increases by two.

You can also test a user override with the final verifier:

```sh
dotnet run --project samples/PluginAuthoring/VerifyPlugin -- samples/PluginAuthoring/SessionJournal 2
```

The optional argument writes a user Increment value before loading. The verifier checks counts of two and four. A negative argument checks normalization to one.

The sample reads settings during activation. Editing the file alone does not change the active settings object. Reload activates a new instance and reads the file again.

A malformed file causes a warning and validated defaults. Check host logs when an option appears ineffective. Do not assume that activation proves successful parsing.

## Store the count

The sample reads the `sessions` key with `Storage.TryGet`. It parses a string with invariant culture. An absent or invalid value starts the example at zero.

The session callback calls `Storage.Set`, then `Storage.Save`. The first changes memory. The second persists the key/value table.

The table is stored at `userdata/session-journal/data/storage.toml`. Package updates leave this data outside the immutable version directory.

Use `Storage.GetPath("report.json")` for a separate file. Keep its format version inside the file if later plugin releases will change that format.

Do not write generated data beside `SessionJournal.dll`. An installed package directory is immutable. A source cache is also temporary implementation storage.

## Plan data changes

A new plugin version can read an old storage format. A downgrade may not understand a new format. Test both directions before advertising rollback support.

Keep a backup before a destructive migration. The marketplace can restore an earlier package selection. It cannot undo arbitrary changes that your plugin makes to user files.

Next: [Commands and text](04-commands-and-text.md).

For optional advanced verification, use the DMCBK sample checkout from [Chapter 1](01-project.md#optional-advanced-verification):

```sh
dotnet run --project samples/PluginAuthoring/VerifyStages -- samples/PluginAuthoring/TutorialStages/Stage03 3
```

To test your own package, replace the sample folder argument with its absolute path. Keep the stage number unchanged.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
