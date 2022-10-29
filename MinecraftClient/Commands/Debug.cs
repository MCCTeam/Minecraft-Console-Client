using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    public class Debug : Command
    {
        public override string CmdName { get { return "debug"; } }
        public override string CmdUsage { get { return "debug [on|off]"; } }
        public override string CmdDesc { get { return Translations.cmd_debug_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
                Settings.Config.Logging.DebugMessages = (GetArg(command).ToLower() == "on");
            else
                Settings.Config.Logging.DebugMessages = !Settings.Config.Logging.DebugMessages;
            if (Settings.Config.Logging.DebugMessages)
                return Translations.cmd_debug_state_on;
            else
                return Translations.cmd_debug_state_off;
        }
    }
}
