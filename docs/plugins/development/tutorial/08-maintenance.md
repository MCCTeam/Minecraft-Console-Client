# Chapter 8: Maintain and troubleshoot the plugin

Keep the source package and compiled package tests in your development workflow. Test the artifact that you intend to publish.

## Diagnose a failed load

1. Check that the package has `plugin.toml` at its root.
2. Check that the declared entry exists.
3. Check the host's DMCBK and UMPK versions.
4. Check the plugin's required capabilities.
5. Read the individual plugin state.
6. Read the runtime compiler or activation diagnostic.

A successful author build does not prove successful source loading. Runtime source compilation needs explicit namespaces and accessible reference assemblies.

| Symptom | Check |
| --- | --- |
| The command prints a dotted key | The English language file and lookup key |
| The count resets after reload | The assigned storage path and `Storage.Save()` |
| The count rises by an unexpected amount | The generated user settings file |
| Beacon reports a missing provider | Initialization order and individual loaded state |
| A helper DLL cannot load | `deps`, packaged paths, and contract sharing |
| A native library cannot load | Process target, native files, and dependency metadata |
| A command remains after unload | Scoped registration and retained references |
| The callback does not run in a test | Wait condition, session start, and test budget |

## Extend the plugin

Add one behavior at a time. Give every resource an owner.

Use `SessionCreated` for observations that must include handshake or login. Use `SessionStarted` for play behavior. Use the session's cancellation token for work bound to that connection.

Check the matching tracking and action capabilities before game operations. A connected session does not imply that every feature is enabled.

For another plugin's service, declare the dependency and contract. Do not copy a contract DLL privately into both packages. The CLR needs a shared type identity.

For an optional provider, request its service at the point of use. Handle absence without disabling the plugin's unrelated behavior.

## Change a released plugin

1. Choose a new SemVer version.
2. Update code and manifest versions.
3. Test old stored data with the new version.
4. Test missing and invalid settings.
5. Test reconnect and unload.
6. Build each declared release asset.
7. Test the staged assets.
8. Publish new immutable assets.

Do not replace the bytes of an existing release. A catalogue checksum identifies those bytes. A broken release can be yanked while its history remains available.

The [plugin references](../index.md) describe extension contracts beyond this tutorial.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
