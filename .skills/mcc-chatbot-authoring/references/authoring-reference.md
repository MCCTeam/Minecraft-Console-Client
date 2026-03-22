# MCC ChatBot Reference

Self-contained authoring notes for Minecraft Console Client chat bots.

## Bot types

MCC supports two common authoring paths:
- standalone script bots loaded at runtime with `/script`
- built-in bots compiled into the MCC codebase

Default to a standalone `/script` bot unless the user explicitly asks for a built-in bot or repo wiring.

## Embedded current patterns

This skill is intended to work even without an MCC checkout. The patterns below capture the important behavior that would otherwise be borrowed from current repo examples.

If the local repo is available, you can verify against files such as `TestBot.cs`, `RemoteControl.cs`, `FollowPlayer.cs`, `ItemsCollector.cs`, and `Farmer.cs`. If it is not available, use the embedded patterns here directly.

### Minimal chat parsing pattern

Use this as the baseline for public/private chat handling:

```csharp
public override void GetText(string text)
{
    string message = "";
    string sender = "";
    text = GetVerbatim(text);

    if (IsPrivateMessage(text, ref message, ref sender))
    {
        LogToConsole("PM from " + sender + ": " + message);
    }
    else if (IsChatMessage(text, ref message, ref sender))
    {
        LogToConsole("Chat from " + sender + ": " + message);
    }
}
```

What matters:
- normalize first with `GetVerbatim(text)`
- handle PMs before public chat if both matter
- keep simple chat bots deterministic and small

### Owner-gated PM control pattern

Use this when a bot owner should be able to whisper MCC internal commands:

```csharp
public override void GetText(string text)
{
    text = GetVerbatim(text).Trim();
    string command = "";
    string sender = "";

    if (IsPrivateMessage(text, ref command, ref sender)
        && Settings.Config.Main.Advanced.BotOwners.Contains(sender.ToLowerInvariant()))
    {
        CmdResult result = new();
        PerformInternalCommand(command, ref result);
        SendPrivateMessage(sender, result.ToString());
    }
}
```

What matters:
- `PerformInternalCommand(...)` is for MCC commands, not server chat commands
- owner gating should use `Settings.Config.Main.Advanced.BotOwners`
- if `CmdResult` is used in a standalone script, add `//using MinecraftClient.CommandHandler`

### Periodic work pattern

Use `Update()` plus a counter or timestamp for simple repeated work:

```csharp
private int count = 0;

public override void Update()
{
    count++;
    if (count < Settings.DoubleToTick(60))
        return;

    count = 0;
    SendText("/list");
}
```

What matters:
- avoid a worker thread for simple periodic loops
- avoid `Thread.Sleep(...)` inside `Update()`
- if sending chat, do it from a join-safe path like `Update()` or `AfterGameJoined()`, not `Initialize()`

### Built-in Brigadier command pattern

Use this for built-in command bots:

```csharp
public override void Initialize()
{
    McClient.dispatcher.Register(l => l.Literal("help")
        .Then(l => l.Literal(CommandName)
            .Executes(r => OnCommandHelp(r.Source, string.Empty))
        )
    );

    McClient.dispatcher.Register(l => l.Literal(CommandName)
        .Then(l => l.Literal("stop")
            .Executes(r => OnCommandStop(r.Source)))
        .Then(l => l.Literal("_help")
            .Executes(r => OnCommandHelp(r.Source, string.Empty))
            .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
    );
}

public override void OnUnload()
{
    McClient.dispatcher.Unregister(CommandName);
    McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
}
```

What matters:
- register commands in `Initialize()`
- unregister the command tree in `OnUnload()`
- remove the help child you added in `OnUnload()`
- prefer this over legacy command wrappers for new built-in work

### Built-in config and wiring pattern

Use this as the default built-in shape:

```csharp
public class ExampleBot : ChatBot
{
    public static Configs Config = new();

    [TomlDoNotInlineObject]
    public class Configs
    {
        public bool Enabled = false;

        public void OnSettingUpdate()
        {
        }
    }
}
```

