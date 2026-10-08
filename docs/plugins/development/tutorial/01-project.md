# Chapter 1: Prepare the project

Your author project checks C# code during development. Your package contains the files that the runtime loads. These are different outputs.

For a small plugin, one C# file can serve both purposes. A larger plugin can use several source files and ship compiled output.

## Prerequisites

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
2. Check the installation with `dotnet --version`.
3. Select an empty work directory.
4. Check that your NuGet source contains DMCBK `0.1.0-preview.3`.

If this preview is not available on your configured feed, use the [local package instructions](https://github.com/MCCTeam/DMCBK/blob/master/docs/getting-started/installation.md). Do not replace the package reference with a reference to MCC source.

## Create the author project

Run these commands from your work directory:

```sh
dotnet new classlib --framework net10.0 --name SessionJournal
cd SessionJournal
dotnet add package DMCBK.PluginSdk --version 0.1.0-preview.3
```

1. Remove `Class1.cs`.
2. Save the entry below as `SessionJournal.cs`.
3. Replace the project file with the [sample project file](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/SessionJournal/SessionJournal.csproj).
4. Build the project with `dotnet build -c Release`.

The expected result is `Build succeeded`. The DLL appears under `bin/Release/net10.0`.

The sample disables implicit usings. Each C# file declares its namespaces. Runtime source compilation does not inherit a project's implicit usings.

The SDK supplies `IPlugin`, contexts, settings, localization, and Beacon contracts. It also supplies references needed by this sample through its dependencies. Do not add the plugin runtime to the entry project just to implement `IPlugin`.

## First entry file

This plugin has no behavior yet. It checks that the SDK reference and entry contract work.

```csharp
using System.Threading.Tasks;
using DMCBK.PluginSdk;

public sealed class SessionJournal : IPlugin
{
    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "session-journal";
        descriptor.Version = "1.0.0";
    }

    public Task ActivateAsync(PluginContext context) => Task.CompletedTask;
}
```

## Create the package files

Create this structure beside the entry file:

```text
SessionJournal/
├── SessionJournal.csproj
├── SessionJournal.cs
└── plugin.toml
```

Create this first manifest. Add resource directories in later chapters.

```toml
schema-version = 2
id = "session-journal"
version = "1.0.0"
kind = "source"
target = "any"
entry = "SessionJournal.cs"
framework = "net10.0"
api-version = "1.0"
dmcbk = ">=0.1.0-preview.3 <0.2.0"
umpk = ">=0.9.0-beta.4 <0.10.0"
needs = ["commands"]
```

`kind = "source"` tells the runtime to compile the entry file. `target = "any"` declares portable source. `needs = ["commands"]` requires the Commands module in the host. Chapter 5 adds Beacon.

The project file helps development. The source loader reads `SessionJournal.cs`, not the project. It does not restore additional packages declared only in that project.

## Optional advanced verification

These verification programs come from the optional DMCBK sample checkout. You do not need that checkout to author, build, or load your plugin in MCC.

1. Open an operating-system terminal in a separate work directory.
2. Obtain the sample checkout.
3. Enter its repository directory.

```sh
git clone https://github.com/MCCTeam/DMCBK.git dmcbk-examples
cd dmcbk-examples
```

4. Run the stage verification command from that directory.

Keep this directory as your working directory for later optional verification commands. Return to your plugin project for its build commands.

The verifier arguments select an included stage package and the expected stage number:


```sh
dotnet run --project samples/PluginAuthoring/VerifyStages -- samples/PluginAuthoring/TutorialStages/Stage01 1
```

The expected result is `PASS: tutorial stage 1.` This checks real runtime source loading and activation.

Next: [Entry class and sessions](02-entry-and-sessions.md).

To test your own package, replace the sample folder argument with its absolute path. Keep the stage number unchanged.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
