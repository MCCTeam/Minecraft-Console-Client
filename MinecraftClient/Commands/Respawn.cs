using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Respawn : Command
    {
        public override string CMDName { get { return "respawn"; } }
        public override string CMDDesc { get { return "respawn: respawn after death."; } }

        public override string Run(McTcpClient handler, string command)
        {
            handler.SendRespawnPacket();
            return "You have respawned.";
        }
    }
}
