using System;
using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class List : Command
    {
        public override string CmdName { get { return "list"; } }
        public override string CmdUsage { get { return "list"; } }
        public override string CmdDesc { get { return Translations.cmd_list_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            return string.Format(Translations.cmd_list_players, String.Join(", ", handler.GetOnlinePlayers()));
        }
    }
}

