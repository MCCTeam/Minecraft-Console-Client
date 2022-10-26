using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    public class Exit : Command
    {
        public override string CmdName { get { return "exit"; } }
        public override string CmdUsage { get { return "exit"; } }
        public override string CmdDesc { get { return "cmd.exit.desc"; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient? handler, string command, Dictionary<string, object>? localVars)
        {
            Program.Exit();
            return "";
        }

        public override IEnumerable<string> GetCMDAliases()
        {
            return new string[] { "quit" };
        }
    }
}
