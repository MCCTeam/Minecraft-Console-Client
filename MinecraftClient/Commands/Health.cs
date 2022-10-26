using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    class Health : Command
    {
        public override string CmdName { get { return "health"; } }
        public override string CmdUsage { get { return "health"; } }
        public override string CmdDesc { get { return "cmd.health.desc"; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            return Translations.Get("cmd.health.response", handler.GetHealth(), handler.GetSaturation(), handler.GetLevel(), handler.GetTotalExperience());
        }
    }
}
