using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Debug : Command
    {
        public override string CmdName { get { return "debug"; } }
        public override string CmdUsage { get { return "debug [on|off]"; } }
        public override string CmdDesc { get { return "cmd.debug.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
                Settings.Config.Logging.DebugMessages = (GetArg(command).ToLower() == "on");
            else
                Settings.Config.Logging.DebugMessages = !Settings.Config.Logging.DebugMessages;
            return Translations.Get(Settings.Config.Logging.DebugMessages ? "cmd.debug.state_on" : "cmd.debug.state_off");
        }
    }
}
