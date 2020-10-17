using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Reco : Command
    {
        public override string CmdName { get { return "reco"; } }
        public override string CmdUsage { get { return "reco [account]"; } }
        public override string CmdDesc { get { return "cmd.reco.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            string[] args = getArgs(command);
            if (args.Length > 0)
            {
                if (!Settings.SetAccount(args[0]))
                {
                    return Translations.Get("cmd.connect.unknown", args[0]);
                }
            }
            Program.Restart();
            return "";
        }
    }
}
