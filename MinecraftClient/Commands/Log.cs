using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Log : Command
    {
        public override string CmdName { get { return "log"; } }
        public override string CmdUsage { get { return "log <text>"; } }
        public override string CmdDesc { get { return Translations.cmd_log_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                ConsoleIO.WriteLogLine(GetArg(command));
                return "";
            }
            else return GetCmdDescTranslated();
        }
    }
}
