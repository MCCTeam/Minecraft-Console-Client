# Chapter 6: Load the plugin and test its behavior

An author build checks C# references. Runtime loading also checks the manifest, source compiler, activation, and registration ownership.

Restore the verifier project before using offline commands. Use the local-feed instructions if the preview is not on your configured source.

The verification program uses `DMCBK.Testing`. Its fake server exchanges real protocol frames through in-memory pipes. It opens no socket and requires no Minecraft account.

## Run the supplied verification program

Use the optional DMCBK sample checkout from [Chapter 1](01-project.md#optional-advanced-verification). Run this command from that checkout:

```sh
dotnet run --project samples/PluginAuthoring/VerifyPlugin -- samples/PluginAuthoring/SessionJournal
```

If you created your own package folder, pass its absolute path as the final argument.

The expected final line is:

```text
PASS: load, two fresh sessions, storage, reload, Beacon and command cleanup.
```

The [complete program](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/VerifyPlugin/Program.cs) performs these checks:

1. Load the actual package folder through the plugin runtime.
2. Check the individual plugin's loaded state.
3. Check the command before connecting.
4. Start two separate sessions.
5. Wait for each session callback.
6. Check the saved storage file.
7. Reload the plugin.
8. Check that the restored count is two.
9. Call the Beacon function.
10. Unload the plugin.
11. Check that its command no longer succeeds.

`WaitForAsync` checks work performed on the event thread. A fixed sleep can read state too early on a slow machine.

The verifier creates two harness clients that share one temporary data root. Each client runs one session. The second client restores the first count. The verifier removes the root at the end.

The harness closes its in-memory transport after a session. Repeating RunSessionAsync on that same harness cannot test reconnect. This verifier checks separate session callbacks and restart persistence. Test same-client reconnect with a controlled reusable server.

## Make a real host load the plugin

Use the [local loading example](https://github.com/MCCTeam/DMCBK/blob/master/docs/guides/plugins.md#load-a-local-plugin). Supply absolute paths to `PluginOptions`.

Declare the package folder in `DevelopmentFolders`. Supply a separate plugin root for user data and caches. Compose Commands before Plugins.

Check both the operation result and `runtime.List()`. An aggregate result can succeed while a particular plugin remains unloaded.

Initialize Beacon before loading this tutorial plugin. Its manifest requires both `commands` and `beacon`.

## Understand the test limits

The test proves source compilation, session callbacks, persisted data, reload, command dispatch, Beacon integration, and command cleanup.

It does not prove public-server compatibility, online authentication, native loading, or game actions. This plugin does not perform game actions.

For a plugin that sends packets or changes inventory, inspect outgoing frames in the harness. Then test the intended behavior on a controlled server.

Next: [Compiled packages and release](07-package-and-release.md).


## Load your package in MCC

1. Start MCC in a separate runtime directory.
2. Keep the Minecraft session disconnected during the first test.
3. Enter `/plugins validate /absolute/path/to/your-package`.
4. Enter `/plugins load /absolute/path/to/your-package`.
5. Enter `/plugins list`.
6. Check that your plugin reports a loaded state.
7. Connect to your controlled test server.
8. Check the callback result.
9. Enter `/plugins unload session-journal`.

Use `/plugins install /absolute/path/to/your-package` for a persistent installation. Review its plan before you accept it. The installer copies package files into immutable storage. Editing the author folder after installation does not edit that installed copy.

See [MCC plugin management](../../managing.md) for data locations, reloads, and diagnostics.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
