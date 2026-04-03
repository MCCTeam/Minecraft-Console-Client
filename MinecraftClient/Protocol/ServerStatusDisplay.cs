using System;
using System.Text;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Scripting;

namespace MinecraftClient.Protocol
{
    internal static class ServerStatusDisplay
    {
        private const int MaxSamplePlayers = 10;

        internal static void Show(ServerStatusInfo info)
        {
            if (ConsoleIO.Backend is Tui.TuiConsoleBackend tuiBackend)
                ShowTui(info, tuiBackend);
            else
                ShowClassic(info);
        }

        private static void ShowClassic(ServerStatusInfo info)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.Append("§8§m");
            sb.Append(new string('-', 50));
            sb.AppendLine("§r");

            if (!string.IsNullOrEmpty(info.MotdRaw))
            {
                try
                {
                    sb.AppendLine(ChatParser.ParseText(info.MotdRaw));
                }
                catch
                {
                    sb.AppendLine(info.MotdRaw);
                }
            }

            sb.Append("§f");
            sb.Append(Translations.mcc_server_info_label_server);
            sb.Append(" §b");
            sb.Append(info.Host);
            sb.Append("§7:§b");
            sb.AppendLine(info.Port.ToString());

            sb.Append("§f");
            sb.Append(Translations.mcc_server_info_label_version);
            sb.Append(" §b");
            sb.Append(ChatBot.GetVerbatim(info.VersionName));
            sb.Append(" §7(");
            sb.Append(string.Format(Translations.mcc_server_info_label_protocol, "§e" + info.ProtocolVersion + "§7"));
            sb.AppendLine(")");

            if (info.ResolvedProtocol != 0)
            {
                string resolvedMcVer = ProtocolHandler.ProtocolVersion2MCVer(info.ResolvedProtocol);
                sb.Append("§f");
                sb.Append(Translations.mcc_server_info_label_connecting_as);
                sb.Append(" §a");
                sb.Append(resolvedMcVer);
                sb.Append(" §7(");
                sb.Append(string.Format(Translations.mcc_server_info_label_protocol, "§a" + info.ResolvedProtocol + "§7"));
                sb.AppendLine(")");
            }

            if (info.PingMs >= 0)
            {
                sb.Append("§f");
                sb.Append(Translations.mcc_server_info_label_ping);
                sb.Append(" §a");
                sb.AppendLine(string.Format(Translations.mcc_server_info_label_ping_ms, info.PingMs));
            }

            sb.Append("§f");
            sb.Append(Translations.mcc_server_info_label_players);
            sb.Append(" §a");
            sb.Append(info.OnlinePlayers);
            sb.Append("§7/§c");
            sb.AppendLine(info.MaxPlayers.ToString());

            if (info.SamplePlayers.Count > 0)
            {
                sb.Append("§f");
                sb.AppendLine(Translations.mcc_server_info_label_online);

                int shown = Math.Min(info.SamplePlayers.Count, MaxSamplePlayers);
                for (int i = 0; i < shown; i++)
                    sb.AppendLine($"  §a{info.SamplePlayers[i].Name}");

                if (info.SamplePlayers.Count > shown)
                    sb.AppendLine($"  §7{string.Format(Translations.mcc_server_info_sample_more, info.SamplePlayers.Count - shown)}");
            }

            sb.Append("§8§m");
            sb.Append(new string('-', 50));
            sb.Append("§r");

            ConsoleIO.WriteLineFormatted(sb.ToString(), acceptnewlines: true);
        }

        private static void ShowTui(ServerStatusInfo info, Tui.TuiConsoleBackend backend)
        {
            var view = backend.GetView();
            if (view is null)
            {
                ShowClassic(info);
                return;
            }

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var panel = Tui.ServerStatusPanelBuilder.Build(info);
                view.AppendControlToLog(panel);
            });
        }
    }
}
