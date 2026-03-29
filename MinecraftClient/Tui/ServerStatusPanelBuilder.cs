using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace MinecraftClient.Tui
{
    internal static class ServerStatusPanelBuilder
    {
        private const int MaxSamplePlayers = 10;
        private const int FaviconDisplaySize = 16;

        internal static Border Build(Protocol.ServerStatusInfo info)
        {
            var contentPanel = new DockPanel { Background = Brushes.Black };

            if (info.FaviconBase64 is not null)
            {
                var iconGrid = BuildFaviconGrid(info.FaviconBase64, FaviconDisplaySize);
                iconGrid.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(iconGrid, Dock.Left);
                contentPanel.Children.Add(iconGrid);
            }

            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(1, 0, 0, 0),
            };

            AddMotd(infoPanel, info);
            AddAddress(infoPanel, info);
            AddVersion(infoPanel, info);
            AddConnectingAs(infoPanel, info);
            AddPing(infoPanel, info);
            AddPlayers(infoPanel, info);
            AddSamplePlayers(infoPanel, info);

            contentPanel.Children.Add(infoPanel);

            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromArgb(240, 20, 20, 20)),
                Padding = new Thickness(1, 0),
                Child = contentPanel,
                Margin = new Thickness(0, 1),
            };
        }

        private static void AddMotd(StackPanel panel, Protocol.ServerStatusInfo info)
        {
            if (string.IsNullOrEmpty(info.MotdRaw))
                return;

            try
            {
                string motdFormatted = Protocol.Message.ChatParser.ParseText(info.MotdRaw);
                foreach (string line in motdFormatted.Split('\n'))
                    panel.Children.Add(McColorParser.CreateColoredTextBlock(line, TextWrapping.NoWrap));
            }
            catch
            {
                panel.Children.Add(new TextBlock
                {
                    Text = info.MotdRaw,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.NoWrap,
                });
            }
        }

        private static void AddAddress(StackPanel panel, Protocol.ServerStatusInfo info)
        {
            var row = new TextBlock();
            row.Inlines!.Add(Label(Translations.mcc_server_info_label_server));
            row.Inlines.Add(Value(info.Host, McColors.Aqua));
            row.Inlines.Add(new Run($":{info.Port}") { Foreground = McColors.Gray });
            panel.Children.Add(row);
        }

        private static void AddVersion(StackPanel panel, Protocol.ServerStatusInfo info)
        {
            string versionClean = Scripting.ChatBot.GetVerbatim(info.VersionName);
            var row = new TextBlock();
            row.Inlines!.Add(Label(Translations.mcc_server_info_label_version));
            row.Inlines.Add(Value(versionClean, McColors.Aqua));
            row.Inlines.Add(new Run(" (") { Foreground = McColors.Gray });
            row.Inlines.Add(new Run(string.Format(Translations.mcc_server_info_label_protocol, info.ProtocolVersion))
                { Foreground = McColors.Gray });
            row.Inlines.Add(new Run(")") { Foreground = McColors.Gray });
            panel.Children.Add(row);
        }

        private static void AddConnectingAs(StackPanel panel, Protocol.ServerStatusInfo info)
        {
            if (info.ResolvedProtocol == 0)
                return;

            string resolvedMcVer = Protocol.ProtocolHandler.ProtocolVersion2MCVer(info.ResolvedProtocol);
            var row = new TextBlock();
            row.Inlines!.Add(Label(Translations.mcc_server_info_label_connecting_as));
            row.Inlines.Add(Value(resolvedMcVer, McColors.Green));
            row.Inlines.Add(new Run(" (") { Foreground = McColors.Gray });
            row.Inlines.Add(new Run(string.Format(Translations.mcc_server_info_label_protocol, info.ResolvedProtocol))
                { Foreground = McColors.Gray });
            row.Inlines.Add(new Run(")") { Foreground = McColors.Gray });
            panel.Children.Add(row);
        }

        private static void AddPing(StackPanel panel, Protocol.ServerStatusInfo info)
        {
            if (info.PingMs < 0)
                return;

            var pingColor = info.PingMs < 100
                ? McColors.Green
                : info.PingMs < 300
                    ? McColors.Yellow
                    : McColors.Red;

            var row = new TextBlock();
            row.Inlines!.Add(Label(Translations.mcc_server_info_label_ping));
            row.Inlines.Add(new Run(string.Format(Translations.mcc_server_info_label_ping_ms, info.PingMs))
                { Foreground = pingColor });
            panel.Children.Add(row);
        }

        private static void AddPlayers(StackPanel panel, Protocol.ServerStatusInfo info)
        {
            var row = new TextBlock();
            row.Inlines!.Add(Label(Translations.mcc_server_info_label_players));
            row.Inlines.Add(Value($"{info.OnlinePlayers}", McColors.Green));
            row.Inlines.Add(new Run("/") { Foreground = McColors.Gray });
            row.Inlines.Add(Value($"{info.MaxPlayers}", McColors.Red));
            panel.Children.Add(row);
        }

        private static void AddSamplePlayers(StackPanel panel, Protocol.ServerStatusInfo info)
        {
            if (info.SamplePlayers.Count == 0)
                return;

            panel.Children.Add(new TextBlock
            {
                Text = Translations.mcc_server_info_label_online,
                Foreground = McColors.Gray,
            });

            int shown = Math.Min(info.SamplePlayers.Count, MaxSamplePlayers);
            for (int i = 0; i < shown; i++)
            {
                string name = info.SamplePlayers[i].Name;
                if (name.Contains('\u00a7'))
                    panel.Children.Add(McColorParser.CreateColoredTextBlock($"  {name}", TextWrapping.NoWrap));
                else
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"  {name}",
                        Foreground = McColors.Green,
                    });
            }

            if (info.SamplePlayers.Count > shown)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"  {string.Format(Translations.mcc_server_info_sample_more, info.SamplePlayers.Count - shown)}",
                    Foreground = McColors.Gray,
                });
            }
        }

        private static Run Label(string text) =>
            new(text + " ") { Foreground = McColors.Gray };

        private static Run Value(string text, IBrush color) =>
            new(text) { Foreground = color };

        #region Favicon Rendering

        private static Grid BuildFaviconGrid(string base64Png, int displaySize)
        {
            byte[] pngBytes;
            try
            {
                pngBytes = Convert.FromBase64String(base64Png);
            }
            catch
            {
                return new Grid();
            }

            int srcWidth, srcHeight;
            byte[] rgba;
            try
            {
                (srcWidth, srcHeight, rgba) = DecodePngToRgba(pngBytes);
            }
            catch
            {
                return new Grid();
            }

            int cellCols = displaySize;
            int cellRows = displaySize / 2;

            var grid = new Grid();
            for (int c = 0; c < cellCols; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
            for (int r = 0; r < cellRows; r++)
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));

            for (int row = 0; row < cellRows; row++)
            {
                for (int col = 0; col < cellCols; col++)
                {
                    int topPixelY = row * 2;
                    int bottomPixelY = row * 2 + 1;

                    var topColor = SamplePixel(rgba, srcWidth, srcHeight, col, topPixelY, cellCols, displaySize);
                    var bottomColor = SamplePixel(rgba, srcWidth, srcHeight, col, bottomPixelY, cellCols, displaySize);

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
                    grid.Children.Add(cell);
                }
            }

            return grid;
        }

        private static Color SamplePixel(byte[] rgba, int srcW, int srcH, int dstX, int dstY, int dstW, int dstH)
        {
            int srcX = dstX * srcW / dstW;
            int srcY = dstY * srcH / dstH;
            srcX = Math.Clamp(srcX, 0, srcW - 1);
            srcY = Math.Clamp(srcY, 0, srcH - 1);

            int idx = (srcY * srcW + srcX) * 4;
            if (idx + 3 >= rgba.Length)
                return Color.FromRgb(0, 0, 0);

            byte r = rgba[idx];
            byte g = rgba[idx + 1];
            byte b = rgba[idx + 2];
            byte a = rgba[idx + 3];

            return a < 128 ? Color.FromRgb(0, 0, 0) : Color.FromRgb(r, g, b);
        }

        private static (int Width, int Height, byte[] Rgba) DecodePngToRgba(byte[] png)
        {
            using var image = new ImageMagick.MagickImage(png);
            int w = (int)image.Width;
            int h = (int)image.Height;

            using var pixels = image.GetPixelsUnsafe();
            var rgba = new byte[w * h * 4];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var pixel = pixels.GetPixel(x, y)!;
                    int idx = (y * w + x) * 4;
                    var color = pixel.ToColor()!;
                    rgba[idx] = (byte)(color.R >> 8);
                    rgba[idx + 1] = (byte)(color.G >> 8);
                    rgba[idx + 2] = (byte)(color.B >> 8);
                    rgba[idx + 3] = (byte)(color.A >> 8);
                }
            }

            return (w, h, rgba);
        }

        #endregion

        private static class McColors
        {
            public static readonly IBrush Gray = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            public static readonly IBrush Aqua = new SolidColorBrush(Color.FromRgb(85, 255, 255));
            public static readonly IBrush Green = new SolidColorBrush(Color.FromRgb(85, 255, 85));
            public static readonly IBrush Red = new SolidColorBrush(Color.FromRgb(255, 85, 85));
            public static readonly IBrush Yellow = new SolidColorBrush(Color.FromRgb(255, 255, 85));
        }
    }
}