Typical host wiring shape:

```csharp
[TomlPrecedingComment("$ChatBot.ExampleBot$")]
public ChatBots.ExampleBot.Configs ExampleBot
{
    get { return ChatBots.ExampleBot.Config; }
    set { ChatBots.ExampleBot.Config = value; ChatBots.ExampleBot.Config.OnSettingUpdate(); }
}
```

```csharp
if (Config.ChatBot.ExampleBot.Enabled) { BotLoad(new ExampleBot()); }
```

What matters:
- built-in configurable bots default to `Enabled = false`
- `OnSettingUpdate()` is the place to normalize config values
- built-in delivery is incomplete without both config wiring and load registration

### Movement gating pattern

Use this shape when a built-in bot owns movement:

```csharp
public override void Initialize()
{
    if (!GetEntityHandlingEnabled())
    {
        LogToConsole("Entity handling is required.");
        UnloadBot();
        return;
    }

    if (!GetTerrainEnabled())
    {
        LogToConsole("Terrain handling is required.");
        UnloadBot();
        return;
    }
}
```

```csharp
var movementLock = BotMovementLock.Instance;
if (movementLock is { IsLocked: true })
    return;

movementLock?.Lock("Example Bot");
```

```csharp
public override void OnUnload()
{
    BotMovementLock.Instance?.UnLock("Example Bot");
}
```

What matters:
- guard terrain and entity handling before movement logic
- built-in movement bots should use `BotMovementLock`
- release the lock on every stop path, including unload and disconnect-sensitive flows

### Dropped-item collector pattern

Use this as the standalone item-search baseline:

```csharp
private DateTime nextScan = DateTime.MinValue;

public override void Update()
{
    var now = DateTime.UtcNow;
    if (now < nextScan || ClientIsMoving())
        return;

    nextScan = now.AddSeconds(1);

    var here = GetCurrentLocation();
    var target = GetEntities().Values
        .Where(entity => entity.Type == EntityType.Item && entity.Location.Distance(here) <= 15)
        .OrderBy(entity => entity.Location.Distance(here))
        .FirstOrDefault();

    if (target != null)
        MoveToLocation(target.Location);
}
```

What matters:
- simple standalone collectors do not need a worker thread
- simple standalone collectors also do not need `BotMovementLock` by default
- `GetEntities()` plus distance ordering is the core search pattern

### Inventory selection pattern

Use this as the default hotbar-switch pattern:

```csharp
private bool TrySwitchToItem(ItemType itemType)
{
    var inventory = GetPlayerInventory();

    var hotbarSlots = inventory.SearchItem(itemType)
        .Where(slot => slot >= 36 && slot <= 44)
        .ToArray();

    if (hotbarSlots.Length == 0)
        return false;

    ChangeSlot((short)(hotbarSlots[0] - 36));
    return true;
}
```

What matters:
- guard with `GetInventoryEnabled()`
- search inventory snapshots, but mutate real server state with helpers like `ChangeSlot(...)`
- do not treat local `Container.Items` mutation as real inventory manipulation

Use the older config examples only for ideas, not as primary scaffolding.

## Standalone script format

A standalone script bot has two parts in this order:
1. metadata block
2. one or more C# classes, with the main bot class inheriting `ChatBot`

Required metadata rules:
- line 1 must be exactly `//MCCScript 1.0`
- metadata must include `MCC.LoadBot(new BotClassName());`
- metadata ends with `//MCCScript Extensions`
- optional metadata directives use `//using Namespace` and `//dll SomeLibrary.dll`
- do not insert a space after `//` in metadata directives

Typical runtime flow:
- place the script file beside MCC
- connect to a server
- load it with `/script YourBotFile.cs`

### Namespace linking for inventory code

If a standalone script uses inventory-specific types such as `Container`, `ItemType`, `WindowActionType`, or `ItemMovingHelper`, add this metadata import:

```csharp
//using MinecraftClient.Inventory
```

For built-in bots, use a normal C# import:

