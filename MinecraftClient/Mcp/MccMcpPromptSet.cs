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

    [McpServerPrompt(Name = "mcc_operator_prompt"), Description("Get the canonical MCC MCP Operator Prompt for external agents using this MCP server.")]
    public string OperatorPrompt()
    {
        return guidanceProvider.GetSystemPrompt();
    }
}
