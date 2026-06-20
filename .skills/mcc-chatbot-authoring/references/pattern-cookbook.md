# MCC Pattern Cookbook

Concrete patterns for standalone MCC `/script` bots. Use these before inventing new scaffolding.

## Periodic task without threads

Use `Update()` plus a timestamp or counter. This comes from the old `sample-script-with-task.cs` example and still holds up well.

```csharp
public class PeriodicTaskBot : ChatBot
{
    private DateTime nextRun = DateTime.MinValue;

    public override void Update()
    {
        var now = DateTime.UtcNow;
        if (now < nextRun)
            return;

        nextRun = now.AddSeconds(30);
        LogDebugToConsole("Running periodic task");
        SendText("/ping");
    }
}
```

Why this pattern is good:
- stays on MCC's normal tick flow
- avoids background threads for simple periodic work
- keeps the bot responsive to unload and disconnect

## Chat and PM handling

This combines the useful parts of `TestBot`, `sample-script-pm-forwarder.cs`, and `RemoteControl.cs`.

```csharp
public override void GetText(string text)
{
    text = GetVerbatim(text);

    string message = "";
    string sender = "";

    if (IsPrivateMessage(text, ref message, ref sender))
    {
        LogToConsole("PM from " + sender + ": " + message);
        return;
    }

    if (IsChatMessage(text, ref message, ref sender))
    {
        LogToConsole("Chat from " + sender + ": " + message);
    }
}
```

Owner-gated internal command handling:

Add `//using MinecraftClient.CommandHandler` in the script metadata if you use `CmdResult`.

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

## Movement with prerequisite checks

Modern movement code should copy the guard style from current built-in bots, not the older constructor-heavy scripts.

```csharp
public override void Initialize()
{
    if (!GetEntityHandlingEnabled() || !GetTerrainEnabled())
    {
        LogToConsole("Entity handling and terrain handling are required.");
        UnloadBot();
    }
}
```

Simple "look at nearest player" logic adapted from `AutoLook.cs`:

```csharp
private Entity? trackedPlayer = null;

public override void OnEntitySpawn(Entity entity)
{
    TryTrack(entity);
}

public override void OnEntityDespawn(Entity entity)
{
    if (trackedPlayer != null && entity.ID == trackedPlayer.ID)
        trackedPlayer = null;
}

public override void OnEntityMove(Entity entity)
{
    if (!TryTrack(entity))
        return;

    LookAtLocation(entity.Location);
}

private bool TryTrack(Entity entity)
{
    if (entity.Type != EntityType.Player)
        return false;

    if (trackedPlayer == null)
    {
        trackedPlayer = entity;
        return true;
    }

    if (GetCurrentLocation().Distance(entity.Location) < GetCurrentLocation().Distance(trackedPlayer.Location))
        trackedPlayer = entity;

    return trackedPlayer.ID == entity.ID;
}
```

## Search for dropped items and move to them

This is the safest pattern to preserve from `ItemsCollector.cs` for standalone scripts.

```csharp
public class NearbyItemsBot : ChatBot
{
    private DateTime nextScan = DateTime.MinValue;

    public override void Initialize()
    {
        if (!GetEntityHandlingEnabled() || !GetTerrainEnabled())
        {
            LogToConsole("Entity handling and terrain handling are required.");
            UnloadBot();
        }
    }

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
}
```

Why this version is better than older farming scripts:
- no unmanaged worker thread
- no busy wait loop around movement
- uses the current `GetEntities()` pattern

## Search for blocks or crops in the world

The old sugar cane and mining scripts still contain a useful search idea: use `GetWorld().FindBlock(...)`, then filter and sort.

```csharp
var targets = GetWorld()
    .FindBlock(GetCurrentLocation(), Material.SugarCane, 16)
    .Where(block =>
        GetWorld().GetBlock(new Location(block.X, block.Y - 1, block.Z)).Type == Material.SugarCane)
    .OrderBy(block => block.Distance(GetCurrentLocation()))
    .ToList();
```

Use this as a search primitive. Then decide separately how to move, dig, or harvest.

## Inventory access and manipulation

If a standalone script uses inventory types directly, add this import in the metadata block:

```csharp
//using MinecraftClient.Inventory
```

For built-in bots, add:

```csharp
using MinecraftClient.Inventory;
```

Always guard inventory logic first:

```csharp
public override void Initialize()
{
    if (!GetInventoryEnabled())
    {
        LogToConsole("Inventory handling is required.");
        UnloadBot();
    }
}
```

Important rule:
- `GetPlayerInventory()` returns a snapshot copy, so editing its `Items` dictionary does not change the server
- actual changes must go through `ChangeSlot(...)`, `WindowAction(...)`, `GetItemMovingHelper(...)`, `UseItemInHand()`, and related helpers

### Search inventory for an item

This combines the useful current logic from `Farmer.cs` and `AutoEat.cs`.

```csharp
private bool TrySwitchToItem(ItemType itemType)
{
    var inventory = GetPlayerInventory();

    if (inventory.Items.TryGetValue(GetCurrentSlot() - 36, out var held) && held.Type == itemType)
        return true;

    var hotbarSlots = inventory.SearchItem(itemType)
        .Where(slot => slot >= 36 && slot <= 44)
        .ToArray();

    if (hotbarSlots.Length == 0)
        return false;

    ChangeSlot((short)(hotbarSlots[0] - 36));
    return true;
}
```

Use this for simple hotbar selection. For deeper inventory reshuffling, built-in bots usually need more helper logic.

### Move an item into the hotbar

Use this when the item exists in inventory but is not already on the hotbar.

```csharp
private bool TryMoveItemToHotbar(ItemType itemType, short targetHotbarSlot = 0)
{
    var inventory = GetPlayerInventory();
    var matches = inventory.SearchItem(itemType);

    if (matches.Length == 0)
        return false;

    var targetInventorySlot = 36 + targetHotbarSlot;

    if (matches[0] >= 36 && matches[0] <= 44)
    {
        ChangeSlot((short)(matches[0] - 36));
        return true;
    }

    var movingHelper = GetItemMovingHelper(inventory);
    movingHelper.Swap(matches[0], targetInventorySlot);
    ChangeSlot(targetHotbarSlot);
    return true;
}
```

Why this pattern is good:
- it reads the current snapshot first
- it does not pretend local `Container` edits affect the server
- it uses the item-moving helper for real inventory manipulation

### Drop or click items with window actions

Use `WindowAction(...)` when the bot needs direct inventory clicks or dropping behavior.

```csharp
private void DropAllOfType(ItemType itemType)
{
    var inventory = GetPlayerInventory();

    foreach (int slot in inventory.SearchItem(itemType))
        WindowAction(0, slot, WindowActionType.DropItemStack);
}
```

Use this pattern carefully:
- verify the correct inventory ID first
- prefer reacting to `OnInventoryUpdate(...)` for larger inventory workflows
- for crafting or chest workflows, use `GetInventories()` and `CloseInventory(...)` as needed

## Built-in command bot pattern

Only use this when the user explicitly asks for a built-in bot.

```csharp
public override void Initialize()
{
    McClient.dispatcher.Register(l => l.Literal("help")
        .Then(l => l.Literal(CommandName)
            .Executes(r => OnCommandHelp(r.Source, string.Empty))
        )
    );

    McClient.dispatcher.Register(l => l.Literal(CommandName)
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

Use a built-in bot only when the user explicitly asks for compiled MCC behavior or repo wiring.
