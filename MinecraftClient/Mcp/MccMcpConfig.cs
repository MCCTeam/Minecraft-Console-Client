using Tomlet.Attributes;

namespace MinecraftClient.Mcp;

public sealed class MccMcpConfig
{
    public bool Enabled { get; set; }
    public MccMcpTransportConfig Transport { get; set; } = new();
    public MccMcpCapabilityToggles Capabilities { get; set; } = new();
}

public sealed class MccMcpTransportConfig
{
    [TomlInlineComment("$ChatBot.McpServer.Transport.BindHost$")]
    public string BindHost { get; set; } = "127.0.0.1";

    [TomlInlineComment("$ChatBot.McpServer.Transport.Port$")]
    public int Port { get; set; } = 33333;

    [TomlInlineComment("$ChatBot.McpServer.Transport.Route$")]
    public string Route { get; set; } = "/mcp";

    [TomlInlineComment("$ChatBot.McpServer.Transport.RequireAuthToken$")]
    public bool RequireAuthToken { get; set; }

    [TomlInlineComment("$ChatBot.McpServer.Transport.AuthTokenEnvVar$")]
    public string AuthTokenEnvVar { get; set; } = "MCC_MCP_AUTH_TOKEN";
}

public sealed class MccMcpCapabilityToggles
{
    [TomlInlineComment("$ChatBot.McpServer.Capabilities.SessionStatus$")]
    public bool SessionStatus { get; set; } = true;

    [TomlInlineComment("$ChatBot.McpServer.Capabilities.ChatAndCommands$")]
    public bool ChatAndCommands { get; set; } = true;

    [TomlInlineComment("$ChatBot.McpServer.Capabilities.Movement$")]
    public bool Movement { get; set; } = true;

    [TomlInlineComment("$ChatBot.McpServer.Capabilities.Inventory$")]
    public bool Inventory { get; set; } = true;

    [TomlInlineComment("$ChatBot.McpServer.Capabilities.EntityWorld$")]
    public bool EntityWorld { get; set; } = true;
}
