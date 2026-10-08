using Mcc.Cli.Localization;
using DMCBK.Core;
using Umpk.Protocol.Java;
using Umpk.Text;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Renders the server-status/MOTD panel for the classic host: a dashed separator, the MOTD, host:port, the reported version/protocol, the resolved (actually-spoken) version/protocol when known, a color-coded ping, the player count, and up to 10 sample names.
/// Shape ported from legacy's <c>ServerStatusDisplay.ShowClassic</c> (MinecraftClient/Protocol/ServerStatusDisplay.cs:20-101).
/// <para>
/// Ping coloring is ported from legacy's TUI <c>ServerStatusPanelBuilder.AddPing</c> (green under 100ms, yellow under 300ms, red otherwise): legacy's own classic renderer left ping a flat green with no threshold at all, which reads like an oversight rather than a deliberate choice, so both new hosts use the one real threshold legacy has instead of reproducing the flat color.
/// </para>
/// </summary>
internal static class ServerStatusPanel
{
    private const int MaxSamplePlayers = 10;

    /// <summary>
    /// Builds the panel as already-ANSI-colored lines, one per <c>writeLine</c> call (embedding a raw newline in a single write can desync the rich console's line tracking, so callers must write these one at a time rather than joining them).
    /// </summary>
    public static IReadOnlyList<string> BuildLines(
        ServerStatusReceivedEventArgs status, AnsiComponentRenderer motdRenderer, ConsoleColorDepth depth)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(motdRenderer);

        ServerStatus info = status.Status;
        string separator = Colored(new string('-', 50), TextColor.DarkGray, depth);

        var lines = new List<string> { separator };

        if (info.Description is { } motd)
            lines.Add(motdRenderer.Render(motd));

        lines.Add(
            $"{Colored(Strings.ServerInfoLabelServer, TextColor.White, depth)} " +
            $"{Colored(status.Host, TextColor.Aqua, depth)}{Colored(":", TextColor.Gray, depth)}" +
            Colored(status.Port.ToString(), TextColor.Aqua, depth));

        lines.Add(
            $"{Colored(Strings.ServerInfoLabelVersion, TextColor.White, depth)} " +
            $"{Colored(info.VersionName ?? Strings.ServerInfoVersionUnknown, TextColor.Aqua, depth)} " +
            $"{Colored("(", TextColor.Gray, depth)}{ProtocolText(info.Protocol, TextColor.Yellow, depth)}{Colored(")", TextColor.Gray, depth)}");

        if (status.ResolvedVersion is { } resolved)
        {
            // Greens are vanilla §a bright green, matching legacy's ServerStatusDisplay (which colors these rows " §a") and the old TUI's McColors.Green (85,255,85).
            lines.Add(
                $"{Colored(Strings.ServerInfoLabelConnectingAs, TextColor.White, depth)} " +
                $"{Colored(resolved.Version.Name, TextColor.Green, depth)} {Colored("(", TextColor.Gray, depth)}" +
                $"{ProtocolText(resolved.Version.Protocol, TextColor.Green, depth)}{Colored(")", TextColor.Gray, depth)}");
        }

        long pingMs = (long)info.Latency.TotalMilliseconds;
        TextColor pingColor = pingMs < 100 ? TextColor.Green : pingMs < 300 ? TextColor.Yellow : TextColor.Red;
        lines.Add(
            $"{Colored(Strings.ServerInfoLabelPing, TextColor.White, depth)} " +
            Colored(Strings.ServerInfoPingMs(pingMs), pingColor, depth));

        if (info.OnlinePlayers is { } online && info.MaxPlayers is { } max)
        {
            lines.Add(
                $"{Colored(Strings.ServerInfoLabelPlayers, TextColor.White, depth)} " +
                $"{Colored(online.ToString(System.Globalization.CultureInfo.InvariantCulture), TextColor.Green, depth)}" +
                $"{Colored("/", TextColor.Gray, depth)}" +
                Colored(max.ToString(System.Globalization.CultureInfo.InvariantCulture), TextColor.Red, depth));
        }

        if (info.Sample.Count > 0)
        {
            lines.Add(Colored(Strings.ServerInfoLabelOnlinePlayers, TextColor.White, depth));

            int shown = Math.Min(info.Sample.Count, MaxSamplePlayers);
            for (int i = 0; i < shown; i++)
                lines.Add("  " + Colored(info.Sample[i].Name, TextColor.Green, depth));

            if (info.Sample.Count > shown)
                lines.Add("  " + Colored(Strings.ServerInfoSampleMore(info.Sample.Count - shown), TextColor.Gray, depth));
        }

        lines.Add(separator);
        return lines;
    }

    private static string ProtocolText(int? protocol, TextColor color, ConsoleColorDepth depth)
        => Colored(
            protocol is { } value ? Strings.ServerInfoLabelProtocol(value) : Strings.ServerInfoProtocolUnknown,
            color,
            depth);

    private static string Colored(string text, TextColor color, ConsoleColorDepth depth)
    {
        if (depth == ConsoleColorDepth.Disable)
            return text;

        string sgr = AnsiColorMapping.Sgr(color, depth);
        return sgr.Length == 0 ? text : $"[{sgr}m{text}[0m";
    }
}
