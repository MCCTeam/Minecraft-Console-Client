using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace MinecraftClient.Tui
{
    internal static class MccBannerPanelBuilder
    {
        internal static Border Build(string? buildInfo)
        {
            var contentPanel = new DockPanel { Background = Brushes.Black };

            var icon = BuildIcon();
            icon.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(icon, Dock.Left);
            contentPanel.Children.Add(icon);

            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(1, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            AddTitle(infoPanel);
            AddVersionRange(infoPanel);
            AddGithub(infoPanel);

            if (buildInfo is not null)
                AddBuildInfo(infoPanel, buildInfo);

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

        private static void AddTitle(StackPanel panel)
        {
            var row = new TextBlock();
            row.Inlines!.Add(new Run("Minecraft Console Client")
            { Foreground = Pal.Gold, FontWeight = FontWeight.Bold });
            row.Inlines.Add(new Run($" v{Program.Version}") { Foreground = Pal.Aqua });
            panel.Children.Add(row);
        }

        private static void AddVersionRange(StackPanel panel)
        {
            var row = new TextBlock();
            row.Inlines!.Add(Lbl(Translations.mcc_banner_label_mc_versions));
            row.Inlines.Add(Val(Program.MCLowestVersion, Pal.Green));
            row.Inlines.Add(new Run(" - ") { Foreground = Pal.Gray });
            row.Inlines.Add(Val(Program.MCHighestVersion, Pal.Green));
            panel.Children.Add(row);
        }

        private static void AddGithub(StackPanel panel)
        {
            var row = new TextBlock();
            row.Inlines!.Add(Val("Github.com/MCCTeam", Pal.Gray));
            panel.Children.Add(row);
        }

        private static void AddBuildInfo(StackPanel panel, string buildInfo)
        {
            panel.Children.Add(new TextBlock
            {
                Text = buildInfo,
                Foreground = Pal.DarkGray,
            });
        }

        #region Icon

        private static readonly Color B1 = Color.FromRgb(200, 200, 200); // bezel bright
        private static readonly Color B2 = Color.FromRgb(160, 160, 160); // bezel mid
        private static readonly Color B3 = Color.FromRgb(120, 120, 120); // bezel dark
        private static readonly Color Sc = Color.FromRgb(32, 32, 32);    // screen
        private static readonly Color Sd = Color.FromRgb(26, 26, 26);    // screen (dark)
        private static readonly Color S = Color.FromRgb(20, 20, 20);     // screen bg
        private static readonly Color C = Color.FromRgb(55, 200, 55);    // creeper green

        // @formatter:off
        private static readonly Color[,] Pixels =
        {
            { B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B1,  B2 },
            { B1,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  B3 },
            { B1,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  B3 },
            { B1,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sd,  Sd,  B3 },
            { B1,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sd,  Sd,  Sd,  S,   S,   S,   B3 },
            { B1,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sc,  Sd,  Sd,  S,   S,   S,   S,   S,   S,   B3 },
            { B1,  Sc,  Sc,  Sc,  Sc,  Sd,  Sd,  S,   S,   C,   C,   S,   S,   C,   C,   S,   B3 },
            { B1,  Sc,  Sc,  Sd,  Sd,  S,   S,   S,   S,   C,   C,   S,   S,   C,   C,   S,   B3 },
            { B1,  Sd,  Sd,  S,   S,   S,   S,   S,   S,   S,   S,   C,   C,   S,   S,   S,   B3 },
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

            var pixelGrid = new Grid();
            for (int c = 0; c < cols; c++)
                pixelGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
            for (int r = 0; r < textRows; r++)
                pixelGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));

            for (int row = 0; row < textRows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var topColor = Pixels[row * 2, col];
                    var bottomColor = Pixels[row * 2 + 1, col];

                    var cell = new TextBlock
                    {
                        Text = "\u2580",
                        Foreground = new SolidColorBrush(topColor),
                        Background = new SolidColorBrush(bottomColor),
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                    };

                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    pixelGrid.Children.Add(cell);
                }
            }

            var prompt = new TextBlock
            {
                Text = " ＞_",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Background = new SolidColorBrush(Sc),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            Grid.SetRow(prompt, 1);
            Grid.SetColumn(prompt, 1);
            Grid.SetColumnSpan(prompt, 4);
            pixelGrid.Children.Add(prompt);

            return pixelGrid;
        }

        #endregion

        private static Run Lbl(string text) =>
            new(text + " ") { Foreground = Pal.Gray };

        private static Run Val(string text, IBrush color) =>
            new(text) { Foreground = color };

        private static class Pal
        {
            public static readonly IBrush Gray = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            public static readonly IBrush DarkGray = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            public static readonly IBrush Aqua = new SolidColorBrush(Color.FromRgb(85, 255, 255));
            public static readonly IBrush Green = new SolidColorBrush(Color.FromRgb(85, 255, 85));
            public static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(255, 170, 0));
        }
    }
}
