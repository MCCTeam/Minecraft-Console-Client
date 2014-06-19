using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Send : Command
    {
        public override string CMDName { get { return "send"; } }
        public override string CMDDesc { get { return "send <text>: send a chat message or command."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                handler.SendText(getArg(command));
                return "";
            }
            else return CMDDesc;
        }
    }
}
