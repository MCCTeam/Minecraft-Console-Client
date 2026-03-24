using System;
using System.Linq;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;

namespace MinecraftClient.Commands
{
    public class Debug : Command
    {
        public override string CmdName { get { return "debug"; } }
        public override string CmdUsage { get { return "debug [on|off|state]"; } }
        public override string CmdDesc { get { return Translations.cmd_debug_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => SetDebugMode(r.Source, true))
                .Then(l => l.Literal("on")
                    .Executes(r => SetDebugMode(r.Source, false, true)))
                .Then(l => l.Literal("off")
                    .Executes(r => SetDebugMode(r.Source, false, false)))
                .Then(l => l.Literal("state")
                    .Executes(r => ShowState(r.Source)))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int SetDebugMode(CmdResult r, bool flip, bool mode = false)
        {
            McClient handler = CmdResult.currentHandler!;

            if (flip)
                Settings.Config.Logging.DebugMessages = !Settings.Config.Logging.DebugMessages;
            else
                Settings.Config.Logging.DebugMessages = mode;

            handler.Log.DebugEnabled = Settings.Config.Logging.DebugMessages;

            if (Settings.Config.Logging.DebugMessages)
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_debug_state_on);
            else
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_debug_state_off);
        }

        private int ShowState(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            var sb = new StringBuilder();

            sb.AppendLine("§e=== MCC Debug State ===");
            sb.AppendLine($"§7Server:    §f{handler.GetServerHost()}:{handler.GetServerPort()}");
            sb.AppendLine($"§7Username:  §f{handler.GetUsername()}");
            sb.AppendLine($"§7Protocol:  §f{handler.GetProtocolVersion()}");
            sb.AppendLine($"§7GameMode:  §f{handler.GetGamemode()}");
            sb.AppendLine($"§7Health:    §f{handler.GetHealth():F1}");
            sb.AppendLine($"§7Food:      §f{handler.GetSaturation()}");

            var loc = handler.GetCurrentLocation();
            sb.AppendLine($"§7Location:  §f{loc.X:F2}, {loc.Y:F2}, {loc.Z:F2}");

            sb.AppendLine($"§7TPS:       §f{handler.GetServerTPS():F1}");

            sb.AppendLine($"§7Console:   §f{(ConsoleIO.Backend?.GetType().Name ?? "null")}");

            var features = new StringBuilder();
            features.Append(handler.GetTerrainEnabled() ? "§aTerrain " : "§8Terrain ");
            features.Append(handler.GetInventoryEnabled() ? "§aInventory " : "§8Inventory ");
            features.Append(handler.GetEntityHandlingEnabled() ? "§aEntity " : "§8Entity ");
            sb.AppendLine($"§7Features:  {features}");

            sb.AppendLine($"§7Debug:     §f{(Settings.Config.Logging.DebugMessages ? "§aON" : "§cOFF")}");

            var bots = handler.GetLoadedChatBots();
            sb.AppendLine($"§7Bots ({bots.Count}): §f{string.Join(", ", bots.Select(b => b.GetType().Name))}");

            var players = handler.GetOnlinePlayers();
            sb.AppendLine($"§7Players:   §f{players.Length} online");

            handler.Log.Info(sb.ToString());
            return r.SetAndReturn(CmdResult.Status.Done);
        }
    }
}
