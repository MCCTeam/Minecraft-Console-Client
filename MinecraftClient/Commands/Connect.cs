﻿using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    public class Connect : Command
    {
        public override string CmdName { get { return "connect"; } }
        public override string CmdUsage { get { return "connect <server> [account]"; } }
        public override string CmdDesc { get { return "cmd.connect.desc"; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient? handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string[] args = GetArgs(command);
                if (args.Length > 1)
                {
                    if (!Settings.Config.Main.Advanced.SetAccount(args[1]))
                    {
                        return Translations.Get("cmd.connect.unknown", args[1]);
                    }
                }

                if (Settings.Config.Main.SetServerIP(new Settings.MainConfigHealper.MainConfig.ServerInfoConfig(args[0]), true))
                {
                    Program.Restart();
                    return "";
                }
                else return Translations.Get("cmd.connect.invalid_ip", args[0]);
            }
            else return GetCmdDescTranslated();
        }
    }
}
