using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

string endpoint = Environment.GetEnvironmentVariable("MCC_MCP_ENDPOINT") ?? "http://127.0.0.1:33333/mcp";
string model = "minimax/minimax-m2.7";
bool useStdio = string.Equals(Environment.GetEnvironmentVariable("MCC_MCP_USE_STDIO"), "1", StringComparison.Ordinal);
string? openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
string openRouterBaseUrl = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL") ?? "https://openrouter.ai/api/v1";
string? mcpAuthToken = Environment.GetEnvironmentVariable("MCC_MCP_AUTH_TOKEN");

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

var executed = new List<object>();

CallToolResult sessionStatus = await CallAndStore("mcc_session_status");
await CallAndStore("mcc_players_list");
await CallAndStore("mcc_send_chat", new Dictionary<string, object?> { ["text"] = "/say mcp_full_sweep" });
await CallAndStore("mcc_run_internal_command", new Dictionary<string, object?> { ["command"] = "debug state" });

(double lookX, double lookY, double lookZ) = GetLookTarget(sessionStatus);
await CallAndStore("mcc_look_at", new Dictionary<string, object?> { ["x"] = lookX, ["y"] = lookY, ["z"] = lookZ });
await CallAndStore("mcc_move_to", new Dictionary<string, object?> { ["x"] = lookX, ["y"] = lookY, ["z"] = lookZ, ["timeoutMs"] = 2000 });

CallToolResult inventorySnapshot = await CallAndStore("mcc_inventory_snapshot", new Dictionary<string, object?> { ["inventoryId"] = 0 });
int actionSlot = GetInventoryActionSlot(inventorySnapshot);
await CallAndStore("mcc_inventory_window_action", new Dictionary<string, object?> { ["inventoryId"] = 0, ["slotId"] = actionSlot, ["actionType"] = "LeftClick" });

await CallAndStore("mcc_entities_query", new Dictionary<string, object?> { ["maxCount"] = 20 });
CallToolResult entitiesList = await CallAndStore("mcc_entities_list", new Dictionary<string, object?> { ["maxCount"] = 20 });
int? firstEntityId = GetFirstEntityId(entitiesList);
if (firstEntityId.HasValue)
{
    await CallAndStore("mcc_entity_info", new Dictionary<string, object?>
    {
        ["entityId"] = firstEntityId.Value,
        ["includeMetadata"] = false,
        ["includeEquipment"] = true,
        ["includeEffects"] = true
    });
}
await CallAndStore("mcc_blocks_find", new Dictionary<string, object?> { ["query"] = "Grass", ["radius"] = 6, ["maxCount"] = 50 });
await CallAndStore("mcc_player_nearby", new Dictionary<string, object?> { ["radius"] = 48.0, ["includeSelf"] = false });
await CallAndStore("mcc_world_block_at", new Dictionary<string, object?> { ["x"] = 0, ["y"] = 80, ["z"] = 0 });

string evidenceJson = JsonSerializer.Serialize(executed, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(evidenceJson);

if (!useStdio && !string.IsNullOrWhiteSpace(openRouterApiKey))
{
    using HttpClient http = new();
    http.BaseAddress = new Uri(openRouterBaseUrl.TrimEnd('/') + "/");
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openRouterApiKey);
    http.DefaultRequestHeaders.Add("HTTP-Referer", "https://localhost/mcc-mcp-sample");
    http.DefaultRequestHeaders.Add("X-Title", "MCC MCP Sample Client");

    var payload = new
    {
        model,
        messages = new object[]
        {
            new { role = "system", content = "Summarize the MCP tool execution output briefly." },
            new { role = "user", content = evidenceJson }
        }
    };

    HttpResponseMessage response = await http.PostAsync(
        "chat/completions",
        new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    string body = await response.Content.ReadAsStringAsync();
    Console.WriteLine(body);
}

async Task<CallToolResult> CallAndStore(string toolName, IReadOnlyDictionary<string, object?>? args = null)
{
    CallToolResult result = await client.CallToolAsync(toolName, args);
    executed.Add(new
    {
        tool = toolName,
        arguments = args,
        isError = result.IsError,
        result = result
    });
    return result;
}

static (double x, double y, double z) GetLookTarget(CallToolResult sessionStatus)
{
    JsonElement? data = TryReadData(sessionStatus);
    if (data is JsonElement jsonData &&
        jsonData.TryGetProperty("location", out JsonElement location) &&
        TryReadDouble(location, "x", out double x) &&
        TryReadDouble(location, "y", out double y) &&
        TryReadDouble(location, "z", out double z))
    {
        return (x, y, z);
    }

    return (0.5, 80.0, 0.5);
}

static int GetInventoryActionSlot(CallToolResult inventorySnapshot)
{
    JsonElement? data = TryReadData(inventorySnapshot);
    if (data is JsonElement jsonData &&
        jsonData.TryGetProperty("slots", out JsonElement slots) &&
        slots.ValueKind == JsonValueKind.Array)
    {
        foreach (JsonElement slot in slots.EnumerateArray())
        {
            if (TryReadInt(slot, "slot", out int slotId))
                return slotId;
        }
    }

    return 0;
}

static int? GetFirstEntityId(CallToolResult entitiesList)
{
    JsonElement? data = TryReadData(entitiesList);
    if (data is not JsonElement jsonData)
        return null;

    if (!jsonData.TryGetProperty("entities", out JsonElement entities)
        || entities.ValueKind != JsonValueKind.Array
        || entities.GetArrayLength() == 0)
    {
        return null;
    }

    JsonElement first = entities[0];
    if (TryReadInt(first, "id", out int entityId))
        return entityId;

    return null;
}

static JsonElement? TryReadData(CallToolResult result)
{
    if (result.Content is null)
        return null;

    foreach (ContentBlock content in result.Content)
    {
        if (content is TextContentBlock text &&
            !string.IsNullOrWhiteSpace(text.Text))
        {
            using JsonDocument doc = JsonDocument.Parse(text.Text);
            if (doc.RootElement.TryGetProperty("data", out JsonElement data))
                return data.Clone();
        }
    }

    return null;
}

static bool TryReadDouble(JsonElement element, string property, out double value)
{
    value = 0;
    return element.TryGetProperty(property, out JsonElement prop) && prop.TryGetDouble(out value);
}

static bool TryReadInt(JsonElement element, string property, out int value)
{
    value = 0;
    return element.TryGetProperty(property, out JsonElement prop) && prop.TryGetInt32(out value);
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