```csharp
using MinecraftClient.Inventory;
```

## Lifecycle summary

Common lifecycle hooks:
- `Initialize()`
  called once when the bot loads; use it for cheap setup only
- `AfterGameJoined()`
  called after the server has been joined successfully, and again after reconnecting; use it when chat can be sent
- `Update()`
  called roughly every 100 ms
- `OnUnload()`
  called when the bot unloads; release resources here
- `OnDisconnect(DisconnectReason reason, string message)`
  called on disconnect; stop background work and clean up reconnect-sensitive state here

Important rule:
- do not send chat from `Initialize()`; use `AfterGameJoined()` instead
- prefer `Initialize()` over constructors for environment checks and resource setup

## Common event hooks

Useful event hooks include:
- `GetText(string text)`
- `GetText(string text, string? json)`
- `OnPlayerJoin(Guid uuid, string name)`
- `OnPlayerLeave(Guid uuid, string? name)`
- `OnEntitySpawn(Entity entity)`
- `OnEntityDespawn(Entity entity)`
- `OnEntityMove(Entity entity)`
- `OnHealthUpdate(float health, int food)`
- `OnMapData(...)`
- `OnInventoryUpdate(int inventoryId)`
- `OnPluginMessage(string channel, byte[] data)`
- `OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)`

Only override hooks that actually exist in the target MCC ChatBot API.

## Common helpers

Text and messaging helpers:
- `GetVerbatim(text)` strips Minecraft formatting codes
- `IsChatMessage(text, ref message, ref sender)` parses public chat
- `IsPrivateMessage(text, ref message, ref sender)` parses private chat
- `IsValidName(username)` validates a Minecraft username
- `SendText(text)` sends chat or server commands
- `SendPrivateMessage(player, message)` sends a private message
- `PerformInternalCommand(command, ...)` runs an internal MCC command, not a server command
- `LogToConsole(text)` writes a bot-prefixed console message

Lifecycle and threading helpers:
- `InvokeOnMainThread(...)`
- `ScheduleOnMainThread(...)`
- `ReconnectToTheServer(...)`
- `UnloadBot()`
- `BotLoad(chatBot)`
- `RunScript(filename, ...)`

World and player-state helpers:
- `GetWorld()`
- `GetEntities()`
- `GetCurrentLocation()`
- `ClientIsMoving()`
- `GetOnlinePlayers()`
- `GetOnlinePlayersWithUUID()`
- `GetServerTPS()`
- `GetProtocolVersion()`

Movement and inventory helpers:
- `MoveToLocation(...)`
- `LookAtLocation(...)`
- `GetInventoryEnabled()`
- `GetPlayerInventory()`
- `GetInventories()`
- `GetItemMovingHelper(...)`
- `WindowAction(...)`
- `ChangeSlot(...)`
- `GetCurrentSlot()`
- `UseItemInHand()`
- `UseItemInLeftHand()`
- `CloseInventory(...)`
- `DigBlock(...)`
- `InteractEntity(...)`

## Inventory notes

Inventory handling is optional in MCC. Check `GetInventoryEnabled()` before relying on inventory state or mutation.

Important behavior:
- `GetPlayerInventory()` returns a snapshot copy of the player's inventory
- `GetInventories()` returns current container snapshots
- writing to those `Container` objects locally does not update the server
- to actually change inventory state, use `ChangeSlot(...)`, `WindowAction(...)`, `GetItemMovingHelper(...)`, `UseItemInHand()`, or related helpers

Useful practical facts:
- hotbar selection uses `ChangeSlot(0..8)`
- hotbar slots are commonly `36..44` in inventory slot numbering
- the offhand slot is commonly `45`
- `Container.SearchItem(...)` is the normal way to locate items by type

Good inventory workflow:
1. guard with `GetInventoryEnabled()`
2. read the current container using `GetPlayerInventory()`
3. locate slots with `SearchItem(...)` or `Items`
4. mutate server state using `ChangeSlot(...)`, `WindowAction(...)`, or `ItemMovingHelper`
5. if needed, react to `OnInventoryUpdate(...)`, `OnInventoryOpen(...)`, or `OnInventoryClose(...)`

