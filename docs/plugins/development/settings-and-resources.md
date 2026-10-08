# Settings, storage and resources

Packages describe defaults. User data survives package replacement. Keep those responsibilities separate when designing a plugin.

## Declare typed settings

```csharp
using System;
using System.Threading.Tasks;
using DMCBK.PluginSdk;

public sealed class CounterSettings : IValidatablePluginSettings
{
    public int IntervalSeconds { get; set; } = 30;
    public void Validate() => IntervalSeconds = Math.Clamp(IntervalSeconds, 1, 3600);
}

public sealed class SettingsPlugin : IPlugin
{
    private CounterSettings? _settings;

    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "settings-example";
        descriptor.WithSettings<CounterSettings>();
    }

    public Task ActivateAsync(PluginContext context)
    {
        _settings = context.Settings.Load<CounterSettings>();
        context.ConfigurationReloaded += (_, _) =>
            _settings = context.Settings.Load<CounterSettings>();
        return Task.CompletedTask;
    }
}
```

The settings type needs a public parameterless constructor. `Validate()` normalizes values after loading. Runtime counters and connection objects do not belong in settings.

`context.Settings.Save(settings)` persists user changes. Save does not replace the need to normalize values before accepting them from an editor.

A package can supply `defaults/settings.toml` defaults. User values overlay package defaults. Updates do not overwrite existing user choices.

A malformed user file produces a warning and validated defaults. It does not make successful activation proof that the user's file parsed correctly.

## Persist data

| API | Behavior |
| --- | --- |
| `Storage.DataDirectory` | Assigned persistent data root |
| `Storage.GetPath(relativePath)` | Resolve a path inside that root and create parent directories |
| `Storage.TryGet(key, out value)` | Read a stored string |
| `Storage.Set(key, value)` | Change the in-memory key/value store |
| `Storage.Remove(key)` | Remove a key |
| `Storage.Save()` | Persist the key/value store |

1. Write user data through the assigned storage root.
2. Call `Save()` after changing the key/value store.
3. Serialize concurrent counter updates in your plugin.
4. Keep files out of immutable package and source-cache directories.

The key/value file is `data/storage.toml`. Changing the in-memory store without saving does not persist it. Each save writes a complete temporary file and then replaces `storage.toml`. Concurrent saves from the same storage instance are serialized. A read sees a complete saved snapshot. Custom file readers on Windows must allow file replacement with `FileShare.Delete`.

## Translate user-facing text

1. Create `lang/en.toml` in the package.
2. Give each message a stable key.
3. Use `context.Strings.Get(key)` for plain text.
4. Use `context.Strings.Format(key, args)` for parameterized text.

```toml
[counter]
joined = "Session {0} started."
interval_help = "Seconds between counter reports."
```

Nested TOML keys become dotted lookup keys, such as `counter.joined`.

Lookup follows the selected culture, its parents and English. A missing key appears as the key itself. A partial translation can fall back per message.

Language files load when the plugin loads. Reload the plugin after editing a translation file.

Tomlet settings comments can use `$counter.interval_help$` placeholders. The settings writer resolves them through the plugin's own language table.

## Supply a manual

1. Create `man/en/session-counter.md`.
2. Explain purpose, commands, settings and limitations.
3. Add `man = ["session-counter"]` to the manifest.
4. Add translated topics under matching language directories when needed.

Manual registration belongs to the current client. Unloading the plugin removes its contributions.

## Handle sensitive configuration

User settings are ordinary files. They are not a secret vault. Avoid logging access tokens or including credentials in release archives.

A host can supply its own secure storage workflow. A plugin should document which settings contain credentials and how its host supplies them.

Next: [Dependencies and loading](dependencies-and-loading.md).

## Defaults and user values, with an example

Suppose the package declares `Increment = 1` in `defaults/settings.toml`. A user writes `Increment = 3` in their settings file. The active value is three.

A later release can add a new default property. An existing user value still takes precedence. Do not copy new package defaults over the user's file during activation.

The runtime loads values when requested. Your cached settings object does not change when a user edits the file. Reload the plugin or explicitly load a new settings object at a documented hook.

`Validate` changes the in-memory values. `Save` serializes them. Call validation before saving values received from a UI or external service.

| Location | Owner and purpose |
| --- | --- |
| Package `defaults/settings.toml` | Author defaults |
| User `settings.toml` | User choices |
| User `data/storage.toml` | Persisted key/value data |
| User `data/report.json` | Plugin-owned structured output |
| Package `lang/en.toml` | English message resources |
| Package `man/en/topic.md` | User manual topic |

## Design a settings change

Keep property names stable when possible. A property rename can make an old user value ineffective. Explain migration steps in release notes.

Normalize values within realistic limits. A negative interval, empty hostname, or inverted range needs an explicit rule. A silent default is appropriate only when the user can understand it.

Test missing files, partial files, invalid TOML, and values outside the accepted range. Check the diagnostic path as well as the resulting setting.

## Translate parameterized text

Keep placeholders in each translation. The message `"Sessions: {0}"` has one argument. A translation can move that placeholder, but it must not remove or change its meaning.

Use invariant formats for stored data. Use the host's selected language for displayed text. A localized decimal format should not determine the format of a persistent machine-readable file.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
