using Mcc.Cli.Localization;
using Mcc.Cli.Startup;
using Mcc.Cli.Tui.Hosting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Umpk.Data.Java;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>
/// Builds the startup banner shown at the top of the TUI chat log: a bordered icon panel with the hand-drawn creeper face (ported verbatim from legacy's <c>MccBannerPanelBuilder</c>, MinecraftClient/Tui/MccBannerPanelBuilder.cs:102-164), the MCC title and client version (the same assembly informational version the classic banner reports, see <see cref="ClientVersion"/>), the supported MC version range, and the repo hint.
/// Gated as a whole on console.toml's Display_Icon_Banner by the caller (<see cref="MainTuiView"/>), matching the classic host's ClassicBanner gate (<c>CliHost.PrintClassicBanner</c>).
/// </summary>
internal static class BannerBuilder
{
    public static IReadOnlyList<Control> BuildLines() => [Build()];

    /// <summary>
    /// Minecraft's own chat palette, which is what the legacy banner draws with (MinecraftClient/Tui/MccBannerPanelBuilder.cs:174-181).
    /// Avalonia's named brushes are NOT interchangeable with it and were the reason the banner read as low-contrast grey-on-grey: Brushes.Gray is #808080, dimmer than MC's grey (#AAAAAA), and Brushes.DarkGray is #A9A9A9, which is LIGHTER than Brushes.Gray, so "subtitle grey" and "hint dark grey" came out inverted.
    /// </summary>
    private static class Pal
    {
        public static readonly IBrush Gray = new SolidColorBrush(Color.FromRgb(170, 170, 170));
        public static readonly IBrush DarkGray = new SolidColorBrush(Color.FromRgb(85, 85, 85));
        public static readonly IBrush Aqua = new SolidColorBrush(Color.FromRgb(85, 255, 255));
        public static readonly IBrush Green = new SolidColorBrush(Color.FromRgb(85, 255, 85));
        public static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(255, 170, 0));
    }

    private static Border Build()
    {
        var contentPanel = new DockPanel { Background = Brushes.Black };

        Control icon = BuildIcon();
        icon.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(icon, Dock.Left);
        contentPanel.Children.Add(icon);

        var infoPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(1, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        infoPanel.Children.Add(TitleLine());
        infoPanel.Children.Add(new TextBlock { Text = Strings.TuiBannerSubtitle, Foreground = Pal.Gray });
        infoPanel.Children.Add(VersionRangeLine());
        infoPanel.Children.Add(new TextBlock { Text = Strings.TuiBannerHint, Foreground = Pal.DarkGray });

        contentPanel.Children.Add(infoPanel);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(240, 20, 20, 20)),
            Padding = new Thickness(1, 0),
            Child = contentPanel,
            Margin = new Thickness(0),
        };
    }

    private static TextBlock TitleLine()
    {
        var row = new TextBlock();
        row.Inlines!.Add(new Run(Strings.TuiBannerTitle) { Foreground = Pal.Gold, FontWeight = FontWeight.Bold });
        row.Inlines.Add(new Run($" v{ClientVersion.Current}") { Foreground = Pal.Aqua });
        return row;
    }

    private static TextBlock VersionRangeLine()
    {
        var versions = JavaVersions.All;
        string lowVersion = versions[0].Version.Name;
        string highVersion = versions[^1].Version.Name;

        var row = new TextBlock();
        row.Inlines!.Add(new Run(Strings.TuiBannerVersionsLabel + " ") { Foreground = Pal.Gray });
        row.Inlines.Add(new Run(lowVersion) { Foreground = Pal.Green });
        row.Inlines.Add(new Run(" - ") { Foreground = Pal.Gray });
        row.Inlines.Add(new Run(highVersion) { Foreground = Pal.Green });
        return row;
    }

    #region Icon

    // Ported verbatim from legacy's MccBannerPanelBuilder.cs:95-164 (the hand-drawn creeper glyph grid).

    private static readonly Color B1 = Color.FromRgb(200, 200, 200); // bezel bright
    private static readonly Color B2 = Color.FromRgb(160, 160, 160); // bezel mid
    private static readonly Color B3 = Color.FromRgb(120, 120, 120); // bezel dark
    private static readonly Color S = Color.FromRgb(20, 20, 20);     // screen bg
    private static readonly Color C = Color.FromRgb(55, 200, 55);    // creeper green

    // @formatter:off
    private static readonly Color[,] Pixels =
    {
        { B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B2 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   C,   C,   S,   S,   C,   C,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   C,   C,   S,   S,   C,   C,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   C,   C,   S,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   C,   C,   C,   C,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   C,   C,   C,   C,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   C,   S,   S,   C,   S,   S,   B3 },
        { B1,  S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   S,   B3 },
        { B2,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3,  B3 },
    };
    // @formatter:on

    private static Control BuildIcon()
    {
        int cols = Pixels.GetLength(1);
        int textRows = Pixels.GetLength(0) / 2;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0),
        };

        for (int row = 0; row < textRows; row++)
        {
            var line = new TextBlock { Padding = new Thickness(0), Margin = new Thickness(0) };

            for (int col = 0; col < cols; col++)
            {
                var topColor = Pixels[row * 2, col];
                var bottomColor = Pixels[(row * 2) + 1, col];

                if (row == 1 && col == 1)
                {
                    line.Inlines!.Add(new Run(" ＞_")
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                        Background = new SolidColorBrush(S),
                    });
                    col += 4;
                    topColor = Pixels[row * 2, col];
                    bottomColor = Pixels[(row * 2) + 1, col];
                }

                line.Inlines!.Add(new Run("▀")
                {
                    Foreground = new SolidColorBrush(topColor),
                    Background = new SolidColorBrush(bottomColor),
                });
            }

            panel.Children.Add(line);
        }

        return panel;
    }

    #endregion
}
