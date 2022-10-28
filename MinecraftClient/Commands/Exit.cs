using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Exit : Command
    {
        public override string CmdName { get { return "exit"; } }
        public override string CmdUsage { get { return "exit"; } }
        public override string CmdDesc { get { return Translations.cmd_exit_desc; } }

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
