# Build a plugin, one chapter at a time

This guide builds **Session Journal**, a plugin that counts connection sessions. It stores the count, adds a local command, and exposes a Beacon function. You can test it without a Minecraft account or server.

A plugin is code that a host loads into its own process. The host creates the client and selects its modules. The plugin receives services through `PluginContext`. It does not own the client's connection.

Follow the chapters in order. The [complete sample](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/SessionJournal/README.md) contains the final files. The [verification program](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/VerifyPlugin/Program.cs) exercises them through the real plugin loader.

| Chapter | Result |
| --- | --- |
| [1. Prepare the project](01-project.md) | A library project with the author SDK |
| [2. Understand the entry class](02-entry-and-sessions.md) | A counter that reacts to each session |
| [3. Add settings and storage](03-settings-and-storage.md) | A validated option and a persistent count |
| [4. Add commands and text](04-commands-and-text.md) | A localized command and a manual |
| [5. Connect Beacon](05-beacon.md) | A script reads the count |
| [6. Load and test](06-load-and-test.md) | Executable checks for source loading and session ownership |
| [7. Package compiled output](07-package-and-release.md) | A portable compiled asset |
| [8. Maintain the plugin](08-maintenance.md) | A method for debugging and safe updates |

The guide uses .NET 10, DMCBK `0.1.0-preview.5`, UMPK `0.9.0-beta.6`, API `1.0`, and manifest schema `2`. Pin these versions while following it.

## Words used in this guide

| Word | Meaning |
| --- | --- |
| Host | The application that creates a DMCBK client and loads plugins |
| SDK | The author contracts supplied by `DMCBK.PluginSdk` |
| Manifest | The `plugin.toml` file that describes one package |
| Session | One connection, from creation to disconnection |
| Registration | A command, callback, function, or service attached to an owner |
| Scope | An owner that removes its registrations when its lifetime ends |
| Source asset | A package with one C# entry file that the runtime compiles |
| Compiled asset | A package with an entry DLL built by the author |
| Capability | A named feature that a plugin or script requires |
| Runtime target | The operating system and process architecture used to select an asset |

You need [basic C# knowledge](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/tutorials/): classes, methods, variables, events, and asynchronous methods. The guide explains DMCBK concepts as they appear. Learn the C# concepts first if their syntax is unfamiliar.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
