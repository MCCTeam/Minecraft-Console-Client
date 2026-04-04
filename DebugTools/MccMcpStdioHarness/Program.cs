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

builder.Services.AddSingleton(new MccMcpConfig());
builder.Services.AddSingleton<IMccMcpCapabilities, DeterministicCapabilities>();
builder.Services.AddSingleton<MccMcpGuidanceProvider>();
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<MccMcpToolSet>()
    .WithPrompts<MccMcpPromptSet>();

await builder.Build().RunAsync();

internal sealed class DeterministicCapabilities : IMccMcpCapabilities
{
    private static double C(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private readonly List<RecentEvent> recentEvents = [];
    private long nextEventId = 1;
    private double playerX = C(0.5);
    private double playerY = C(80.0);
    private double playerZ = C(0.5);
    private float yaw;
    private float pitch;
    private int currentSlot = 1;
    private bool sneaking;
    private bool sprinting;
    private float health = 20.0f;
    private bool disconnecting;

    public DeterministicCapabilities()
    {
        AddRecentEvent("player_join", new { name = "HarnessBot" });
        AddRecentEvent("inventory_open", new { inventoryId = 1, type = "Generic_9x3", title = "Chest" });
        AddRecentEvent("weather_rain", new { level = 1.0 });
        AddRecentEvent("title", new { text = "mcp_title" });
        AddRecentEvent("actionbar", new { text = "mcp_actionbar" });
    }

    public MccMcpResult GetSessionStatus() =>
        MccMcpResult.Ok(new
        {
            connected = !disconnecting,
            host = "deterministic.local",
            port = 25565,
            username = "HarnessBot",
            location = new { x = playerX, y = playerY, z = playerZ }
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
            health,
            saturation = 20,
            gamemode = 1,
            currentSlot,
            yaw,
            pitch,
            location = new { x = playerX, y = playerY, z = playerZ },
            effects = new object[0]
        });

    public MccMcpResult GetWorldState() =>
        MccMcpResult.Ok(new
        {
            connected = !disconnecting,
            host = "deterministic.local",
            port = 25565,
            username = "HarnessBot",
            protocol = 769,
            terrainEnabled = true,
            inventoryEnabled = true,
            entityHandlingEnabled = true,
            location = new { x = playerX, y = playerY, z = playerZ },
            tps = 20.0,
            dimension = "minecraft:overworld",
            loadedChunkCount = 9,
            pendingChunkCount = 0,
            totalChunkCount = 9,
            loadRatio = 1.0,
            worldAge = 12000L,
            timeOfDay = 6000L,
            rainLevel = 1.0,
            thunderLevel = 0.0
        });

    public MccMcpResult GetChunkStatus(double? x, double? y, double? z)
    {
        double resolvedX = x ?? playerX;
        double resolvedY = y ?? playerY;
        double resolvedZ = z ?? playerZ;
        int chunkX = (int)Math.Floor(resolvedX) >> 4;
        int chunkZ = (int)Math.Floor(resolvedZ) >> 4;

        return MccMcpResult.Ok(new
        {
            location = new { x = C(resolvedX), y = C(resolvedY), z = C(resolvedZ) },
            chunk = new { x = chunkX, z = chunkZ },
            loaded = true,
            fullyLoaded = true,
            loadedChunkCount = 9,
            pendingChunkCount = 0,
            totalChunkCount = 9,
            loadRatio = 1.0
        });
    }

    public MccMcpResult RaycastBlock(double maxDistance, bool includeNeighbors)
    {
        object? neighbors = includeNeighbors
            ? new
            {
                north = new { x = 0, y = 79, z = -1, material = "Air", typeLabel = "Air" },
                south = new { x = 0, y = 79, z = 1, material = "Air", typeLabel = "Air" },
                east = new { x = 1, y = 79, z = 0, material = "Air", typeLabel = "Air" },
                west = new { x = -1, y = 79, z = 0, material = "Air", typeLabel = "Air" },
                above = new { x = 0, y = 80, z = 0, material = "Air", typeLabel = "Air" },
                below = new { x = 0, y = 78, z = 0, material = "Stone", typeLabel = "Stone" }
            }
            : null;

        return MccMcpResult.Ok(new
        {
            hit = true,
            maxDistance,
            playerLocation = new { x = playerX, y = playerY, z = playerZ },
            eyeLocation = new { x = playerX, y = C(playerY + 1.62), z = playerZ },
            location = new { x = 0, y = 79, z = 0 },
            block = new { material = "Stone", typeLabel = "Stone", blockId = 1, blockMeta = 0 },
            distance = 1.12,
            eyeDistance = 2.03,
            neighbors
        });
    }

    public MccMcpResult PreviewPath(double x, double y, double z, bool allowUnsafe, int maxOffset, int minOffset, int timeoutMs, int maxWaypoints)
    {
        object[] waypoints =
        [
            new { x = playerX, y = playerY, z = playerZ },
            new { x = C((playerX + x) / 2), y = C((playerY + y) / 2), z = C((playerZ + z) / 2) },
            new { x = C(x), y = C(y), z = C(z) }
        ];

        return MccMcpResult.Ok(new
        {
            pathFound = true,
            exactReachable = true,
            target = new { x = C(x), y = C(y), z = C(z) },
            startLocation = new { x = playerX, y = playerY, z = playerZ },
            finalWaypoint = new { x = C(x), y = C(y), z = C(z) },
            finalDistance = 0.0,
            waypointCount = waypoints.Length,
            truncated = waypoints.Length > Math.Max(1, maxWaypoints),
            waypoints = waypoints.Take(Math.Max(1, maxWaypoints)).ToArray(),
            allowUnsafe,
            maxOffset,
            minOffset,
            timeoutMs = timeoutMs <= 0 ? 5000 : timeoutMs
        });
    }

    public MccMcpResult GetPlayersList() =>
        MccMcpResult.Ok(new
        {
            players = new[] { "HarnessBot", "PlayerOne" }
        });

    public MccMcpResult GetPlayersDetailed(bool includeSelf, bool includeCoordinates)
    {
        List<object> players = [];
        if (includeSelf)
        {
            players.Add(new
            {
                name = "HarnessBot",
                uuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ping = 5,
                gamemode = 1,
                listed = true,
                displayName = "HarnessBot",
                entityId = 1,
                x = includeCoordinates ? playerX : (double?)null,
                y = includeCoordinates ? playerY : (double?)null,
                z = includeCoordinates ? playerZ : (double?)null
            });
        }

        players.Add(new
        {
            name = "PlayerOne",
            uuid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ping = 12,
            gamemode = 1,
            listed = true,
            displayName = "PlayerOne",
            entityId = 2,
            x = includeCoordinates ? C(3.5) : (double?)null,
            y = includeCoordinates ? C(80.0) : (double?)null,
            z = includeCoordinates ? C(0.5) : (double?)null
        });

        return MccMcpResult.Ok(new
        {
            count = players.Count,
            players = players.ToArray()
        });
    }

    public MccMcpResult GetPlayerStats() =>
        MccMcpResult.Ok(new
        {
            health,
            saturation = 20,
            level = 12,
            totalExperience = 245,
            gamemode = 1,
            playerEntityId = 1,
            currentSlot,
            yaw,
            pitch,
            sneaking,
            sprinting,
            location = new { x = playerX, y = playerY, z = playerZ },
            tps = 20.0
        });

    public MccMcpResult GetStatusEffects() =>
        MccMcpResult.Ok(new
        {
            count = 0,
            effects = Array.Empty<object>()
        });

    public MccMcpResult GetRecentEvents(long afterId, int maxCount, string? typeFilter)
    {
        RecentEvent[] events = recentEvents
            .Where(e => e.Id > afterId)
            .Where(e => string.IsNullOrWhiteSpace(typeFilter) || string.Equals(e.Type, typeFilter, StringComparison.OrdinalIgnoreCase))
            .Take(Math.Max(1, maxCount))
            .ToArray();

        return MccMcpResult.Ok(new
        {
            afterId,
            latestId = recentEvents.Count > 0 ? recentEvents[^1].Id : 0,
            count = events.Length,
            events = events.Select(e => new
            {
                id = e.Id,
                timestampUtc = e.TimestampUtc,
                type = e.Type,
                data = e.Data
            }).ToArray()
        });
    }

    public MccMcpResult GetLoadedBots() =>
        MccMcpResult.Ok(new
        {
            count = 2,
            bots = new object[]
            {
                new { name = "McpServer", fullTypeName = "MinecraftClient.ChatBots.McpServer", isScript = false },
                new { name = "HarnessScript", fullTypeName = "MinecraftClient.ChatBots.Script", isScript = true }
            }
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

    public MccMcpResult DisconnectClient()
    {
        disconnecting = true;
        AddRecentEvent("disconnect", new { reason = "requested", message = "Disconnect requested by test client." });
        return MccMcpResult.Ok(new { disconnecting = true });
    }

    public MccMcpResult RunInternalCommand(string command) =>
        MccMcpResult.Ok(new { command, status = "Done", output = "deterministic" });

    public MccMcpResult UseItemOnHand() =>
        MccMcpResult.Ok(new { success = true, action = "use_item_on_hand" });

    public MccMcpResult ChangeHotbarSlot(int slot)
    {
        currentSlot = slot;
        return MccMcpResult.Ok(new { success = true, slot });
    }

    public MccMcpResult SelectHotbarItem(string itemType, bool preferLowestSlot)
    {
        currentSlot = string.Equals(itemType, "DiamondSword", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        return MccMcpResult.Ok(new
        {
            success = true,
            itemType,
            inventorySlot = currentSlot - 1,
            selectedSlot = currentSlot,
            count = string.Equals(itemType, "DiamondSword", StringComparison.OrdinalIgnoreCase) ? 1 : 32,
            preferLowestSlot
        });
    }

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

    public MccMcpResult AttackEntity(int entityId) =>
        MccMcpResult.Ok(new { success = true, entityId, interaction = "Attack" });

    public MccMcpResult FindNearestEntity(string? typeFilter, string? nameFilter, double radius, bool includePlayers)
    {
        bool wantsArmorStand = string.IsNullOrWhiteSpace(typeFilter)
            || string.Equals(typeFilter, "ArmorStand", StringComparison.OrdinalIgnoreCase)
            || string.Equals(typeFilter, "Armor Stand", StringComparison.OrdinalIgnoreCase);

        if (wantsArmorStand && radius >= 4.0)
        {
            return MccMcpResult.Ok(new
            {
                id = 7,
                type = "ArmorStand",
                typeLabel = "Armor Stand",
                uuid = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                name = "Armor Stand",
                customName = (string?)null,
                x = C(2.5),
                y = C(80.0),
                z = C(0.5),
                distance = 2.0,
                health = 20.0f,
                pose = "Standing",
                latency = 0
            });
        }

        if (includePlayers && radius >= 3.0)
        {
            return MccMcpResult.Ok(new
            {
                id = 2,
                type = "Player",
                typeLabel = "Player",
                uuid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                name = string.IsNullOrWhiteSpace(nameFilter) ? "PlayerOne" : nameFilter,
                customName = (string?)null,
                x = C(3.5),
                y = C(80.0),
                z = C(0.5),
                distance = 3.0,
                health = 20.0f,
                pose = "Standing",
                latency = 12
            });
        }

        return MccMcpResult.Fail("invalid_state", data: new { typeFilter, nameFilter, radius, includePlayers });
    }

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

    public Task<MccMcpResult> MoveToAsync(double x, double y, double z, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs) =>
        Task.FromResult(MoveTo(x, y, z, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeoutMs));

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

    public Task<MccMcpResult> MoveToPlayerAsync(string playerName, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs) =>
        Task.FromResult(MoveToPlayer(playerName, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeoutMs));

    public MccMcpResult LookAt(double x, double y, double z) =>
        MccMcpResult.Ok(new { looked = true, x = C(x), y = C(y), z = C(z) });

    public MccMcpResult LookDirection(string direction)
    {
        switch (direction.Trim().ToLowerInvariant())
        {
            case "up":
                yaw = 0.0f;
                pitch = -90.0f;
                break;
            case "down":
                yaw = 0.0f;
                pitch = 90.0f;
                break;
            case "north":
                yaw = 180.0f;
                pitch = 0.0f;
                break;
            case "south":
                yaw = 0.0f;
                pitch = 0.0f;
                break;
            case "east":
                yaw = -90.0f;
                pitch = 0.0f;
                break;
            case "west":
                yaw = 90.0f;
                pitch = 0.0f;
                break;
        }

        return MccMcpResult.Ok(new { success = true, direction, yaw, pitch });
    }

    public MccMcpResult LookAngles(float yaw, float pitch)
    {
        this.yaw = yaw;
        this.pitch = pitch;
        return MccMcpResult.Ok(new { success = true, yaw, pitch });
    }

    public MccMcpResult PlayAnimation(string hand) =>
        MccMcpResult.Ok(new { success = true, hand });

    public MccMcpResult ToggleSneak(bool enabled)
    {
        sneaking = enabled;
        return MccMcpResult.Ok(new { success = true, enabled = sneaking });
    }

    public MccMcpResult ToggleSprint(bool enabled)
    {
        sprinting = enabled;
        return MccMcpResult.Ok(new { success = true, enabled = sprinting });
    }

    public MccMcpResult ListInventories() =>
        MccMcpResult.Ok(new
        {
            count = 2,
            inventories = new object[]
            {
                new { id = 0, type = "PlayerInventory", title = "Player Inventory", slotCount = 46, nonEmptySlots = 1, active = false },
                new { id = 1, type = "Generic_9x3", title = "Chest", slotCount = 63, nonEmptySlots = 2, active = true }
            }
        });

    public MccMcpResult GetInventorySnapshot(int inventoryId) =>
        MccMcpResult.Ok(new
        {
            id = inventoryId,
            type = inventoryId == 0 ? "PlayerInventory" : "Generic_9x3",
            title = inventoryId == 0 ? "Player Inventory" : "Chest",
            slotCount = inventoryId == 0 ? 46 : 63,
            slots = new[]
            {
                new { slot = 0, type = "Stone", count = 64 }
            }
        });

    public MccMcpResult SearchInventories(string query, int maxCount, bool exactMatch, bool includeContainers)
    {
        List<object> matches = [];

        if (query.Contains("stone", StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(new
            {
                inventoryId = 0,
                inventoryType = "PlayerInventory",
                inventoryTitle = "Player Inventory",
                slot = 0,
                itemType = "Stone",
                typeLabel = "Stone",
                count = 32,
                isPlayerInventory = true,
                hotbarSlot = 1
            });
        }

        if (query.Contains("diamond", StringComparison.OrdinalIgnoreCase) || query.Contains("sword", StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(new
            {
                inventoryId = 0,
                inventoryType = "PlayerInventory",
                inventoryTitle = "Player Inventory",
                slot = 1,
                itemType = "DiamondSword",
                typeLabel = "Diamond Sword",
                count = 1,
                isPlayerInventory = true,
                hotbarSlot = 2
            });
        }

        if (includeContainers)
        {
            matches.Add(new
            {
                inventoryId = 1,
                inventoryType = "Generic_9x3",
                inventoryTitle = "Chest",
                slot = 0,
                itemType = "Stone",
                typeLabel = "Stone",
                count = 16,
                isPlayerInventory = false,
                hotbarSlot = (int?)null
            });
        }

        object[] result = matches.Take(Math.Max(1, maxCount)).ToArray();
        return MccMcpResult.Ok(new
        {
            query,
            exactMatch,
            includeContainers,
            count = result.Length,
            matches = result
        });
    }

    public MccMcpResult OpenContainerAt(int x, int y, int z, int timeoutMs, bool closeCurrent)
    {
        AddRecentEvent("inventory_open", new { inventoryId = 1, type = "Generic_9x3", title = "Chest", x, y, z });
        return MccMcpResult.Ok(new
        {
            success = true,
            openAccepted = true,
            opened = true,
            timeoutMs = timeoutMs <= 0 ? 5000 : timeoutMs,
            x,
            y,
            z,
            block = new { material = "Chest", typeLabel = "Chest", blockId = 0, blockMeta = 0 },
            inventory = new { id = 1, type = "Generic_9x3", title = "Chest", slotCount = 63, nonEmptySlots = 2 }
        });
    }

    public Task<MccMcpResult> OpenContainerAtAsync(int x, int y, int z, int timeoutMs, bool closeCurrent) =>
        Task.FromResult(OpenContainerAt(x, y, z, timeoutMs, closeCurrent));

    public MccMcpResult CloseContainer(int inventoryId, int timeoutMs)
    {
        int resolvedInventoryId = inventoryId <= 0 ? 1 : inventoryId;
        AddRecentEvent("inventory_close", new { inventoryId = resolvedInventoryId });
        return MccMcpResult.Ok(new
        {
            success = true,
            closed = true,
            inventoryId = resolvedInventoryId,
            timeoutMs = timeoutMs <= 0 ? 5000 : timeoutMs
        });
    }

    public Task<MccMcpResult> CloseContainerAsync(int inventoryId, int timeoutMs) =>
        Task.FromResult(CloseContainer(inventoryId, timeoutMs));

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

    public MccMcpResult DepositContainerItem(string itemType, int count, int inventoryId, bool preferLargestStack) =>
        MccMcpResult.Ok(new
        {
            success = true,
            direction = "deposit",
            itemType,
            requestedCount = count,
            movedCount = count,
            beforePlayerCount = 64,
            afterPlayerCount = Math.Max(0, 64 - count),
            beforeContainerCount = 0,
            afterContainerCount = count,
            inventoryId = inventoryId <= 0 ? 1 : inventoryId,
            containerType = "Generic_9x3",
            touchedSourceSlots = new[] { 36 },
            touchedTargetSlots = new[] { 0 }
        });

    public MccMcpResult WithdrawContainerItem(string itemType, int count, int inventoryId, bool preferLargestStack) =>
        MccMcpResult.Ok(new
        {
            success = true,
            direction = "withdraw",
            itemType,
            requestedCount = count,
            movedCount = count,
            beforePlayerCount = 0,
            afterPlayerCount = count,
            beforeContainerCount = 64,
            afterContainerCount = Math.Max(0, 64 - count),
            inventoryId = inventoryId <= 0 ? 1 : inventoryId,
            containerType = "Generic_9x3",
            touchedSourceSlots = new[] { 0 },
            touchedTargetSlots = new[] { 36 }
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

    public Task<MccMcpResult> PickupItemsAsync(string itemType, double radius, int maxItems, bool allowUnsafe, int timeoutMs) =>
        Task.FromResult(PickupItems(itemType, radius, maxItems, allowUnsafe, timeoutMs));

    public MccMcpResult Respawn()
    {
        health = 20.0f;
        AddRecentEvent("respawn", new { location = new { x = playerX, y = playerY, z = playerZ } });
        return MccMcpResult.Ok(new { success = true, respawned = true });
    }

    public MccMcpResult GetWorldBlockAt(int x, int y, int z) =>
        MccMcpResult.Ok(new { x, y, z, material = "Air", blockId = 0, blockMeta = 0 });

    private void AddRecentEvent(string type, object? data)
    {
        recentEvents.Add(new RecentEvent(nextEventId++, DateTimeOffset.UtcNow, type, data));
        if (recentEvents.Count > 100)
            recentEvents.RemoveAt(0);
    }

    private sealed record RecentEvent(long Id, DateTimeOffset TimestampUtc, string Type, object? Data);
}
