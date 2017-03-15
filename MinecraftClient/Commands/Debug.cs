using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Debug : Command
    {
        public override string CMDName { get { return "debug"; } }
        public override string CMDDesc { get { return "debug [on|off]: toggle debug messages."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                Settings.DebugMessages = (getArg(command).ToLower() == "on");
            }
            else Settings.DebugMessages = !Settings.DebugMessages;
            return "Debug messages are now " + (Settings.DebugMessages ? "ON" : "OFF");
        }
    }
}
