using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace MinecraftClient.Mcp;

public sealed class MccMcpGuidanceProvider
{
    private const string EmbeddedSkillResourceSuffix = "MccMcpOperatorSkill.md";
    private const string BestPracticesHeading = "## Best Practices";
    private const string ExampleScenariosHeading = "## Example Scenarios";

    private readonly MccMcpConfig config;
    private readonly Lazy<GuidanceDocument> guidanceDocument;

    public MccMcpGuidanceProvider(MccMcpConfig config)
    {
        this.config = config;
        guidanceDocument = new Lazy<GuidanceDocument>(LoadGuidanceDocument);
    }

    public string SkillName => "mcc-mcp-operator";

    public string GetSystemPrompt()
    {
        GuidanceDocument document = guidanceDocument.Value;
        MccMcpAgentCapabilityStatus capabilityStatus = BuildCapabilityStatus();
        StringBuilder builder = new();
        builder.AppendLine("You are an external agent controlling Minecraft Console Client (MCC) through its built-in MCP server.");
        builder.AppendLine("Use the following operator guide as your system prompt. Treat the capability snapshot as authoritative and do not invent unsupported actions.");
        builder.AppendLine();
        builder.AppendLine(document.BodyMarkdown);
        builder.AppendLine();
        builder.AppendLine("Current capability snapshot");
        builder.AppendLine($"- sessionStatus: {FormatCapability(capabilityStatus.SessionStatus)}");
        builder.AppendLine($"- chatAndCommands: {FormatCapability(capabilityStatus.ChatAndCommands)}");
        builder.AppendLine($"- movement: {FormatCapability(capabilityStatus.Movement)}");
        builder.AppendLine($"- inventory: {FormatCapability(capabilityStatus.Inventory)}");
        builder.AppendLine($"- entityWorld: {FormatCapability(capabilityStatus.EntityWorld)}");
        return builder.ToString().Trim();
    }

    public MccMcpAgentGuidancePayload GetToolPayload()
    {
        GuidanceDocument document = guidanceDocument.Value;
        return new MccMcpAgentGuidancePayload
        {
            SkillName = SkillName,
            SkillMarkdown = document.SkillMarkdown,
            SystemPrompt = GetSystemPrompt(),
            BestPractices = document.BestPractices,
            ExampleScenarios = document.ExampleScenarios,
            CapabilityStatus = BuildCapabilityStatus()
        };
    }

    private GuidanceDocument LoadGuidanceDocument()
    {
        Assembly assembly = typeof(MccMcpGuidanceProvider).Assembly;
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(EmbeddedSkillResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded MCP skill resource '{EmbeddedSkillResourceSuffix}' was not found.");

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded MCP skill resource '{resourceName}' could not be opened.");

        using StreamReader reader = new(stream, Encoding.UTF8);
        string skillMarkdown = reader.ReadToEnd();
        string bodyMarkdown = StripFrontmatter(skillMarkdown);
        string bestPracticesSection = ExtractSection(bodyMarkdown, BestPracticesHeading);
        string exampleScenariosSection = ExtractSection(bodyMarkdown, ExampleScenariosHeading);

        return new GuidanceDocument(
            skillMarkdown.Replace("\r\n", "\n").Trim(),
            bodyMarkdown,
            ExtractBulletList(bestPracticesSection),
            ExtractExampleScenarios(exampleScenariosSection));
    }

    private MccMcpAgentCapabilityStatus BuildCapabilityStatus()
    {
        return new MccMcpAgentCapabilityStatus
        {
            SessionStatus = config.Capabilities.SessionStatus,
            ChatAndCommands = config.Capabilities.ChatAndCommands,
            Movement = config.Capabilities.Movement,
            Inventory = config.Capabilities.Inventory,
            EntityWorld = config.Capabilities.EntityWorld
        };
    }

    private static string StripFrontmatter(string markdown)
    {
        string normalized = markdown.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
            return normalized.Trim();

        int endOfFrontmatter = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (endOfFrontmatter < 0)
            return normalized.Trim();

        return normalized[(endOfFrontmatter + 5)..].Trim();
    }

    private static string ExtractSection(string markdownBody, string heading)
    {
        int headingIndex = markdownBody.IndexOf(heading, StringComparison.Ordinal);
        if (headingIndex < 0)
            return string.Empty;

        int sectionStart = headingIndex + heading.Length;
        int nextHeading = markdownBody.IndexOf("\n## ", sectionStart, StringComparison.Ordinal);
        string section = nextHeading >= 0
            ? markdownBody[sectionStart..nextHeading]
            : markdownBody[sectionStart..];

        return section.Trim();
    }

    private static string[] ExtractBulletList(string section)
    {
        return section
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line[2..].Trim())
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private static MccMcpAgentScenario[] ExtractExampleScenarios(string section)
    {
        if (string.IsNullOrWhiteSpace(section))
            return [];

        List<MccMcpAgentScenario> scenarios = [];
        string? currentTitle = null;
        List<string> currentBodyLines = [];

        foreach (string rawLine in section.Split('\n'))
        {
            string line = rawLine.TrimEnd();
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                AddScenario(scenarios, currentTitle, currentBodyLines);
                currentTitle = line[4..].Trim();
                currentBodyLines = [];
                continue;
            }

            if (currentTitle is not null)
                currentBodyLines.Add(line);
        }

        AddScenario(scenarios, currentTitle, currentBodyLines);
        return scenarios.ToArray();
    }

    private static void AddScenario(List<MccMcpAgentScenario> scenarios, string? title, List<string> bodyLines)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        string guidance = string.Join('\n', bodyLines)
            .Trim();

        scenarios.Add(new MccMcpAgentScenario
        {
            Title = title,
            Guidance = guidance
        });
    }

    private static string FormatCapability(bool enabled)
    {
        return enabled ? "enabled" : "disabled";
    }

    private sealed record GuidanceDocument(
        string SkillMarkdown,
        string BodyMarkdown,
        string[] BestPractices,
        MccMcpAgentScenario[] ExampleScenarios);
}

public sealed class MccMcpAgentGuidancePayload
{
    [JsonPropertyName("skillName")]
    public string SkillName { get; init; } = string.Empty;

    [JsonPropertyName("skillMarkdown")]
    public string SkillMarkdown { get; init; } = string.Empty;

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; init; } = string.Empty;

    [JsonPropertyName("bestPractices")]
    public string[] BestPractices { get; init; } = [];

    [JsonPropertyName("exampleScenarios")]
    public MccMcpAgentScenario[] ExampleScenarios { get; init; } = [];

    [JsonPropertyName("capabilityStatus")]
    public MccMcpAgentCapabilityStatus CapabilityStatus { get; init; } = new();
}

public sealed class MccMcpAgentScenario
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("guidance")]
    public string Guidance { get; init; } = string.Empty;
}

public sealed class MccMcpAgentCapabilityStatus
{
    [JsonPropertyName("sessionStatus")]
    public bool SessionStatus { get; init; }

    [JsonPropertyName("chatAndCommands")]
    public bool ChatAndCommands { get; init; }

    [JsonPropertyName("movement")]
    public bool Movement { get; init; }

    [JsonPropertyName("inventory")]
    public bool Inventory { get; init; }

    [JsonPropertyName("entityWorld")]
    public bool EntityWorld { get; init; }
}
