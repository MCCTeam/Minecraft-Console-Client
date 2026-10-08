# /plugins

List, load, enable, disable and reload plugins.

## Syntax

```text
/plugins [list [outdated|local|enabled|disabled]|load <folder-or-cs>|enable|disable|unload|reload|settings <id|all> regen|reset|install <what> [<version>] [yes]|uninstall <id> [purge]|update [<id>|all] [yes]|outdated|pin <id> [<version>]|unpin <id>|info <id>|deps <id>|search <text> [in <marketplace>]|marketplace [list|add <source> [as <name>]|remove <name>|refresh [<name>]|auto-update <name> off|check|apply]|new <id>|validate [<folder>]|doctor] [plugin id|all]
```

## Forms

- `/plugins list`: every discovered plugin and its state
- `/plugins enable <id>`: enable one
- `/plugins disable <id>`: disable one
- `/plugins load <path>`: load a folder or a .cs file
- `/plugins unload <id|all>`: unload
- `/plugins reload [id]`: reload one, or all of them
- `/plugins settings <id|all> regen`: rewrite settings.toml, keeping the values
- `/plugins settings <id|all> reset`: rewrite settings.toml from the defaults
- `/plugins list <filter>`: outdated, local, enabled or disabled
- `/plugins install <what> [<version>] [yes]`: from owner/repo, a Git or archive URL, or a folder
- `/plugins uninstall <id> [purge]`: remove it; purge deletes its settings and data too
- `/plugins update [<id>|all] [yes]`: fetch newer versions of what MCC installed
- `/plugins outdated`: what has a newer version, and what is held back
- `/plugins pin <id> [<version>]`: hold a plugin where it is
- `/plugins unpin <id>`: let it move again
- `/plugins info <id>`: manifest, source, dependencies, languages
- `/plugins deps <id>`: what it needs, and what needs it
- `/plugins search <text> [in <marketplace>]`: look through the added marketplaces
- `/plugins marketplace list`: which marketplaces are added
- `/plugins marketplace add <source> [as <name>]`: from owner/repo, a URL, or a folder
- `/plugins marketplace remove <name>`: forget one; its plugins stay installed
- `/plugins marketplace refresh [<name>]`: re-fetch the catalogues
- `/plugins marketplace auto-update <name> off|check|apply`: what it may do at startup
- `/plugins new <id>`: scaffold a plugin folder that follows every convention
- `/plugins validate [<folder>]`: check one folder, or every installed plugin
- `/plugins doctor`: one report on everything wrong or worth knowing
- `/plugins ui`: open the visual plugin manager (dialog hosts)
- `/plugins rollback <transaction>`: Restore the retained installation transaction.
- `/plugins install <what> [version] [--source] [--source-fallback] [--prerelease] [yes]`: Install a selected compatible release asset.

## Flags

| Flag | Meaning |
| --- | --- |
| `--source` | Prefer the release's source asset. |
| `--source-fallback` | Allow source fallback for a mixed release. |
| `--prerelease` | Permit prerelease candidates. |
| `yes` | Accept the operation confirmation. |

## Examples

Type these lines inside MCC. Replace names, coordinates, IDs, and paths with values from your installation.

```text
/plugins list
/plugins info auto-fishing
/plugins deps auto-fishing
/plugins settings all regen
/plugins marketplace list
/plugins search fishing
/plugins install ./Downloads/my-plugin yes
/plugins new hello-world
/plugins validate
/plugins doctor
```

## Behavior and requirements

This command manages the new plugin runtime and schema-2 marketplaces. It can run while MCC has no game connection. A plugin's own actions can still need a session.

`disable` prevents activation. `unload` removes the current loaded instance. `reload` recreates instances and registrations. Managed enabled state belongs in the installation lock, not the immutable package.

`settings regen` preserves values and rewrites comments. `settings reset` replaces values with defaults. `uninstall ... purge` also removes plugin settings and data. Read the confirmation before accepting it.

`install` selects one compatible asset per plugin in the dependency graph. A mixed release needs `--source-fallback` before installation may use source when a suitable compiled asset is absent. `--source` explicitly prefers source.

`pin` holds a managed plugin version. `unpin` permits future updates. `rollback <transaction>` refers to a retained installation transaction, not a guessed plugin version. Inspect operation output and the marketplace guide for transaction identifiers.

`marketplace` and its shorter form `market` manage catalogue bindings and update policy. Removing a marketplace does not uninstall its plugins. Old manifest and catalogue schemas are rejected.

Read [using plugins](../plugins/index.md), [the plugin tutorial](../plugins/development/tutorial/index.md), and [marketplaces](../marketplaces/index.md) for complete workflows.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
