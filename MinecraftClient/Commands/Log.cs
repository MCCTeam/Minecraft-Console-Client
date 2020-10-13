using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Log : Command
    {
        public override string CmdName { get { return "log"; } }
        public override string CmdUsage { get { return "log <text>"; } }
        public override string CmdDesc { get { return "cmd.log.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                ConsoleIO.WriteLogLine(getArg(command));
                return "";
            }
            else return GetCmdDescTranslated();
        }
    }
}
