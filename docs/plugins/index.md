# Plugins

Plugins add automation, chat bridges, file logging, commands, and external services to MCC. Each plugin runs in the MCC process. Install a plugin only when you trust its publisher and code.

MCC 2.0 uses DMCBK plugins and schema 2. Old ChatBot source, MCC SDK binaries, and old marketplace manifests do not load as new plugins.

## Start with your goal

| You want to | Read |
| --- | --- |
| Install, enable, update, or remove a plugin | [Manage plugins](managing.md) |
| Choose an official plugin | [Official plugin reference](official.md) |
| Find packages from a publisher | [Use marketplaces](../marketplaces/using.md) |
| Write a small C# plugin | [First plugin](development/getting-started.md) |
| Build a complete plugin project | [Eight-chapter plugin tutorial](development/tutorial/index.md) |
| Publish plugin versions and platform assets | [Create a marketplace](../marketplaces/creating.md) |

## Know the four separate states

A package can be installed without being enabled. An enabled package can fail to load. A loaded plugin can keep its own behavior disabled through an `Enabled` setting. Finally, a game feature may need a connected session.

Check `/plugins list` for activation. Check `/plugins info <id>` for package details and failure messages. Read each plugin settings file to check its own behavior switch.

Source packages contain one C# entry file. Compiled packages contain a DLL and private dependencies. A marketplace can offer both kinds, including several compiled platform assets, for one plugin version. MCC selects one asset per plugin in the resolved dependency graph.

[Command reference](managing.md#plugin-command-reference) · [Beacon integration](development/beacon-integration.md)
