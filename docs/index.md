# MCC 2.0 documentation

Minecraft Console Client (MCC) connects to Minecraft Java Edition servers from a terminal. You can chat, inspect the game, control your player, and run automation without a graphical Minecraft window.

::: warning Early development build
MCC 2.0 is an early development build. Features can change. Keep a copy of your configuration before an update.
:::

## Start here

| Your goal | Read |
| --- | --- |
| Install and open MCC | [Installation](getting-started/installation.md) |
| Connect for the first time | [Your first session](getting-started/first-session.md) |
| Understand account sign-in | [Accounts and authentication](client/accounts.md) |
| Change settings or save servers | [Configuration](client/configuration.md) and [Servers](client/servers.md) |
| Use the full terminal interface | [Classic console and TUI](client/interface.md) |
| Understand a technical term | [Glossary](client/glossary.md) |
| Find a command | [Command reference](commands/index.md) |
| Write a Beacon script | [Beacon guide](beacon/index.md) and [tutorial](beacon/guide/index.md) |
| Install or write a plugin | [Plugin guide](plugins/index.md) and [tutorial](plugins/development/tutorial/index.md) |
| Add or create a marketplace | [Marketplace guide](marketplaces/index.md) |
| Run MCC in a container | [Docker Compose](deployment/docker.md) |
| Fix a problem | [Troubleshooting](troubleshooting/index.md) |
| Use a coding agent for scripts or plugins | [Agent skills](contributing/agent-skills.md) |
| Translate MCC or these pages | [Translations](contributing/translations.md) |

## Choose a learning path

For your first session, complete [installation](getting-started/installation.md), then follow [the first-session tutorial](getting-started/first-session.md). That tutorial explains where to type each command and how to stop the client.

For automation, start with Beacon. A `.bcn` file can respond to chat, events, and timers. The Beacon guide includes a chaptered tutorial and reference material.

For a C# extension, use the plugin guide. It covers source plugins, compiled plugins, settings, lifecycle events, commands, packaging, and testing. The official plugin pages explain each maintained plugin's setup.

## Read examples correctly

A `bash` or `powershell` block runs in your operating system's terminal. A `text` block that starts with `/` runs in MCC. A `toml` block belongs in the named configuration file. A `beacon` block belongs in a `.bcn` script.

Replace a value such as `<server>` with your own value. Do not type the angle brackets. In syntax descriptions, `[value]` means optional. The symbol `|` separates alternatives. Choose one alternative.

Examples use the default MCC command prefix, `/`. If you change that prefix, use your configured prefix instead.

## What MCC can show

MCC can show chat, player lists, inventory containers, health, effects, recipes, entities, advancements, chunks, maps, books, and server dialogs. Available data depends on the Minecraft protocol and the enabled features. An empty view can mean that the server sent no data.

MCC does not render a graphical Minecraft world. A terminal map shows data already available to the client. Server rules and permissions still control actions.

[Continue to installation](getting-started/installation.md).
