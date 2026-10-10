# Make your first plugin

This plugin counts successful session starts. It stores the count outside the package and exposes the current value through the client's variable store.

## Create the author project

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
2. Create a class library named `SessionCounter`.
3. Add `DMCBK.PluginSdk` version `0.1.0-preview.5`.
4. Replace the generated class with this file.

```csharp
using System.Globalization;
using System.Threading.Tasks;
using DMCBK.PluginSdk;

public sealed class SessionCounter : IPlugin
{
    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "session-counter";
        descriptor.Version = "1.0.0";
        descriptor.ApiVersion = PluginApiVersion.Major;
    }

    public Task ActivateAsync(PluginContext context)
    {
        int count = 0;
        if (context.Storage.TryGet("sessions", out string? saved))
            int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out count);

        context.SessionStarted += (_, _) =>
        {
            count++;
            string value = count.ToString(CultureInfo.InvariantCulture);
            context.Storage.Set("sessions", value);
            context.Storage.Save();
            context.Variables.Set("session_counter_sessions", value);
        };
        return Task.CompletedTask;
    }
}
```

`Configure` declares identity. `ActivateAsync` attaches callbacks once. Each reconnect produces another `SessionStarted` event.

The callback writes a small counter. For frequent events, batch disk writes instead of saving on every callback.

## Create a source package

1. Create a directory named `session-counter`.
2. Copy `SessionCounter.cs` into that directory.
3. Add this `plugin.toml` file.

```toml
schema-version = 2
id = "session-counter"
version = "1.0.0"
kind = "source"
target = "any"
entry = "SessionCounter.cs"
framework = "net10.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
needs = ["commands"]
```

The archive root contains `plugin.toml` and the entry file. Do not add an extra wrapper directory inside the archive.

Runtime source compilation reads one entry file. It does not restore your author project's NuGet dependencies or build its `.csproj`.

A development project can use implicit usings. A runtime source entry must include the namespaces that it uses explicitly.

## Load it in a host

1. Add `DMCBK.Commands` and `DMCBK.Plugins` to the host.
2. Compose Commands before Plugins.
3. Supply explicit package and data paths.
4. Load plugins before starting the client.

Use the complete [local loading example](https://github.com/MCCTeam/DMCBK/blob/master/docs/guides/plugins.md#load-a-local-plugin).

Check each plugin's `Loaded` state after loading. An aggregate operation result does not prove that every individual plugin activated.

Installed package files are immutable. User settings and data belong in `userdata/<id>` under the plugin root.

The [downloadable source example](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/SessionCounter/SessionCounter.cs) and [manifest](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/SessionCounter/plugin.toml) match this tutorial.

Next: [Lifecycle and commands](lifecycle-and-commands.md).

## Check the complete example

The [SessionCounter sample](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/SessionCounter/README.md) includes exact test commands and a runnable verifier. Use it to check the entry through runtime source compilation.

The count changes only after a successful session start. Loading the plugin while disconnected does not increase it.

The variable store holds text for the current client instance. The persistent storage table holds text on disk. This example updates both with the same number for different purposes.

Use [Session Journal](tutorial/index.md) when you want a complete project with settings, a command, translated text, Beacon, compiled packaging, and reconnect tests.

## Check compatibility before loading

| Field | What it checks |
| --- | --- |
| `schema-version` | The manifest format, currently `2` |
| `api-version` | Plugin contract major and required minor |
| `dmcbk` | The library version used by the host |
| `umpk` | The engine version used by the host |
| `framework` | The managed target framework |
| `target` | Operating system and process architecture |
| `needs` | Host capabilities such as Commands or Beacon |
| `hosts` | Optional application ID and version restrictions |

Do not use a wildcard merely to silence a compatibility failure. Test the versions that you declare. Keep the library version, API version, and plugin release version separate.


## Load your package in MCC

1. Start MCC in a separate runtime directory.
2. Keep the Minecraft session disconnected during the first test.
3. Enter `/plugins validate /absolute/path/to/your-package`.
4. Enter `/plugins load /absolute/path/to/your-package`.
5. Enter `/plugins list`.
6. Check that your plugin reports a loaded state.
7. Connect to your controlled test server.
8. Check the callback result.
9. Enter `/plugins unload session-counter`.

Use `/plugins install /absolute/path/to/your-package` for a persistent installation. Review its plan before you accept it. The installer copies package files into immutable storage. Editing the author folder after installation does not edit that installed copy.

See [MCC plugin management](../managing.md) for data locations, reloads, and diagnostics.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
