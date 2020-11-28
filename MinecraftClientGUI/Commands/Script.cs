using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Script : Command
    {
        public override string CmdName { get { return "script"; } }
        public override string CmdUsage { get { return "script <scriptname>"; } }
        public override string CmdDesc { get { return "cmd.script.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                handler.BotLoad(new ChatBots.Script(getArg(command), null, localVars));
                return "";
            }
            else return GetCmdDescTranslated();
        }
    }
}
