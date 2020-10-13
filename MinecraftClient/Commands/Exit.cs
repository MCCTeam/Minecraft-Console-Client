using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Exit : Command
    {
        public override string CmdName { get { return "exit"; } }
        public override string CmdUsage { get { return "exit"; } }
        public override string CmdDesc { get { return "cmd.exit.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            Program.Exit();
            return "";
        }

        public override IEnumerable<string> getCMDAliases()
        {
            return new string[] { "quit" };
        }
    }
}
