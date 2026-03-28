using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftClient.Mcp;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<IMccMcpCapabilities, DeterministicCapabilities>();
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<MccMcpToolSet>();

await builder.Build().RunAsync();

internal sealed class DeterministicCapabilities : IMccMcpCapabilities
{
    private static double C(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public MccMcpResult GetSessionStatus() =>
        MccMcpResult.Ok(new
        {
            connected = true,
            host = "deterministic.local",
            port = 25565,
            username = "HarnessBot",
            location = new { x = C(0.5), y = C(80.0), z = C(0.5) }
        });

    public MccMcpResult GetServerInfo() =>
        MccMcpResult.Ok(new
        {
            host = "deterministic.local",
            port = 25565,
            tps = 20.0
        });

    public MccMcpResult GetPlayerState() =>
        MccMcpResult.Ok(new
        {
            nickname = "HarnessBot",
            username = "HarnessBot",
            health = 20.0f,
            saturation = 20,
            gamemode = 1,
            currentSlot = 1,
            yaw = 0.0f,
            pitch = 0.0f,
            location = new { x = C(0.5), y = C(80.0), z = C(0.5) },
            effects = new object[0]
        });

    public MccMcpResult GetPlayersList() =>
        MccMcpResult.Ok(new
        {
            players = new[] { "HarnessBot", "PlayerOne" }
        });

    public MccMcpResult GetChatHistory(int maxCount, bool includeJson) =>
        MccMcpResult.Ok(new
        {
            count = 2,
            entries = new object[]
            {
                new { timestampUtc = DateTimeOffset.UtcNow.AddSeconds(-10), kind = "chat", text = "<PlayerOne> hello", sender = "PlayerOne", message = "hello", json = includeJson ? "{}" : null },
                new { timestampUtc = DateTimeOffset.UtcNow.AddSeconds(-5), kind = "system", text = "HarnessBot joined the game", sender = (string?)null, message = (string?)null, json = includeJson ? "{}" : null }
            }
        });

    public MccMcpResult GetInternalCommands() =>
        MccMcpResult.Ok(new
        {
            count = 4,
            commands = new[]
            {
                new { name = "debug", usage = "debug [on|off|state]", description = "Toggle debug or print state." },
                new { name = "move", usage = "move <x> <y> <z>", description = "Move to location." },
                new { name = "useitem", usage = "useitem [x] [y] [z]", description = "Use current held item." },
                new { name = "dig", usage = "dig <x> <y> <z> [duration]", description = "Dig block at location." }
            }
        });

    public MccMcpResult GetMaterialsList(string? filter, int maxCount) =>
        MccMcpResult.Ok(new
        {
            total = 3,
            count = 3,
            filter,
            materials = new[]
            {
                new { name = "Air", typeLabel = "Air" },
                new { name = "GrassBlock", typeLabel = "Grass Block" },
                new { name = "OakLog", typeLabel = "Oak Log" }
            }
        });

    public MccMcpResult GetBlockTypesList(string? filter, int maxCount) =>
        MccMcpResult.Ok(new
        {
            total = 3,
            count = 3,
            filter,
            blockTypes = new[]
            {
                new { name = "Air", typeLabel = "Air" },
                new { name = "GrassBlock", typeLabel = "Grass Block" },
                new { name = "OakLog", typeLabel = "Oak Log" }
            }
        });

    public MccMcpResult GetEntityTypesList(string? filter, int maxCount) =>
        MccMcpResult.Ok(new
        {
            total = 3,
            count = 3,
            filter,
            entityTypes = new[]
            {
                new { name = "Player", typeLabel = "Player" },
                new { name = "Item", typeLabel = "Item" },
                new { name = "Villager", typeLabel = "Villager" }
            }
        });

    public MccMcpResult SendChat(string text) =>
        MccMcpResult.Ok(new { echoed = text });

    public MccMcpResult QuitClient() =>
        MccMcpResult.Ok(new { quitting = true });

    public MccMcpResult RunInternalCommand(string command) =>
        MccMcpResult.Ok(new { command, status = "Done", output = "deterministic" });

    public MccMcpResult UseItemOnHand() =>
        MccMcpResult.Ok(new { success = true, action = "use_item_on_hand" });

    public MccMcpResult ChangeHotbarSlot(int slot) =>
        MccMcpResult.Ok(new { success = true, slot });

    public MccMcpResult UseItemOnBlock(double x, double y, double z) =>
        MccMcpResult.Ok(new { success = true, x = C(x), y = C(y), z = C(z), action = "useitem" });

    public MccMcpResult DigBlock(double x, double y, double z, double durationSeconds) =>
        MccMcpResult.Ok(new
        {
            success = true,
            target = new { x = C(x), y = C(y), z = C(z) },
            beforeBlock = new { material = "OakLog", typeLabel = "Oak Log", blockId = 137, blockMeta = 0 },
            afterBlock = new { material = "Air", typeLabel = "Air", blockId = 0, blockMeta = 0 },
            commandAccepted = true,
            changed = true,
            destroyed = true,
            attempts = 1,
            attemptedDurationsSeconds = new[] { durationSeconds > 0 ? durationSeconds : 1.5 },
            distance = 1.5,
            playerLocation = new { x = C(0.5), y = C(80.0), z = C(0.5) }
        });

    public MccMcpResult PlaceBlock(int x, int y, int z, string face, string hand, bool lookAtBlock) =>
        MccMcpResult.Ok(new { success = true, x, y, z, face, hand, lookAtBlock, action = "place_block" });

    public MccMcpResult InteractEntity(int entityId, string interaction, string hand) =>
        MccMcpResult.Ok(new { success = true, entityId, interaction, hand });

    public MccMcpResult ScanNearbyBlocks(int radius, int maxCount, string? materialFilter) =>
        MccMcpResult.Ok(new
        {
            center = new { x = 0, y = 79, z = 0 },
            radius,
            count = 1,
            blocks = new[]
            {
                new { x = 0, y = 79, z = 0, material = materialFilter ?? "GrassBlock", blockId = 9, blockMeta = 0, distance = 0.0 }
            }
        });

    public MccMcpResult FindBlocks(string? query, int radius, int maxCount, bool exactMatch) =>
        MccMcpResult.Ok(new
        {
            center = new { x = 0, y = 79, z = 0 },
            radius,
            query,
            exactMatch,
            count = 2,
            blocks = new object[]
            {
                new { x = 1, y = 79, z = 0, material = "GrassBlock", typeLabel = "Grass Block", blockId = 9, blockMeta = 0, distance = 1.0 },
                new { x = 2, y = 79, z = 0, material = "Dirt", typeLabel = "Dirt", blockId = 10, blockMeta = 0, distance = 2.0 }
            }
        });

    public MccMcpResult IsPlayerNearby(string? playerName, double radius, bool includeSelf) =>
        MccMcpResult.Ok(new
        {
            radius,
            playerName,
            includeSelf,
            anyNearby = true,
            count = 1,
            players = new object[]
            {
                new
                {
                    entityId = 1,
                    uuid = Guid.Empty,
                    name = "PlayerOne",
                    customName = (string?)null,
                    x = C(3.5),
                    y = C(80.0),
                    z = C(0.5),
                    distance = 3.0,
                    latency = 5
                }
            }
        });

    public MccMcpResult LocatePlayer(string playerName, bool includeSelf) =>
        MccMcpResult.Ok(new
        {
            playerName,
            matchedName = "PlayerOne",
            entityId = 1,
            uuid = Guid.Empty,
            x = C(3.5),
            y = C(80.0),
            z = C(0.5),
            distance = 3.0
        });

    public MccMcpResult CanReachPosition(double x, double y, double z, bool allowUnsafe, int maxOffset, int minOffset, int timeoutMs) =>
        MccMcpResult.Ok(new
        {
            reachable = true,
            exactReachable = true,
            target = new { x = C(x), y = C(y), z = C(z) },
            startLocation = new { x = C(0.5), y = C(80.0), z = C(0.5) },
            finalWaypoint = new { x = C(x), y = C(y), z = C(z) },
            finalDistance = 0.0,
            waypointCount = 4,
            allowUnsafe,
            maxOffset,
            minOffset,
            timeoutMs = timeoutMs <= 0 ? 5000 : timeoutMs
        });

    public MccMcpResult MoveTo(double x, double y, double z, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs) =>
        MccMcpResult.Ok(new
        {
            pathFound = true,
            arrived = true,
            tolerance = 1.5,
            verifyWaitMs = 250,
            target = new { x = C(x), y = C(y), z = C(z) },
            startLocation = new { x = C(0.5), y = C(80.0), z = C(0.5) },
            finalLocation = new { x = C(x), y = C(y), z = C(z) },
            finalDistance = 0.0,
            distanceMoved = 3.0,
            allowUnsafe,
            allowDirectTeleport,
            maxOffset,
            minOffset,
            timeoutMs
        });

    public MccMcpResult MoveToPlayer(string playerName, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs) =>
        MccMcpResult.Ok(new
        {
            pathFound = true,
            arrived = true,
            tolerance = 1.5,
            verifyWaitMs = 250,
            target = new
            {
                playerName = "PlayerOne",
                entityId = 1,
                x = C(3.5),
                y = C(80.0),
                z = C(0.5)
            },
            startLocation = new { x = C(0.5), y = C(80.0), z = C(0.5) },
            finalLocation = new { x = C(3.5), y = C(80.0), z = C(0.5) },
            finalDistance = 0.0,
            distanceMoved = 3.0,
            allowUnsafe,
            allowDirectTeleport,
            maxOffset,
            minOffset,
            timeoutMs
        });

    public MccMcpResult LookAt(double x, double y, double z) =>
        MccMcpResult.Ok(new { looked = true, x = C(x), y = C(y), z = C(z) });

    public MccMcpResult GetInventorySnapshot(int inventoryId) =>
        MccMcpResult.Ok(new
        {
            id = inventoryId,
            slots = new[]
            {
                new { slot = 0, type = "Stone", count = 64 }
            }
        });

    public MccMcpResult InventoryWindowAction(int inventoryId, int slotId, string actionType) =>
        MccMcpResult.Ok(new { success = true, inventoryId, slotId, actionType });

    public MccMcpResult DropInventoryItem(string itemType, int count, int inventoryId, bool preferStack) =>
        MccMcpResult.Ok(new
        {
            success = true,
            itemType,
            requestedCount = count,
            droppedCount = count,
            beforeCount = 64,
            afterCount = Math.Max(0, 64 - count),
            inventoryId,
            touchedSlots = new[] { 36 },
            preferStack
        });

    public MccMcpResult QueryEntities(int maxCount) =>
        MccMcpResult.Ok(new
        {
            count = 1,
            entities = new[]
            {
                new { id = 1, type = "Player", x = C(0.5), y = C(80.0), z = C(0.5) }
            }
        });

    public MccMcpResult ListEntities(int maxCount, string? typeFilter, double radius) =>
        MccMcpResult.Ok(new
        {
            totalTracked = 1,
            count = 1,
            entities = new[]
            {
                new
                {
                    id = 1,
                    type = "Player",
                    typeLabel = "Player",
                    uuid = Guid.Empty,
                    name = "HarnessBot",
                    customName = (string?)null,
                    x = C(0.5),
                    y = C(80.0),
                    z = C(0.5),
                    distance = 0.0,
                    health = 20.0f,
                    pose = "Standing",
                    latency = 5
                }
            }
        });

    public MccMcpResult GetEntityInfo(int entityId, bool includeMetadata, bool includeEquipment, bool includeEffects) =>
        MccMcpResult.Ok(new
        {
            id = entityId,
            type = "Player",
            typeLabel = "Player",
            uuid = Guid.Empty,
            name = "HarnessBot",
            customName = (string?)null,
            customNameVisible = false,
            x = C(0.5),
            y = C(80.0),
            z = C(0.5),
            yaw = 0.0f,
            pitch = 0.0f,
            health = 20.0f,
            pose = "Standing",
            latency = 5,
            objectData = -1,
            metadata = includeMetadata ? new { flags = 0 } : null,
            equipment = includeEquipment ? new[] { new { slot = 0, type = "Stone", count = 1 } } : null,
            activeEffects = includeEffects ? new object[0] : null
        });

    public MccMcpResult FindSigns(string text, bool exactMatch, int radius, int maxCount, bool includeBackText) =>
        MccMcpResult.Ok(new
        {
            text,
            exactMatch,
            radius,
            includeBackText,
            count = 1,
            signs = new[]
            {
                new
                {
                    x = 2,
                    y = 80,
                    z = 1,
                    material = "OakSign",
                    typeLabel = "Oak Sign",
                    distance = 1.8,
                    isWaxed = false,
                    frontText = new[] { "home", "storage" },
                    backText = includeBackText ? new[] { "north wall" } : Array.Empty<string>(),
                    matchedLines = new[] { text }
                }
            }
        });

    public MccMcpResult ListItemEntities(string? itemType, double radius, int maxCount) =>
        MccMcpResult.Ok(new
        {
            itemType = itemType ?? "OakLog",
            radius,
            count = 1,
            items = new[]
            {
                new
                {
                    entityId = 99,
                    itemType = "OakLog",
                    typeLabel = "Oak Log",
                    count = 3,
                    x = C(2.5),
                    y = C(80.0),
                    z = C(1.5),
                    distance = 2.24
                }
            }
        });

    public MccMcpResult PickupItems(string itemType, double radius, int maxItems, bool allowUnsafe, int timeoutMs) =>
        MccMcpResult.Ok(new
        {
            itemType,
            radius,
            maxItems,
            allowUnsafe,
            timeoutMs = timeoutMs <= 0 ? 2500 : timeoutMs,
            attempted = 1,
            successfulPickups = 1,
            collectedCount = 3,
            initialInventoryCount = 0,
            finalInventoryCount = 3,
            remainingNearby = 0,
            attempts = new object[]
            {
                new
                {
                    entityId = 99,
                    itemType,
                    typeLabel = "Oak Log",
                    expectedCount = 3,
                    target = new { x = C(2.5), y = C(80.0), z = C(1.5) },
                    pathFound = true,
                    arrived = true,
                    entityGone = true,
                    inventoryDelta = 3,
                    startLocation = new { x = C(0.5), y = C(80.0), z = C(0.5) },
                    finalLocation = new { x = C(2.5), y = C(80.0), z = C(1.5) },
                    finalDistance = 0.0
                }
            }
        });

    public MccMcpResult GetWorldBlockAt(int x, int y, int z) =>
        MccMcpResult.Ok(new { x, y, z, material = "Air", blockId = 0, blockMeta = 0 });
}
