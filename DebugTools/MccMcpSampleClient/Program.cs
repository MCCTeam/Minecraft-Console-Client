using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

string endpoint = Environment.GetEnvironmentVariable("MCC_MCP_ENDPOINT") ?? "http://127.0.0.1:33333/mcp";
bool useStdio = string.Equals(Environment.GetEnvironmentVariable("MCC_MCP_USE_STDIO"), "1", StringComparison.Ordinal);
string? mcpAuthToken = Environment.GetEnvironmentVariable("MCC_MCP_AUTH_TOKEN");
string repoRoot = FindRepoRoot();
string rconScript = Path.Combine(repoRoot, "tools", "mc-rcon.sh");
string rconPort = Environment.GetEnvironmentVariable("MCC_RCON_PORT") ?? "25575";
string rconPassword = Environment.GetEnvironmentVariable("MCC_RCON_PASSWORD") ?? "test123";
bool skipSetup = string.Equals(Environment.GetEnvironmentVariable("MCC_MCP_SKIP_SETUP"), "1", StringComparison.Ordinal);
bool runLocalSetup = !useStdio && !skipSetup && IsLocalEndpoint(endpoint) && File.Exists(rconScript);
var executed = new List<object>();
var checks = new List<string>();

try
{
    await using McpClient client = useStdio
        ? await McpClient.CreateAsync(new StdioClientTransport(CreateStdioOptions()))
        : await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            TransportMode = HttpTransportMode.AutoDetect,
            AdditionalHeaders = string.IsNullOrWhiteSpace(mcpAuthToken)
                ? null
                : new Dictionary<string, string> { ["Authorization"] = $"Bearer {mcpAuthToken}" }
        }));

    ToolEnvelope initialWorldState = await CallSuccessAsync(client, executed, "mcc_world_state");
    JsonElement initialWorldData = RequireData(initialWorldState);
    string botName = ReadString(initialWorldData, "username") ?? "CursorBot";
    Coordinate initialLocation = ReadCoordinate(initialWorldData, "location");

    long setupBaseline = 0;
    if (runLocalSetup)
    {
        ToolEnvelope baselineEvents = await CallSuccessAsync(client, executed, "mcc_recent_events", new Dictionary<string, object?>
        {
            ["afterId"] = 0L,
            ["maxCount"] = 1
        });
        setupBaseline = ReadInt64(RequireData(baselineEvents), "latestId");

        await PrepareWorldAsync(rconScript, rconPort, rconPassword, botName, initialLocation);
        await Task.Delay(1500);
    }

    ToolEnvelope worldState = await WaitForPredicateAsync(
        client,
        executed,
        "mcc_world_state",
        null,
        envelope =>
        {
            if (!envelope.Success || envelope.Data is not JsonElement data)
                return false;

            return data.TryGetProperty("loadedChunkCount", out JsonElement loaded)
                && loaded.TryGetInt32(out int loadedChunkCount)
                && loadedChunkCount >= 0
                && HasNonNullProperty(data, "worldAge")
                && HasNonNullProperty(data, "timeOfDay");
        },
        "mcc_world_state never reported chunk/time state.");

    JsonElement worldData = RequireData(worldState);
    Coordinate worldLocation = ReadCoordinate(worldData, "location");
    string dimension = RequireString(worldData, "dimension");
    int loadedChunkCount = ReadInt32(worldData, "loadedChunkCount");
    int pendingChunkCount = ReadInt32(worldData, "pendingChunkCount");
    int totalChunkCount = ReadInt32(worldData, "totalChunkCount");
    double loadRatio = ReadDouble(worldData, "loadRatio");
    _ = RequireString(worldData, "host");
    _ = ReadInt32(worldData, "port");
    _ = RequireString(worldData, "username");
    _ = ReadInt32(worldData, "protocol");
    _ = ReadDouble(worldData, "tps");
    Ensure(!string.IsNullOrWhiteSpace(dimension), "mcc_world_state returned an empty dimension.");
    Ensure(loadedChunkCount + pendingChunkCount == totalChunkCount, "mcc_world_state chunk counters are inconsistent.");
    Ensure(loadRatio is >= 0 and <= 1, "mcc_world_state loadRatio is out of range.");
    Ensure(HasNonNullProperty(worldData, "worldAge"), "mcc_world_state.worldAge is null.");
    Ensure(HasNonNullProperty(worldData, "timeOfDay"), "mcc_world_state.timeOfDay is null.");
    if (runLocalSetup || useStdio)
    {
        Ensure(HasNonNullProperty(worldData, "rainLevel"), "mcc_world_state.rainLevel is null after setup.");
        Ensure(HasNonNullProperty(worldData, "thunderLevel"), "mcc_world_state.thunderLevel is null after setup.");
    }
    checks.Add("mcc_world_state");

    ToolEnvelope chunkStatus = await CallSuccessAsync(client, executed, "mcc_chunk_status");
    JsonElement chunkData = RequireData(chunkStatus);
    JsonElement chunk = RequireProperty(chunkData, "chunk");
    _ = ReadInt32(chunk, "x");
    _ = ReadInt32(chunk, "z");
    Ensure(ReadBoolean(chunkData, "loaded"), "mcc_chunk_status reported the current chunk as unloaded.");
    Ensure(ReadInt32(chunkData, "loadedChunkCount") + ReadInt32(chunkData, "pendingChunkCount") == ReadInt32(chunkData, "totalChunkCount"),
        "mcc_chunk_status chunk counters are inconsistent.");
    checks.Add("mcc_chunk_status");

    await CallSuccessAsync(client, executed, "mcc_look_direction", new Dictionary<string, object?> { ["direction"] = "Down" });
    ToolEnvelope raycast = await CallSuccessAsync(client, executed, "mcc_raycast_block", new Dictionary<string, object?>
    {
        ["maxDistance"] = 8.0,
        ["includeNeighbors"] = true
    });
    JsonElement raycastData = RequireData(raycast);
    Ensure(ReadBoolean(raycastData, "hit"), "mcc_raycast_block did not report a hit after looking down.");
    JsonElement raycastBlock = RequireProperty(raycastData, "block");
    Ensure(!string.Equals(RequireString(raycastBlock, "material"), "Air", StringComparison.OrdinalIgnoreCase),
        "mcc_raycast_block hit Air instead of a solid block.");
    Ensure(RequireProperty(raycastData, "neighbors").ValueKind == JsonValueKind.Object,
        "mcc_raycast_block did not include neighbors when requested.");
    checks.Add("mcc_raycast_block");

    ToolEnvelope pathPreview = await CallSuccessAsync(client, executed, "mcc_path_preview", new Dictionary<string, object?>
    {
        ["x"] = Math.Floor(worldLocation.X) + 2,
        ["y"] = worldLocation.Y,
        ["z"] = Math.Floor(worldLocation.Z),
        ["allowUnsafe"] = false,
        ["timeoutMs"] = 2000,
        ["maxWaypoints"] = 32
    });
    JsonElement pathData = RequireData(pathPreview);
    Ensure(ReadBoolean(pathData, "pathFound"), "mcc_path_preview did not find a path to a nearby target.");
    Ensure(RequireProperty(pathData, "waypoints").GetArrayLength() > 0, "mcc_path_preview returned no waypoints.");
    checks.Add("mcc_path_preview");

    ToolEnvelope stoneSearch = await WaitForPredicateAsync(
        client,
        executed,
        "mcc_inventory_search",
        new Dictionary<string, object?>
        {
            ["query"] = "Stone",
            ["maxCount"] = 20,
            ["exactMatch"] = true,
            ["includeContainers"] = false
        },
        envelope => envelope.Success && envelope.Data is JsonElement data && ReadInt32(data, "count") > 0,
        "mcc_inventory_search never found Stone in the player inventory.");
    Ensure(ContainsItemType(RequireData(stoneSearch), "Stone"), "mcc_inventory_search results did not include Stone.");

    ToolEnvelope swordSearch = await WaitForPredicateAsync(
        client,
        executed,
        "mcc_inventory_search",
        new Dictionary<string, object?>
        {
            ["query"] = "DiamondSword",
            ["maxCount"] = 20,
            ["exactMatch"] = true,
            ["includeContainers"] = false
        },
        envelope => envelope.Success && envelope.Data is JsonElement data && ReadInt32(data, "count") > 0,
        "mcc_inventory_search never found DiamondSword in the player inventory.");
    Ensure(ContainsItemType(RequireData(swordSearch), "DiamondSword"), "mcc_inventory_search results did not include DiamondSword.");
    checks.Add("mcc_inventory_search");

    ToolEnvelope selectItem = await CallSuccessAsync(client, executed, "mcc_select_item", new Dictionary<string, object?>
    {
        ["itemType"] = "DiamondSword",
        ["preferLowestSlot"] = true
    });
    JsonElement selectData = RequireData(selectItem);
    int selectedSlot = ReadInt32(selectData, "selectedSlot");

    ToolEnvelope playerStats = await CallSuccessAsync(client, executed, "mcc_player_stats");
    JsonElement playerStatsData = RequireData(playerStats);
    Ensure(ReadInt32(playerStatsData, "currentSlot") == selectedSlot, "mcc_select_item did not update mcc_player_stats.currentSlot.");
    _ = ReadInt32(playerStatsData, "playerEntityId");
    _ = ReadInt32(playerStatsData, "level");
    _ = ReadInt32(playerStatsData, "totalExperience");
    _ = ReadCoordinate(playerStatsData, "location");
    checks.Add("mcc_select_item");
    checks.Add("mcc_player_stats");

    ToolEnvelope playersDetailed = await CallSuccessAsync(client, executed, "mcc_players_detailed", new Dictionary<string, object?>
    {
        ["includeSelf"] = true,
        ["includeCoordinates"] = true
    });
    JsonElement playersData = RequireData(playersDetailed);
    JsonElement selfPlayer = FindPlayer(RequireProperty(playersData, "players"), botName);
    _ = RequireString(selfPlayer, "uuid");
    _ = ReadInt32(selfPlayer, "ping");
    _ = ReadInt32(selfPlayer, "entityId");
    _ = ReadDouble(selfPlayer, "x");
    _ = ReadDouble(selfPlayer, "y");
    _ = ReadDouble(selfPlayer, "z");
    checks.Add("mcc_players_detailed");

    ToolEnvelope statusEffects = await CallSuccessAsync(client, executed, "mcc_status_effects");
    Ensure(RequireProperty(RequireData(statusEffects), "effects").ValueKind == JsonValueKind.Array,
        "mcc_status_effects.effects is not an array.");
    checks.Add("mcc_status_effects");

    ToolEnvelope animation = await CallSuccessAsync(client, executed, "mcc_animation", new Dictionary<string, object?>
    {
        ["hand"] = "MainHand"
    });
    Ensure(ReadBoolean(RequireData(animation), "success"), "mcc_animation did not report success.");

    ToolEnvelope sneakOn = await CallSuccessAsync(client, executed, "mcc_toggle_sneak", new Dictionary<string, object?> { ["enabled"] = true });
    Ensure(ReadBoolean(RequireData(sneakOn), "enabled"), "mcc_toggle_sneak(true) did not report enabled=true.");

    ToolEnvelope sprintOn = await CallSuccessAsync(client, executed, "mcc_toggle_sprint", new Dictionary<string, object?> { ["enabled"] = true });
    Ensure(ReadBoolean(RequireData(sprintOn), "enabled"), "mcc_toggle_sprint(true) did not report enabled=true.");

    await CallSuccessAsync(client, executed, "mcc_look_angles", new Dictionary<string, object?>
    {
        ["yaw"] = 45.0f,
        ["pitch"] = -15.0f
    });
    ToolEnvelope updatedStats = await CallSuccessAsync(client, executed, "mcc_player_stats");
    JsonElement updatedStatsData = RequireData(updatedStats);
    Ensure(Math.Abs(ReadDouble(updatedStatsData, "yaw") - 45.0) < 0.01, "mcc_look_angles did not update yaw.");
    Ensure(Math.Abs(ReadDouble(updatedStatsData, "pitch") - (-15.0)) < 0.01, "mcc_look_angles did not update pitch.");
    checks.Add("mcc_animation");
    checks.Add("mcc_toggle_sneak");
    checks.Add("mcc_toggle_sprint");
    checks.Add("mcc_look_direction");
    checks.Add("mcc_look_angles");

    ToolEnvelope nearestEntity = await WaitForPredicateAsync(
        client,
        executed,
        "mcc_entity_nearest",
        new Dictionary<string, object?>
        {
            ["typeFilter"] = "ArmorStand",
            ["radius"] = 16.0,
            ["includePlayers"] = false
        },
        envelope => envelope.Success,
        "mcc_entity_nearest never found a nearby ArmorStand.");
    JsonElement nearestData = RequireData(nearestEntity);
    int entityId = ReadInt32(nearestData, "id");
    Ensure(string.Equals(RequireString(nearestData, "type"), "ArmorStand", StringComparison.OrdinalIgnoreCase),
        "mcc_entity_nearest did not return an ArmorStand.");

    ToolEnvelope attackEntity = await CallSuccessAsync(client, executed, "mcc_entity_attack", new Dictionary<string, object?>
    {
        ["entityId"] = entityId
    });
    Ensure(ReadBoolean(RequireData(attackEntity), "success"), "mcc_entity_attack did not report success.");
    checks.Add("mcc_entity_nearest");
    checks.Add("mcc_entity_attack");

    long recentSetupAfterId = runLocalSetup ? setupBaseline : 0;
    if (runLocalSetup || useStdio)
    {
        ToolEnvelope setupEvents = await WaitForRecentEventTypesAsync(
            client,
            executed,
            recentSetupAfterId,
            "weather_rain",
            "title",
            "actionbar");
        JsonElement setupEventsData = RequireData(setupEvents);
        Ensure(GetEventTypes(setupEventsData).Contains("weather_rain", StringComparer.OrdinalIgnoreCase), "mcc_recent_events did not include weather_rain.");
        Ensure(GetEventTypes(setupEventsData).Contains("title", StringComparer.OrdinalIgnoreCase), "mcc_recent_events did not include title.");
        Ensure(GetEventTypes(setupEventsData).Contains("actionbar", StringComparer.OrdinalIgnoreCase), "mcc_recent_events did not include actionbar.");
    }

    ToolEnvelope actionbarEvents = await CallSuccessAsync(client, executed, "mcc_recent_events", new Dictionary<string, object?>
    {
        ["afterId"] = 0L,
        ["maxCount"] = 20,
        ["typeFilter"] = "actionbar"
    });
    JsonElement actionbarData = RequireData(actionbarEvents);
    Ensure(ReadInt32(actionbarData, "count") > 0, "mcc_recent_events typeFilter=actionbar returned no events.");
    Ensure(AllEventsMatchType(actionbarData, "actionbar"), "mcc_recent_events typeFilter returned mixed event types.");

    long inventoryBaseline = ReadInt64(actionbarData, "latestId");
    int chestX = (int)Math.Floor(worldLocation.X) + 2;
    int chestY = (int)Math.Floor(worldLocation.Y);
    int chestZ = (int)Math.Floor(worldLocation.Z);
    ToolEnvelope openContainer = await WaitForPredicateAsync(
        client,
        executed,
        "mcc_container_open_at",
        new Dictionary<string, object?>
        {
            ["x"] = chestX,
            ["y"] = chestY,
            ["z"] = chestZ,
            ["timeoutMs"] = 3000,
            ["closeCurrent"] = true
        },
        envelope => envelope.Success,
        "mcc_open_container_at never opened the nearby chest.");
    JsonElement inventoryInfo = RequireProperty(RequireData(openContainer), "inventory");
    int openedInventoryId = ReadInt32(inventoryInfo, "id");
    ToolEnvelope closeContainer = await CallSuccessAsync(client, executed, "mcc_container_close", new Dictionary<string, object?>
    {
        ["inventoryId"] = openedInventoryId,
        ["timeoutMs"] = 3000
    });
    Ensure(ReadBoolean(RequireData(closeContainer), "closed"), "mcc_close_container did not close the chest.");

    ToolEnvelope inventoryEvents = await WaitForRecentEventTypesAsync(
        client,
        executed,
        inventoryBaseline,
        "inventory_open",
        "inventory_close");
    JsonElement inventoryEventsData = RequireData(inventoryEvents);
    Ensure(GetEventTypes(inventoryEventsData).Contains("inventory_open", StringComparer.OrdinalIgnoreCase), "mcc_recent_events did not include inventory_open.");
    Ensure(GetEventTypes(inventoryEventsData).Contains("inventory_close", StringComparer.OrdinalIgnoreCase), "mcc_recent_events did not include inventory_close.");
    checks.Add("mcc_recent_events");

    if (runLocalSetup)
    {
        long deathBaseline = ReadInt64(inventoryEventsData, "latestId");
        await RunRconCommandAsync(rconScript, rconPort, rconPassword, $"kill {botName}");
        ToolEnvelope deathEvents = await WaitForRecentEventTypesAsync(client, executed, deathBaseline, "death");
        Ensure(GetEventTypes(RequireData(deathEvents)).Contains("death", StringComparer.OrdinalIgnoreCase),
            "mcc_recent_events never reported death after the RCON kill.");

        long respawnBaseline = ReadInt64(RequireData(deathEvents), "latestId");
        ToolEnvelope respawn = await CallSuccessAsync(client, executed, "mcc_respawn");
        Ensure(ReadBoolean(RequireData(respawn), "success"), "mcc_respawn did not report success.");
        ToolEnvelope respawnEvents = await WaitForRecentEventTypesAsync(client, executed, respawnBaseline, "respawn");
        Ensure(GetEventTypes(RequireData(respawnEvents)).Contains("respawn", StringComparer.OrdinalIgnoreCase),
            "mcc_recent_events never reported respawn after mcc_respawn.");
        checks.Add("mcc_respawn");
    }
    else
    {
        long respawnBaseline = ReadInt64(RequireData(inventoryEvents), "latestId");
        ToolEnvelope respawn = await CallSuccessAsync(client, executed, "mcc_respawn");
        Ensure(ReadBoolean(RequireData(respawn), "success"), "mcc_respawn did not report success.");
        ToolEnvelope respawnEvents = await WaitForRecentEventTypesAsync(client, executed, respawnBaseline, "respawn");
        Ensure(GetEventTypes(RequireData(respawnEvents)).Contains("respawn", StringComparer.OrdinalIgnoreCase),
            "mcc_recent_events never reported respawn after mcc_respawn.");
        checks.Add("mcc_respawn");
    }

    ToolEnvelope loadedBots = await CallSuccessAsync(client, executed, "mcc_loaded_bots");
    JsonElement bots = RequireProperty(RequireData(loadedBots), "bots");
    Ensure(ContainsBot(bots, "McpServer"), "mcc_loaded_bots did not include McpServer.");
    checks.Add("mcc_loaded_bots");

    ToolEnvelope disconnect = await CallSuccessAsync(client, executed, "mcc_disconnect");
    Ensure(ReadBoolean(RequireData(disconnect), "disconnecting"), "mcc_disconnect did not report disconnecting=true.");
    checks.Add("mcc_disconnect");

    if (!useStdio)
    {
        await AssertDisconnectStopsEndpointAsync(client, executed);
    }

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        success = true,
        endpoint,
        useStdio,
        runLocalSetup,
        checks,
        executed
    }, new JsonSerializerOptions { WriteIndented = true }));

    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        success = false,
        endpoint,
        useStdio,
        runLocalSetup,
        error = ex.Message,
        checks,
        executed
    }, new JsonSerializerOptions { WriteIndented = true }));
    Environment.ExitCode = 1;
}

