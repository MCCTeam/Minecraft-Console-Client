# Manage plugins

Enter the commands on this page into the MCC prompt. The examples use the default internal prefix `/`. Unprefixed input can become server chat. If you changed the internal prefix, use that prefix instead. Shell commands such as `dotnet` belong in your operating-system terminal.

## Install your first package

1. Obtain a plugin archive from a trusted publisher.
2. Keep the session disconnected for the first test.
3. Enter `/plugins install /absolute/path/to/plugin.zip`.
4. Read the proposed package and dependency changes.
5. Accept the installation only if those changes match your intent.
6. Enter `/plugins enable <id>` with the package ID.
7. Enter `/plugins list`.
8. Check that the plugin reports a loaded state.
9. Configure its settings before you connect.

Replace `<id>` with the manifest ID, such as `auto-fishing`. Do not type the angle brackets. A source package folder also works with `/plugins install /absolute/path/to/folder`. A compiled package folder must contain its DLL, not only its source project.

`/plugins load /absolute/path/to/folder` loads a development folder. It helps you test files that you still edit. Use installation for managed persistent packages and version history.

The command can accept a direct archive URL or a Git source. Git imports record an immutable revision. A repository import still needs a valid schema-2 manifest and usable entry payload. It does not build arbitrary project files.

## Find the files

The plugin root normally sits beside the configurations directory. For example:

```text
mcc-data/
├── configurations/
│   ├── client.toml
│   └── marketplaces.toml
└── plugins/
    ├── versions/<id>/<version>/<target>/<sha256>/
    ├── userdata/<id>/settings.toml
    ├── userdata/<id>/data/
    ├── cache/
    ├── transactions/
    └── plugins.lock.toml
```

`MCC_PLUGINS` overrides the plugin root. Set an absolute path to avoid dependence on your working directory.

The `versions` directory contains immutable packages. The `userdata` directory contains your settings and saved data. The installation lock selects active versions and records enable state, pins, and source bindings.

Edit user settings, not package defaults. Do not edit the lock while MCC runs. Updates preserve settings and data. Back up the entire plugin root when you need packages, data, and rollback history together.

## Change settings

1. Enter `/plugins info <id>`.
2. Locate `userdata/<id>/settings.toml` under the plugin root.
3. Stop MCC before changing a setting that the plugin also writes.
4. Change the required values.
5. Save the file.
6. Start MCC again.
7. Check the plugin state and behavior.

For an ordinary settings edit while MCC remains open, use `/plugins reload <id>` after saving. Some plugins provide commands that change and save settings directly. Use those commands where their page recommends them.

`/plugins settings <id> regen` regenerates comments and preserves values. `/plugins settings <id> reset` replaces values with defaults. Back up your settings before a reset. The `all` form applies to loaded plugins with a known settings type.

The host enable state and a plugin `Enabled` setting are separate. For example, a loaded AutoFishing plugin with `Enabled = false` does not fish. Change its user setting as well as enabling its package.

## Update and pin

```text
/plugins outdated
/plugins update auto-fishing
/plugins pin auto-fishing
/plugins unpin auto-fishing
/plugins update all
```

Read the update plan before accepting it. A provider update can affect several dependent plugins. A pin prevents version changes until you remove it. `/plugins pin <id> <version>` selects a particular pin under the compatibility rules.

Do not enable automatic updates until you understand the publisher and dependency policy. Use [marketplace policies](../marketplaces/using.md#control-automatic-updates) to choose checks or automatic application.

## Stop or remove a plugin

| Command | Result |
| --- | --- |
| `/plugins disable <id>` | Disable activation and retain package, settings, and data |
| `/plugins unload <id>` | Stop the currently loaded instance |
| `/plugins reload <id>` | Replace the running instance and read its settings again |
| `/plugins uninstall <id>` | Remove the active installation and retain user data |
| `/plugins uninstall <id> purge` | Remove the installation and remove its settings and data |

Use `disable` when you want the plugin to remain inactive. Use `unload` during a development test. Reload is not an update download.

Dependency rules can prevent removal or activation when another plugin requires this one. Inspect `/plugins deps <id>` before you remove a provider.

## Plugin command reference

- `/plugins` or `/plugins list`: List discovered plugins and their state
- `/plugins list outdated`: Filter packages with available updates
- `/plugins list local`: Filter development or direct-source packages
- `/plugins list enabled` / `disabled`: Filter by host enable state
- `/plugins load <folder-or-cs>`: Load a development package or source entry
- `/plugins enable <id>` / `disable <id>`: Change activation policy
- `/plugins unload <id|all>`: Unload one instance or all loaded instances
- `/plugins reload [id]`: Reload one plugin, or all if omitted
- `/plugins settings <id|all> regen`: Rewrite settings comments and retain values
- `/plugins settings <id|all> reset`: Restore settings defaults
- `/plugins install <source-or-id> [version-or-range] [flags] [yes]`: Resolve and install a package
- `/plugins uninstall <id> [purge]`: Remove a package, optionally removing user data
- `/plugins update [id|all] [yes]`: Plan updates for one package or all
- `/plugins rollback <transaction>`: Restore a cached prior installation graph
- `/plugins outdated`: List newer versions and reasons updates cannot apply
- `/plugins pin <id> [version]` / `unpin <id>`: Hold or release a version selection
- `/plugins info <id>`: Show manifest, source, status, languages, and dependencies
- `/plugins deps <id>`: Show providers and dependents
- `/plugins search <text> [in <marketplace>]`: Search registered catalogues
- `/plugins marketplace list`: List publisher bindings
- `/plugins marketplace add <source> [as <name>]`: Add an index URL, repository, or folder
- `/plugins marketplace remove <name>`: Remove the binding and retain installed plugins
- `/plugins marketplace refresh [name]`: Obtain current release metadata
- `/plugins marketplace auto-update <name> off|check|apply`: Set marketplace automatic policy
- `/plugins new <id>`: Create a local plugin scaffold
- `/plugins validate [folder]`: Check a folder, or discovered packages if omitted
- `/plugins doctor`: Report runtime and package problems
- `/plugins ui`: Open the visual plugin manager in a supported host

`/plugins market` is an alias of `/plugins marketplace`. Install flags are `--source`, `--source-fallback`, and `--prerelease`. Each flag may appear once. Quote a version range or path that contains spaces.

`yes` accepts the installation or update without an interactive question. Use it only after you understand the changes. It is useful for controlled file-input runs that cannot answer a prompt.

A rollback transaction is an identifier from installation transaction history. Rollback restores package selections and dependencies. It cannot undo server actions or arbitrary data changes made by plugin code.

## Read a failure

| Symptom | Check |
| --- | --- |
| Unknown plugin command | Check that MCC loaded its plugin |
| Enabled but not loaded | Read `/plugins info <id>` and `/plugins doctor` |
| Loaded but inactive | Check the plugin `Enabled` setting and session requirements |
| Version cannot install | Check API, DMCBK, UMPK, host, framework, and dependency ranges |
| Target unavailable | Check platform assets or explicitly allow source fallback |
| Missing dependency | Add an explicit compatible publisher binding |
| Source compiler error | Check entry code, explicit imports, and declared private references |
| Checksum failure | Ask the publisher to correct its archive or digest |
| Settings change disappears | Stop concurrent plugin writes before editing |

[Official plugin reference](official.md) · [Make a plugin](development/index.md) · [Marketplace format](../marketplaces/format.md)
