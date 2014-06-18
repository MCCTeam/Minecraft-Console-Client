using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Connect : Command
    {
        public override string CMDName { get { return "connect"; } }
        public override string CMDDesc { get { return "connect <serverip>: connect to the specified server."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                Settings.setServerIP(getArgs(command)[0]);
                Program.Restart();
                return "";
            }
            else return CMDDesc;
        }
    }
}
