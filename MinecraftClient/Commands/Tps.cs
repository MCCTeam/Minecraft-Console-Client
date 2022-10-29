using System;
using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    class Tps : Command
    {
        public override string CmdName { get { return "tps"; } }
        public override string CmdUsage { get { return "tps"; } }
        public override string CmdDesc { get { return Translations.cmd_tps_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            var tps = Math.Round(handler.GetServerTPS(), 2);
            string color;
            if (tps < 10)
                color = "§c";  // Red
            else if (tps < 15)
                color = "§e";  // Yellow
            else 
                color = "§a"; // Green
            return Translations.cmd_tps_current + ": " + color + tps;
        }
    }
}
