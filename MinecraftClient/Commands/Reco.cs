using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Reco : Command
    {
        public override string CmdName { get { return "reco"; } }
        public override string CmdUsage { get { return "reco [account]"; } }
        public override string CmdDesc { get { return Translations.cmd_reco_desc; } }

        public override string Run(McClient? handler, string command, Dictionary<string, object>? localVars)
        {
            string[] args = GetArgs(command);
            if (args.Length > 0)
            {
                if (!Settings.Config.Main.Advanced.SetAccount(args[0]))
                {
                    return string.Format(Translations.cmd_connect_unknown, args[0]);
                }
            }
            Program.Restart(keepAccountAndServerSettings: true);
            return "";
        }
    }
}
