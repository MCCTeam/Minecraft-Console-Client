using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MinecraftClient.Mcp;

public sealed class MccMcpPromptSet
{
    private readonly MccMcpGuidanceProvider guidanceProvider;

    public MccMcpPromptSet(MccMcpGuidanceProvider guidanceProvider)
    {
        this.guidanceProvider = guidanceProvider;
    }

    [McpServerPrompt(Name = "mcc_operator_guide"), Description("Get the canonical MCC operator guidance prompt for external agents using this MCP server.")]
    public string OperatorGuide()
    {
        return guidanceProvider.GetSystemPrompt();
    }
}