static async Task PrepareWorldAsync(string rconScript, string rconPort, string rconPassword, string botName, Coordinate location)
{
    string[] commands =
    [
        $"op {botName}",
        $"gamemode creative {botName}",
        $"tp {botName} 0 80 0",
        $"item replace entity {botName} hotbar.0 with minecraft:stone 32",
        $"item replace entity {botName} hotbar.1 with minecraft:diamond_sword 1",
        $"execute as {botName} at @s run setblock ~2 ~ ~ minecraft:chest",
        $"execute as {botName} at @s run summon minecraft:armor_stand ~2 ~ ~1",
        "weather clear",
        "weather rain",
        $"title {botName} title {{\"text\":\"mcp_title\"}}",
        $"title {botName} actionbar {{\"text\":\"mcp_actionbar\"}}"
    ];

    foreach (string command in commands)
    {
        await RunRconCommandAsync(rconScript, rconPort, rconPassword, command);
    }
}

static async Task RunRconCommandAsync(string rconScript, string rconPort, string rconPassword, string command)
{
    ProcessStartInfo startInfo = new("bash")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    startInfo.ArgumentList.Add(rconScript);
    startInfo.ArgumentList.Add(command);
    startInfo.ArgumentList.Add(rconPort);
    startInfo.ArgumentList.Add(rconPassword);

    using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start mc-rcon.sh.");
    string stdout = await process.StandardOutput.ReadToEndAsync();
    string stderr = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"RCON command failed ({command}): {(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr).Trim()}");
    }
}

