using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    public class Respawn : Command
    {
        public override string CmdName { get { return "respawn"; } }
        public override string CmdUsage { get { return "respawn"; } }
        public override string CmdDesc { get { return Translations.cmd_respawn_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            handler.SendRespawnPacket();
            return Translations.cmd_respawn_done;
        }
    }
}
