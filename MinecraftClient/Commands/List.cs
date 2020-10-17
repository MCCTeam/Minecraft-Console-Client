using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class List : Command
    {
        public override string CmdName { get { return "list"; } }
        public override string CmdUsage { get { return "list"; } }
        public override string CmdDesc { get { return "cmd.list.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            return Translations.Get("cmd.list.players", String.Join(", ", handler.GetOnlinePlayers()));
        }
    }
}