static async Task<ToolEnvelope> CallSuccessAsync(
    McpClient client,
    List<object> executed,
    string toolName,
    IReadOnlyDictionary<string, object?>? args = null)
{
    ToolEnvelope envelope = await CallToolAsync(client, executed, toolName, args);
    if (!envelope.Success)
    {
        throw new InvalidOperationException(
            $"{toolName} failed with errorCode={envelope.ErrorCode ?? "<null>"} message={envelope.Message ?? "<null>"}.");
    }

    return envelope;
}

static async Task<ToolEnvelope> WaitForPredicateAsync(
    McpClient client,
    List<object> executed,
    string toolName,
    IReadOnlyDictionary<string, object?>? args,
    Func<ToolEnvelope, bool> predicate,
    string failureMessage,
    int maxAttempts = 12,
    int delayMs = 400)
{
    ToolEnvelope? lastEnvelope = null;
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        ToolEnvelope envelope = await CallToolAsync(client, executed, toolName, args);
        lastEnvelope = envelope;
        if (predicate(envelope))
            return envelope;

        await Task.Delay(delayMs);
    }

    throw new InvalidOperationException(
        $"{failureMessage} Last result: success={lastEnvelope?.Success}, errorCode={lastEnvelope?.ErrorCode ?? "<null>"}.");
}

