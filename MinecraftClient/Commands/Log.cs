using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    public class Log : Command
    {
        public override string CmdName { get { return "log"; } }
        public override string CmdUsage { get { return "log <text>"; } }
        public override string CmdDesc { get { return "cmd.log.desc"; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

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
