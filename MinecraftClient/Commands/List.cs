using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class List : Command
    {
        public override string CMDName { get { return "list"; } }
        public override string CMDDesc { get { return "list: get the player list."; } }

        public override string Run(McTcpClient handler, string command)
        {
            return "PlayerList: " + String.Join(", ", handler.GetOnlinePlayers());
        }
    }
}

