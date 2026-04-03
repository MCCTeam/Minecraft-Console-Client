using System.Collections.Frozen;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;

namespace DebugTools.MccMcpWebPlayground.Harness;

public enum MccToolRisk
{
    ReadOnly,
    Stateful,
    Sensitive,
    EscapeHatch
}

public sealed record MccToolProfile(
    string Name,
    MccToolRisk Risk,
    bool VisibleByDefault,
    bool RequiresExplicitUserIntent);

public sealed record MccToolCatalogEntry(McpClientTool Tool, MccToolProfile Profile);

public sealed class MccToolCatalog
{
    public required Dictionary<string, MccToolCatalogEntry> ToolsByName { get; init; }
    public required IReadOnlyList<object> ModelVisibleTools { get; init; }
}

public static class MccToolPolicy
{
    private static readonly FrozenDictionary<string, MccToolProfile> Profiles =
        new Dictionary<string, MccToolProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["mcc_agent_guidance"] = new("mcc_agent_guidance", MccToolRisk.ReadOnly, false, false),
            ["mcc_inventory_window_action"] = new("mcc_inventory_window_action", MccToolRisk.EscapeHatch, false, false),
            ["mcc_run_internal_command"] = new("mcc_run_internal_command", MccToolRisk.EscapeHatch, false, false),
            ["mcc_quit_client"] = new("mcc_quit_client", MccToolRisk.Sensitive, true, true)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static MccToolProfile GetProfile(string toolName)
    {
        return Profiles.TryGetValue(toolName, out MccToolProfile? profile)
            ? profile
            : new MccToolProfile(toolName, MccToolRisk.Stateful, true, false);
    }

    public static MccToolCatalog BuildCatalog(IList<McpClientTool> tools, MccWebHarnessOptions options, object submitFinalTool)
    {
        Dictionary<string, MccToolCatalogEntry> toolsByName = tools.ToDictionary(
            tool => tool.Name,
            tool => new MccToolCatalogEntry(tool, GetProfile(tool.Name)),
            StringComparer.OrdinalIgnoreCase);

        List<object> visibleTools = [];
        foreach (MccToolCatalogEntry entry in toolsByName.Values.OrderBy(entry => entry.Tool.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!IsVisible(entry.Profile, options))
                continue;

            visibleTools.Add(ToOpenRouterTool(entry.Tool, entry.Profile));
        }

        visibleTools.Add(submitFinalTool);

        return new MccToolCatalog
        {
            ToolsByName = toolsByName,
            ModelVisibleTools = visibleTools
        };
    }

    public static bool RequiresExplicitUserIntent(string toolName)
    {
        return GetProfile(toolName).RequiresExplicitUserIntent;
    }

    public static bool HasExplicitUserIntent(string userRequest, string toolName)
    {
        if (!RequiresExplicitUserIntent(toolName))
            return true;

        string request = userRequest.Trim().ToLowerInvariant();
        return toolName.Equals("mcc_quit_client", StringComparison.OrdinalIgnoreCase)
            && (request.Contains("quit mcc", StringComparison.Ordinal)
                || request.Contains("close mcc", StringComparison.Ordinal)
                || request.Contains("stop mcc", StringComparison.Ordinal)
                || request.Contains("exit mcc", StringComparison.Ordinal)
                || request.Contains("quit the client", StringComparison.Ordinal)
                || request.Contains("stop the client", StringComparison.Ordinal));
    }

    private static bool IsVisible(MccToolProfile profile, MccWebHarnessOptions options)
    {
        if (!profile.VisibleByDefault)
        {
            if (profile.Name.Equals("mcc_inventory_window_action", StringComparison.OrdinalIgnoreCase))
                return options.ExposeInventoryWindowAction;

            if (profile.Name.Equals("mcc_run_internal_command", StringComparison.OrdinalIgnoreCase))
                return options.ExposeInternalCommandTool;

            return false;
        }

        return true;
    }

    private static object ToOpenRouterTool(McpClientTool tool, MccToolProfile profile)
    {
        JsonNode parameters = JsonNode.Parse(tool.JsonSchema.GetRawText()) ?? new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject()
        };

        string description = tool.Description ?? string.Empty;
        if (profile.Risk == MccToolRisk.Sensitive)
            description = $"{description} Requires explicit user intent.";
        else if (profile.Risk == MccToolRisk.EscapeHatch)
            description = $"{description} Advanced escape hatch; prefer higher-level tools first.";

        return new Dictionary<string, object?>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object?>
            {
                ["name"] = tool.Name,
                ["description"] = description,
                ["parameters"] = parameters
            }
        };
    }
}
