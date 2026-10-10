using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DMCBK.Core;
using Umpk.Protocol.Java;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>
/// Builds the server-status/MOTD panel for the TUI: a bordered block with the MOTD (rendered through the component renderer so its colors survive), host:port, the reported version/protocol, the resolved (actually-spoken) version/protocol when known, a color-coded ping, the player count, and up to 10 sample names.
/// Ported in shape from legacy's <c>ServerStatusPanelBuilder</c> (MinecraftClient/Tui/ServerStatusPanelBuilder.cs).
/// <para>
/// Deliberately WITHOUT the favicon pixel art legacy's <c>IconGridBuilder</c> renders: that decoder needs a PNG library (legacy uses ImageMagick), and neither Mcc.Cli nor DMCBK.Core references one.
/// Adding a new dependency purely for this optional decoration was judged not worth it, so the panel renders text-only; this is a deliberate, recorded gap rather than a silently dropped feature.
/// </para>
/// </summary>
internal static class ServerStatusPanel
{
    private const int MaxSamplePlayers = 10;

    /// <summary>Builds the panel control, ready to hand to <see cref="MainTuiView.AppendControlLine"/>.</summary>
    public static Border Build(ServerStatusReceivedEventArgs status, ComponentInlineRenderer? renderer)
    {
        ArgumentNullException.ThrowIfNull(status);

        ServerStatus info = status.Status;
        var infoPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Margin = new Thickness(1, 0),
        };

        AddMotd(infoPanel, info, renderer);
        AddAddress(infoPanel, status);
        AddVersion(infoPanel, info);
        AddConnectingAs(infoPanel, status);
        AddPing(infoPanel, info);
        AddPlayers(infoPanel, info);
        AddSamplePlayers(infoPanel, info);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(240, 20, 20, 20)),
            Padding = new Thickness(1, 0),
            Child = infoPanel,
            Margin = new Thickness(0),
        };
    }

    private static void AddMotd(StackPanel panel, ServerStatus info, ComponentInlineRenderer? renderer)
    {
        if (info.Description is not { } motd)
            return;

        panel.Children.Add(renderer is not null
            ? renderer.Render(motd, TextWrapping.NoWrap)
            : new TextBlock { Text = motd.ToPlainText(), Foreground = Brushes.White, TextWrapping = TextWrapping.NoWrap });
    }

    private static void AddAddress(StackPanel panel, ServerStatusReceivedEventArgs status)
    {
        var row = new TextBlock();
        row.Inlines!.Add(Label(Strings.ServerInfoLabelServer));
        row.Inlines.Add(Value(status.Host, McColors.Aqua));
        row.Inlines.Add(new Run($":{status.Port}") { Foreground = McColors.Gray });
        panel.Children.Add(row);
    }

    private static void AddVersion(StackPanel panel, ServerStatus info)
    {
        var row = new TextBlock();
        row.Inlines!.Add(Label(Strings.ServerInfoLabelVersion));
        row.Inlines.Add(Value(info.VersionName ?? Strings.ServerInfoVersionUnknown, McColors.Aqua));
        row.Inlines.Add(new Run(" (") { Foreground = McColors.Gray });
        row.Inlines.Add(new Run(info.Protocol is { } p ? Strings.ServerInfoLabelProtocol(p) : Strings.ServerInfoProtocolUnknown)
        { Foreground = McColors.Gray });
        row.Inlines.Add(new Run(")") { Foreground = McColors.Gray });
        panel.Children.Add(row);
    }

    private static void AddConnectingAs(StackPanel panel, ServerStatusReceivedEventArgs status)
    {
        if (status.ResolvedVersion is not { } resolved)
            return;

        var row = new TextBlock();
        row.Inlines!.Add(Label(Strings.ServerInfoLabelConnectingAs));
        row.Inlines.Add(Value(resolved.Version.Name, McColors.Green));
        row.Inlines.Add(new Run(" (") { Foreground = McColors.Gray });
        row.Inlines.Add(new Run(Strings.ServerInfoLabelProtocol(resolved.Version.Protocol)) { Foreground = McColors.Gray });
        row.Inlines.Add(new Run(")") { Foreground = McColors.Gray });
        panel.Children.Add(row);
    }

    private static void AddPing(StackPanel panel, ServerStatus info)
    {
        long pingMs = (long)info.Latency.TotalMilliseconds;
        IBrush pingColor = pingMs < 100 ? McColors.Green : pingMs < 300 ? McColors.Yellow : McColors.Red;

        var row = new TextBlock();
        row.Inlines!.Add(Label(Strings.ServerInfoLabelPing));
        row.Inlines.Add(new Run(Strings.ServerInfoPingMs(pingMs)) { Foreground = pingColor });
        panel.Children.Add(row);
    }

    private static void AddPlayers(StackPanel panel, ServerStatus info)
    {
        if (info.OnlinePlayers is not { } online || info.MaxPlayers is not { } max)
            return;

        var row = new TextBlock();
        row.Inlines!.Add(Label(Strings.ServerInfoLabelPlayers));
        row.Inlines.Add(Value(online.ToString(System.Globalization.CultureInfo.InvariantCulture), McColors.Green));
        row.Inlines.Add(new Run("/") { Foreground = McColors.Gray });
        row.Inlines.Add(Value(max.ToString(System.Globalization.CultureInfo.InvariantCulture), McColors.Red));
        panel.Children.Add(row);
    }

    private static void AddSamplePlayers(StackPanel panel, ServerStatus info)
    {
        if (info.Sample.Count == 0)
            return;

        panel.Children.Add(new TextBlock { Text = Strings.ServerInfoLabelOnlinePlayers, Foreground = McColors.Gray });

        int shown = Math.Min(info.Sample.Count, MaxSamplePlayers);
        for (int i = 0; i < shown; i++)
            panel.Children.Add(new TextBlock { Text = $"  {info.Sample[i].Name}", Foreground = McColors.Green });

        if (info.Sample.Count > shown)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"  {Strings.ServerInfoSampleMore(info.Sample.Count - shown)}",
                Foreground = McColors.Gray,
            });
        }
    }

    private static Run Label(string text) => new(text + " ") { Foreground = McColors.Gray };

    private static Run Value(string text, IBrush color) => new(text) { Foreground = color };

    private static class McColors
    {
        public static readonly IBrush Gray = new SolidColorBrush(Color.FromRgb(170, 170, 170));
        public static readonly IBrush Aqua = new SolidColorBrush(Color.FromRgb(85, 255, 255));
        // Vanilla §a bright green, matching legacy's ServerStatusDisplay and the old TUI palette.
        public static readonly IBrush Green = new SolidColorBrush(Color.FromRgb(85, 255, 85));
        public static readonly IBrush Red = new SolidColorBrush(Color.FromRgb(255, 85, 85));
        public static readonly IBrush Yellow = new SolidColorBrush(Color.FromRgb(255, 255, 85));
    }
}