static async Task<ToolEnvelope> WaitForRecentEventTypesAsync(
    McpClient client,
    List<object> executed,
    long afterId,
    params string[] expectedTypes)
{
    HashSet<string> expected = expectedTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    ToolEnvelope? lastEnvelope = null;

    for (int attempt = 0; attempt < 12; attempt++)
    {
        ToolEnvelope envelope = await CallSuccessAsync(client, executed, "mcc_recent_events", new Dictionary<string, object?>
        {
            ["afterId"] = afterId,
            ["maxCount"] = 100
        });
        lastEnvelope = envelope;
        JsonElement data = RequireData(envelope);
        HashSet<string> actual = GetEventTypes(data);
        if (expected.All(actual.Contains))
            return envelope;

        await Task.Delay(400);
    }

    throw new InvalidOperationException(
        $"mcc_recent_events never reported: {string.Join(", ", expectedTypes)} after event id {afterId}. Last latestId={ReadInt64(RequireData(lastEnvelope!), "latestId")}.");
}

static async Task<ToolEnvelope> CallToolAsync(
    McpClient client,
    List<object> executed,
    string toolName,
    IReadOnlyDictionary<string, object?>? args = null)
{
    CallToolResult result = await client.CallToolAsync(toolName, args);
    string responseJson = ExtractResponseJson(result);
    JsonElement root = JsonDocument.Parse(responseJson).RootElement.Clone();
    JsonElement? data = root.TryGetProperty("data", out JsonElement dataElement) ? dataElement.Clone() : null;
    bool success = root.TryGetProperty("success", out JsonElement successElement)
        && successElement.ValueKind == JsonValueKind.True;
    string? errorCode = ReadString(root, "errorCode");
    string? message = ReadString(root, "message");

    executed.Add(new
    {
        tool = toolName,
        arguments = args,
        isError = result.IsError,
        success,
        errorCode,
        message,
        response = root
    });

    return new ToolEnvelope(toolName, result.IsError ?? false, success, errorCode, message, root, data);
}

