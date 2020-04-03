using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Status : Command
    {
        public override string CMDName { get { return "status"; } }
        public override string CMDDesc { get { return "status: Display Health and Food saturation."; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            return "Health: " + handler.GetHealth() + "\n[MCC] Saturation: " + handler.GetSaturation(); // any good ways to print multiple lines?
        }
    }
}
