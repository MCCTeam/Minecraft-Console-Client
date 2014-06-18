using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Reco : Command
    {
        public override string CMDName { get { return "reco"; } }
        public override string CMDDesc { get { return "reco: restart and reconnect to the server."; } }

        public override string Run(McTcpClient handler, string command)
        {
            Program.Restart();
            return "";
        }
    }
}