static async Task AssertDisconnectStopsEndpointAsync(McpClient client, List<object> executed)
{
    for (int attempt = 0; attempt < 15; attempt++)
    {
        try
        {
            await CallToolAsync(client, executed, "mcc_world_state");
        }
        catch
        {
            return;
        }

        await Task.Delay(300);
    }

    throw new InvalidOperationException("The MCP endpoint still responded after mcc_disconnect.");
}

static string ExtractResponseJson(CallToolResult result)
{
    if (result.Content is null)
        throw new InvalidOperationException("Tool response did not contain any content blocks.");

    foreach (ContentBlock content in result.Content)
    {
        if (content is TextContentBlock text && !string.IsNullOrWhiteSpace(text.Text))
            return text.Text;
    }

    throw new InvalidOperationException("Tool response did not contain a text payload.");
}

static JsonElement RequireData(ToolEnvelope envelope)
{
    if (envelope.Data is JsonElement data)
        return data;

    throw new InvalidOperationException($"{envelope.ToolName} returned no data payload.");
}

static JsonElement RequireProperty(JsonElement element, string propertyName)
{
    if (element.TryGetProperty(propertyName, out JsonElement property))
        return property;

    throw new InvalidOperationException($"Missing required property '{propertyName}'.");
}

static string RequireString(JsonElement element, string propertyName)
{
    string? value = ReadString(element, propertyName);
    if (!string.IsNullOrWhiteSpace(value))
        return value;

    throw new InvalidOperationException($"Property '{propertyName}' is missing or empty.");
}