Plugins and channels:
- `RegisterPluginChannel(channel)`
- `UnregisterPluginChannel(channel)`
- `SendPluginChannelMessage(channel, data, ...)`

## Built-in bot pattern

A built-in bot usually follows this shape:
- a class that inherits `ChatBot`
- an optional static `Config` field
- a nested `[TomlDoNotInlineObject]` `Configs` class for settings
- an `Enabled = false` setting by default
- `OnSettingUpdate()` to normalize or validate config values

If the bot is configurable, the host codebase usually also needs:
- config wiring in the chat-bot config model
- load registration so enabled bots are instantiated automatically

In this MCC checkout, the usual built-in wiring points are:
- `MinecraftClient/Settings.cs` inside `Settings.ChatBotConfigHealper.ChatBotConfig`
- `MinecraftClient/McClient.cs` inside `RegisterBots(...)`

Match the surrounding `[TomlPrecedingComment(...)]`, property-forwarding, and `BotLoad(new YourBot())` style instead of inventing a different config path.
When presenting built-in wiring, prefer literal code snippets or patch hunks for those two edits so the wiring can be checked directly.

If the bot adds user-facing settings or messages, follow the host codebase's localization and config-comment conventions instead of scattering hardcoded strings.

## Command pattern

For standalone script bots, prefer chat or PM handling in `GetText(...)` unless the user explicitly asks for built-in command registration.

For built-in commands, prefer the current Brigadier dispatcher pattern:
- register commands in `Initialize()`
- add a help entry if the bot exposes commands
- unregister the command tree in `OnUnload()`
- remove any help child added during registration in `OnUnload()`

Avoid using legacy command wrappers if the current codebase uses direct dispatcher registration.
In this checkout, treat direct `McClient.dispatcher.Register(...)` usage in current built-in bots as the source of truth.

## Concurrency and cleanup

If the bot starts background work:
- stop it in `OnUnload()`
- stop it in `OnDisconnect(...)`
- consider resetting state in `AfterGameJoined()` after relog
- prefer `Update()` plus counters or timestamps over unmanaged threads when the task is simple periodic work

If the bot controls movement:
- use a movement-lock discipline
- release the lock on every stop path
- avoid fighting other movement bots
- `BotMovementLock` is mainly for built-in bots or shared long-running automation; a simple standalone script that just calls `MoveToLocation(...)` does not need it by default

When interacting with client state from background logic, use the main-thread helpers when required by the codebase.

## Practical defaults

For simple chat bots:
- normalize text with `GetVerbatim(text)`
- inspect private chat first if the bot listens for whispers
- then inspect public chat
- keep response logic small and deterministic

For long-running automation bots:
- guard prerequisites early, such as entity handling or terrain support
- fail fast with a clear log message if prerequisites are missing
- release all ongoing work cleanly on unload and disconnect

## Common pitfalls

- Incorrect metadata line 1 will break standalone script loading.
- Missing `MCC.LoadBot(new BotClassName())` will prevent standalone script registration.
- Sending chat in `Initialize()` is too early.
- Doing prerequisite checks or unloading from the constructor is harder to reason about than using `Initialize()`.
- Parsing raw formatted text without `GetVerbatim()` causes brittle chat matching.
- Inventing methods not present in the MCC ChatBot API leads to dead code.
- Built-in bot work is incomplete if config or registration wiring is missing.
- Command bots are incomplete if they register commands but do not unregister them.
- `RegisterChatBotCommand(...)` comes from older samples and is not a reliable current pattern for this checkout.
- `ChatBotCommand` exists, but the current built-in bots use Brigadier directly; do not prefer `ChatBotCommand` for new work.
- Blocking `Thread.Sleep(...)` inside `Update()` is a bad default. Prefer timers, counters, or timestamp-based scheduling.
- Mutating the `Container` returned by `GetPlayerInventory()` does not change the server. Use inventory actions instead.
