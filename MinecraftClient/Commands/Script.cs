﻿using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    public class Script : Command
    {
        public override string CmdName { get { return "script"; } }
        public override string CmdUsage { get { return "script <scriptname>"; } }
        public override string CmdDesc { get { return "cmd.script.desc"; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                handler.BotLoad(new ChatBots.Script(GetArg(command), null, localVars));
                return "";
            }
            else return GetCmdDescTranslated();
        }
    }
}
