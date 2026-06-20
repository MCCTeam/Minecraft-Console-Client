namespace DebugTools.MccMcpWebPlayground.Harness;

public sealed class MccWebHarnessOptions
{
    public const string SectionName = "MccWebHarness";

    public string? Model { get; set; }
    public string OpenRouterBaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string McpEndpoint { get; set; } = "http://127.0.0.1:33333/mcp";
    public int MaxTurns { get; set; } = 48;
    public int MaxToolCalls { get; set; } = 120;
    public int MaxWallClockSeconds { get; set; } = 240;
    public int SoftFinishRemainingTurns { get; set; } = 3;
    public int SoftFinishRemainingToolCalls { get; set; } = 8;
    public int SoftFinishRemainingSeconds { get; set; } = 30;
    public bool RequireProviderParameters { get; set; } = true;
    public bool AllowFallbacks { get; set; }
    public bool DisableParallelToolCalls { get; set; } = true;
    public bool ExposeInventoryWindowAction { get; set; }
    public bool ExposeInternalCommandTool { get; set; }

    public string? ResolveModel()
    {
        return FirstNonEmpty(Environment.GetEnvironmentVariable("OPENROUTER_MODEL"), Model);
    }

    public string ResolveOpenRouterBaseUrl()
    {
        return FirstNonEmpty(Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL"), OpenRouterBaseUrl)
            ?? "https://openrouter.ai/api/v1";
    }

    public string ResolveMcpEndpoint()
    {
        return FirstNonEmpty(Environment.GetEnvironmentVariable("MCC_MCP_ENDPOINT"), McpEndpoint)
            ?? "http://127.0.0.1:33333/mcp";
    }

    public string? ResolveMcpAuthToken()
    {
        return Environment.GetEnvironmentVariable("MCC_MCP_AUTH_TOKEN");
    }

    public string? ResolveApiKey()
    {
        return Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
    }

    public bool HasApiKeyConfigured()
    {
        return !string.IsNullOrWhiteSpace(ResolveApiKey());
    }

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        return candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate))?.Trim();
    }
}
