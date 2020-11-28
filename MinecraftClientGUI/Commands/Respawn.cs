using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Respawn : Command
    {
        public override string CmdName { get { return "respawn"; } }
        public override string CmdUsage { get { return "respawn"; } }
        public override string CmdDesc { get { return "cmd.respawn.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            handler.SendRespawnPacket();
            return Translations.Get("cmd.respawn.done");
        }
    }
}
