using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    class Health : Command
    {
        public override string CmdName { get { return "health"; } }
        public override string CmdUsage { get { return "health"; } }
        public override string CmdDesc { get { return Translations.cmd_health_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            return string.Format(Translations.cmd_health_response, handler.GetHealth(), handler.GetSaturation(), handler.GetLevel(), handler.GetTotalExperience());
        }
    }
}
