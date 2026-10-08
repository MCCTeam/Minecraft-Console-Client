# /help

Show what a command does and how to type it.

## Syntax

```text
/help [command|ui]
```

## Forms

| Form | Meaning |
| --- | --- |
| `/help` | list every command, grouped |
| `/help <command>` | show one command's page |
| `/help ui` | open the searchable command and manual browser |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/help move
/help inventory
/help ui
```

## Behavior and requirements

`/help` reads the live command registry. Plugins and Beacon scripts can add commands. Unloading them removes their scoped commands.

`/help <command>` shows syntax, examples, flags, and requirements. Some commands also accept `/help <command> <topic>` for a subtopic. `/help ui` opens a searchable browser in TUI mode.

The browser's Insert command action fills the input for review. It does not execute anything.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
