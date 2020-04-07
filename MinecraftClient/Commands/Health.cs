using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Health : Command
    {
        public override string CMDName { get { return "health"; } }
        public override string CMDDesc { get { return "health: Display Health and Food saturation."; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            return "Health: " + handler.GetHealth() + ", Saturation: " + handler.GetSaturation();
        }
    }
}
