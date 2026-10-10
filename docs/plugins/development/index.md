# Make MCC plugins with DMCBK

A plugin extends a client through `DMCBK.PluginSdk.IPlugin`. It can add commands, react to sessions, store settings and expose services to other plugins or Beacon.

Plugin authors restore NuGet packages. They do not need MCC source, a Git submodule or a DMCBK source checkout.

| Guide | What you will build or learn |
| --- | --- |
| [First plugin](getting-started.md) | A complete source plugin and manifest |
| [Lifecycle and commands](lifecycle-and-commands.md) | Session ownership, reconnect cleanup and a Brigadier command |
| [Settings, storage and localization](settings-and-resources.md) | User options, durable data, translated text and manuals |
| [Dependencies and assembly loading](dependencies-and-loading.md) | Required providers, optional services and exported contracts |
| [Beacon integration](beacon-integration.md) | A function that scripts can call |
| [Testing and release](testing-and-release.md) | In-memory tests, source/compiled packaging and platform assets |

Start with one source file for a small plugin. Use a compiled project when you need several source files, NuGet dependencies or native libraries.

All examples use API `1.0`, schema `2`, DMCBK `0.1.0-preview.7`, UMPK `0.9.0-beta.6` and .NET 10.

Marketplace v2 rejects old manifests. Existing MCC binary plugins must be rebuilt against DMCBK. See [marketplace v2](../../marketplaces/format.md) for installation and release formats.

## Follow a complete project

Read the [eight-chapter Session Journal guide](tutorial/index.md) for a step-by-step project. It starts with the author project and ends with tested source and compiled packages.

The guide does not assume that you know plugin lifecycle rules. It explains sessions, scopes, manifests, settings, localization, and script boundaries when you use them.

## Choose a starting point

| Your goal | Start here |
| --- | --- |
| Learn the full workflow | [Chaptered tutorial](tutorial/index.md) |
| Add a small disconnected calculation | [First plugin](getting-started.md), then [Beacon integration](beacon-integration.md) |
| React to incoming packets | [Lifecycle and commands](lifecycle-and-commands.md) |
| Share a service with another plugin | [Dependencies and loading](dependencies-and-loading.md) |
| Ship a private library or native DLL | [Dependencies and loading](dependencies-and-loading.md), then [testing and release](testing-and-release.md) |
| Expose user options | [Settings and resources](settings-and-resources.md) |

A package manifest describes a release asset. A descriptor describes the entry class. The manifest takes precedence for package identity and compatibility. Keep both consistent.

Plugins run inside the host process with normal .NET access. They are not isolated from files or network resources by a security sandbox. Install code that you trust.

[Advanced working examples](advanced-examples.md) cover exported contracts, typed services and messages, Beacon events and snapshots, and scoped packet work.


## Test with MCC

Use [MCC plugin management](../managing.md) to load a local package. You do not need MCC source to build a plugin. The optional DMCBK verification programs require its sample checkout. They do not change your plugin project dependencies.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.

## Agent skills

Install the [plugins authoring skill](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/dmcbk-plugin-authoring) for your coding agent:

```bash
npx skills add MCCTeam/MCC-Skills --skill dmcbk-plugin-authoring
```

The skill includes standalone references and examples. Read [agent skill installation](../../contributing/agent-skills.md) for agent selection and installation scope.
