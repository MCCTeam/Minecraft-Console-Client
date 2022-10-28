using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Sneak : Command
    {
        private bool sneaking = false;
        public override string CmdName { get { return "Sneak"; } }
        public override string CmdUsage { get { return "Sneak"; } }
        public override string CmdDesc { get { return Translations.cmd_sneak_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (sneaking)
            {
                var result = handler.SendEntityAction(Protocol.EntityActionType.StopSneaking);
                if (result)
                    sneaking = false;
                return result ? Translations.cmd_sneak_off : Translations.general_fail;
            }
            else
            {
                var result = handler.SendEntityAction(Protocol.EntityActionType.StartSneaking);
                if (result)
                    sneaking = true;
                return result ? Translations.cmd_sneak_on : Translations.general_fail;
            }
        }
    }
}