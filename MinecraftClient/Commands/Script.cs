using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Script : Command
    {
        public override string CMDName { get { return "script"; } }
        public override string CMDDesc { get { return "script <scriptname>: run a script file."; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                handler.BotLoad(new ChatBots.Script(getArg(command), null, localVars));
                return "";
            }
            else return CMDDesc;
        }
    }
}