static string? ReadString(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
        ? property.GetString()
        : null;
}

static int ReadInt32(JsonElement element, string propertyName)
{
    JsonElement property = RequireProperty(element, propertyName);
    if (property.TryGetInt32(out int value))
        return value;

    throw new InvalidOperationException($"Property '{propertyName}' is not an Int32.");
}

static long ReadInt64(JsonElement element, string propertyName)
{
    JsonElement property = RequireProperty(element, propertyName);
    if (property.TryGetInt64(out long value))
        return value;

    throw new InvalidOperationException($"Property '{propertyName}' is not an Int64.");
}

static double ReadDouble(JsonElement element, string propertyName)
{
    JsonElement property = RequireProperty(element, propertyName);
    if (property.TryGetDouble(out double value))
        return value;

    throw new InvalidOperationException($"Property '{propertyName}' is not a Double.");
}

static bool ReadBoolean(JsonElement element, string propertyName)
{
    JsonElement property = RequireProperty(element, propertyName);
    return property.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw new InvalidOperationException($"Property '{propertyName}' is not a Boolean.")
    };
}

static bool HasNonNullProperty(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind != JsonValueKind.Null;
}

static Coordinate ReadCoordinate(JsonElement element, string propertyName)
{
    JsonElement coordinate = RequireProperty(element, propertyName);
    return new Coordinate(
        ReadDouble(coordinate, "x"),
        ReadDouble(coordinate, "y"),
        ReadDouble(coordinate, "z"));
}

