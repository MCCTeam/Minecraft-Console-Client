using System.Text.Json;
using DebugTools.MccMcpWebPlayground.Contracts;
using DebugTools.MccMcpWebPlayground.Infrastructure.Mcp;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DebugTools.MccMcpWebPlayground.Harness;

public sealed class MccGuidanceSource
{
    public const string SourceToolName = "mcc_agent_guidance";
    public const string CanonicalPromptName = "mcc_operator_guide";

    public async Task<MccGuidanceBundle> LoadAsync(McpClient client, CancellationToken cancellationToken)
    {
        CallToolResult result = await client.CallToolAsync(SourceToolName, new Dictionary<string, object?>(), cancellationToken: cancellationToken);
        MccNormalizedToolResult normalized = MccMcpJson.Normalize(result);
        JsonElement data = normalized.Data ?? throw new InvalidOperationException("mcc_agent_guidance did not return data.");

        string[] bestPractices = ReadStringArray(data, "bestPractices");
        string[] exampleTitles = data.TryGetProperty("exampleScenarios", out JsonElement examples)
            && examples.ValueKind == JsonValueKind.Array
                ? examples.EnumerateArray()
                    .Select(example => example.TryGetProperty("title", out JsonElement title) ? title.GetString() : null)
                    .Where(title => !string.IsNullOrWhiteSpace(title))
                    .Cast<string>()
                    .ToArray()
                : [];

        MccCapabilityStatus capabilityStatus = data.TryGetProperty("capabilityStatus", out JsonElement capabilityJson)
            ? JsonSerializer.Deserialize<MccCapabilityStatus>(capabilityJson.GetRawText()) ?? new MccCapabilityStatus(false, false, false, false, false)
            : new MccCapabilityStatus(false, false, false, false, false);

        return new MccGuidanceBundle(
            SourceToolName,
            CanonicalPromptName,
            SkillName: ReadString(data, "skillName") ?? "mcc-mcp-operator",
            GuidanceVersion: ReadString(data, "guidanceVersion") ?? "unknown",
            SystemPrompt: ReadString(data, "systemPrompt") ?? throw new InvalidOperationException("mcc_agent_guidance did not return systemPrompt."),
            BestPractices: bestPractices,
            ExampleScenarioTitles: exampleTitles,
            CapabilityStatus: capabilityStatus);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string[] ReadStringArray(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.Array
            ? property.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Cast<string>()
                .ToArray()
            : [];
    }
}
