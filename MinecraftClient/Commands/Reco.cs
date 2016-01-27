using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Reco : Command
    {
        public override string CMDName { get { return "reco"; } }
        public override string CMDDesc { get { return "reco [account]: restart and reconnect to the server."; } }

        public override string Run(McTcpClient handler, string command)
        {
            string[] args = getArgs(command);
            if (args.Length > 0)
            {
                if (!Settings.SetAccount(args[0]))
                {
                    return "Unknown account '" + args[0] + "'.";
                }
            }
            Program.Restart();
            return "";
        }
    }
}
