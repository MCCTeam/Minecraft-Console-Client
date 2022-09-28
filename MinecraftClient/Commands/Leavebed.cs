using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class leaveBedCommand : Command
    {
        public override string CmdName { get { return "leavebed"; } }
        public override string CmdUsage { get { return "leavebed"; } }
        public override string CmdDesc { get { return "cmd.leavebed.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            handler.SendEntityAction(Protocol.EntityActionType.LeaveBed);
            return Translations.TryGet("cmd.leavebed.leaving");
        }
    }
}