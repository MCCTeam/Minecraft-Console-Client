# Lifecycle, sessions and commands

A plugin lifetime can contain many connection sessions. Resources need an owner at the same lifetime as their use.

## Lifecycle order

| Hook | When to use it |
| --- | --- |
| `Configure` | Declare identity and a settings type without I/O |
| `ActivateAsync` | Register plugin-lifetime behavior, even when disconnected |
| `BeforeConnect` | Inspect, redirect or veto the connection plan |
| `SessionCreated` | Observe handshake, login and configuration before play |
| `SessionStarted` | Attach work to the new play session |
| `SessionEnded` | Forget session-specific state |
| `ConfigurationReloaded` | Read the supplied configuration snapshot |
| `BeforeExit` | Perform bounded shutdown work while the session may still exist |
| `DeactivateAsync` | Release resources created by the plugin |

A plugin loaded during an active session can receive a session-start notification. It cannot assume it observed that session's creation.

`context.CurrentSession` can be null. `context.Session` requires a session. Do not cache either across reconnects.

Configuration reload supplies a new snapshot. It does not mutate the client's original immutable configuration object.

## Session scope

An `ISessionScope` provides the UMPK client, state, events, actions, scheduler, commands, channels and cancellation.

| Resource | Ownership rule |
| --- | --- |
| Plugin command | Register through `context.Commands` |
| Session command | Register through `session.Commands` |
| Packet observer | Register through `session.ObservePackets` |
| Plugin channel | Register through the session's channel APIs |
| Movement | Acquire through `TryAcquireMovement`, then dispose the lease |
| Session work | Honor `session.Detached` |
| Plugin-created task or socket | Stop and dispose it in `DeactivateAsync` |

Host scopes withdraw their owned registrations on session end or plugin unload. Arbitrary event subscriptions and resources outside those scopes remain the author's responsibility.

1. Keep packet callbacks short.
2. Copy packet payloads before retaining them.
3. Use scoped scheduling for session work.
4. Stop plugin-created background work during deactivation.
5. Honor the deactivation cancellation token.

`context.Cron` has a plugin lifetime and can run while disconnected. A callback must check session availability before using game actions.

## Register a command

This plugin registers `plugin-ping`. Its response comes from the plugin's translation file.

```csharp
using System.Threading.Tasks;
using DMCBK.Core.Commands;
using DMCBK.PluginSdk;
using Umpk.Commands;

public sealed class PingPlugin : IPlugin
{
    public void Configure(PluginDescriptor descriptor) => descriptor.Id = "plugin-ping";

    public Task ActivateAsync(PluginContext context)
    {
        context.Commands.Register(new PingCommand(context.Strings));
        return Task.CompletedTask;
    }

    private sealed class PingCommand(IPluginLocalization strings) : CommandBase
    {
        public override string CmdName => "plugin-ping";
        public override string CmdDesc => strings.Get("ping.description");
        public override string CmdUsage => "plugin-ping";

        public override void Register(CommandBuilder<CommandContext> builder)
        {
            builder.Literal(CmdName, command => command.Executes(
                call => call.Source.Result.Ok(strings.Get("ping.reply"))));
        }
    }
}
```

Add these keys to `lang/en.toml`:

```toml
[ping]
description = "Check that the plugin command is available."
reply = "Plugin is ready."
```

The command returns local output through `CommandContext.Result`. It does not send server chat.

`CmdUsage` is a grammar, such as `plugin-ping`, rather than an instruction sentence. The host supplies help rendering and command prefixes.

For typed arguments, extend the Brigadier command tree. Do not add a separate string parser that bypasses completion and scoped registration.

## Failure handling and presentation

Repeated callback failures count against the plugin's crash budget. A failing plugin can be disabled while other plugins remain active.

Cancellation and teardown do not guarantee rollback of external effects. A sent message or completed HTTP request cannot be undone by unloading an assembly.

Host presentation is optional. Use generic presentation requests for images, inventory, books and dialogs. Terminal dimensions, ANSI drawing and menu navigation belong to the host.

Next: [Settings and resources](settings-and-resources.md).

## Reconnect example explained

Consider a plugin loaded before connection. Activation registers its local command once. The first session callback receives session A. After disconnection, A's detach token cancels and its scopes close. The next callback receives session B.

Register plugin-wide commands once in `ActivateAsync`. Register session commands inside the session callback. Repeating plugin-wide registration on every reconnect can create duplicate registrations.

A plugin loaded during session B can receive `SessionStarted` for B. It cannot inspect earlier handshake packets by waiting for that event. Use `SessionCreated` when those packets matter.

## Read data and request actions

`session.State` supplies the current game state. Reading a snapshot does not change the server. Use `session.Actions` for requested effects.

Tracking can be disabled in the host. A terrain, entity, or inventory snapshot may therefore be unavailable. Check the relevant capability before treating absent data as an empty world.

Movement has shared ownership. Acquire a movement lease before steering. Release the lease when the operation stops. Do not assume that your plugin is the only automation module.

## Keep callback errors visible

Catch errors that your plugin can recover from, such as an unavailable optional service. Let unexpected failures reach the runtime's diagnostics. Broad catch blocks can hide repeated defects and prevent crash-budget accounting.

Avoid `async void` event handlers. The event cannot await their completion, and failures can escape normal callback handling. For asynchronous work, use a scoped scheduler or an explicitly owned task with cancellation.

Do not hold a lock while awaiting I/O. Protect shared fields briefly. Pass a stable copy to asynchronous work outside the lock.

## Test cleanup explicitly

1. Load the plugin.
2. Execute its command.
3. Start a session.
4. Stop the session.
5. Start another session.
6. Check that callbacks run once.
7. Unload the plugin.
8. Check that its command no longer succeeds.

Use these steps to test reconnect on one client. The [tutorial verifier](https://github.com/MCCTeam/DMCBK/blob/master/samples/PluginAuthoring/VerifyPlugin/Program.cs) instead checks one session per fresh client, durable state across those clients, and command withdrawal. It does not execute this same-client reconnect sequence.

## Name client variables

Use only ASCII letters, digits, and underscores in client variable names. For example, use `session_journal_sessions`.

The variable store stops a name at the first unsupported character. A dot or dash can therefore make different names select the same entry.

Plugin IDs still use names such as `session-journal`. Plugin IDs and client variable names follow different rules.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
