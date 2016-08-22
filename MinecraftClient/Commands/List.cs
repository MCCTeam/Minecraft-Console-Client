using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class List : Command
    {
        public override string CMDName { get { return "list"; } }
        public override string CMDDesc { get { return "list [raw]: get the player list."; } }

        public override string Run(McTcpClient handler, string command)
        {
            bool rawNames = getArg(command).ToLower() == "raw";
            return "PlayerList: "
                + String.Join(", ",
                    handler.GetOnlinePlayers()
                        .OrderBy(player => player.Name)
                        .Select(player => rawNames
                            ? player.Name
                            : player.DisplayName));
        }
    }
}