static JsonElement FindPlayer(JsonElement players, string playerName)
{
    foreach (JsonElement player in players.EnumerateArray())
    {
        string? name = ReadString(player, "name");
        if (string.Equals(name, playerName, StringComparison.OrdinalIgnoreCase))
            return player;
    }

    throw new InvalidOperationException($"Could not find player '{playerName}' in mcc_players_detailed.");
}

static bool ContainsItemType(JsonElement searchData, string itemType)
{
    JsonElement matches = RequireProperty(searchData, "matches");
    foreach (JsonElement match in matches.EnumerateArray())
    {
        if (string.Equals(ReadString(match, "itemType"), itemType, StringComparison.OrdinalIgnoreCase))
            return true;
    }

    return false;
}

static bool ContainsBot(JsonElement bots, string botName)
{
    foreach (JsonElement bot in bots.EnumerateArray())
    {
        if (string.Equals(ReadString(bot, "name"), botName, StringComparison.OrdinalIgnoreCase))
            return true;
    }

    return false;
}

static HashSet<string> GetEventTypes(JsonElement recentEventsData)
{
    JsonElement events = RequireProperty(recentEventsData, "events");
    return events.EnumerateArray()
        .Select(entry => RequireString(entry, "type"))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

static bool AllEventsMatchType(JsonElement recentEventsData, string type)
{
    JsonElement events = RequireProperty(recentEventsData, "events");
    foreach (JsonElement entry in events.EnumerateArray())
    {
        if (!string.Equals(RequireString(entry, "type"), type, StringComparison.OrdinalIgnoreCase))
            return false;
    }

    return true;
}

static void Ensure(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static bool IsLocalEndpoint(string endpoint)
{
    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
        return false;

    return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase);
}

static string FindRepoRoot()
{
    string current = Directory.GetCurrentDirectory();
    DirectoryInfo? directory = new(current);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "MinecraftClient.sln")))
            return directory.FullName;

        directory = directory.Parent;
    }

    return current;
}

static StdioClientTransportOptions CreateStdioOptions()
{
    string? stdioBin = Environment.GetEnvironmentVariable("MCC_MCP_STDIO_BIN");
    if (!string.IsNullOrWhiteSpace(stdioBin))
    {
        return new StdioClientTransportOptions
        {
            Name = "MCC MCP Stdio Harness",
            Command = stdioBin,
            Arguments = [],
            ShutdownTimeout = TimeSpan.FromSeconds(5)
        };
    }

    return new StdioClientTransportOptions
    {
        Name = "MCC MCP Stdio Harness",
        Command = "dotnet",
        Arguments =
        [
            "run",
            "--project",
            "DebugTools/MccMcpStdioHarness",
            "-c",
            "Release",
            "--no-build"
        ],
        ShutdownTimeout = TimeSpan.FromSeconds(5)
    };
}

internal readonly record struct Coordinate(double X, double Y, double Z);

internal sealed record ToolEnvelope(
    string ToolName,
    bool IsError,
    bool Success,
    string? ErrorCode,
    string? Message,
    JsonElement Root,
    JsonElement? Data);
