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
                VerticalAlignment = VerticalAlignment.Center,
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
                Margin = new Thickness(0),
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

        private static Grid BuildFaviconGrid(string base64Png, int displaySize) =>
            IconGridBuilder.BuildFromBase64(base64Png, displaySize);

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
