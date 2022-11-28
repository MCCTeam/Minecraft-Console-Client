using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftClient.Commands
{
    class Bots : Command
    {
        public override string CmdName { get { return "bots"; } }
        public override string CmdUsage { get { return "bots [list|unload <bot name|all>]"; } }
        public override string CmdDesc { get { return Translations.cmd_bots_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string[] args = GetArgs(command);

                if (args.Length == 1)
                {
                    if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
                    {
                        StringBuilder sb = new();

                        int length = handler.GetLoadedChatBots().Count;

                        if (length == 0)
                            return Translations.cmd_bots_noloaded;

                        for (int i = 0; i < length; i++)
                        {
                            sb.Append(handler.GetLoadedChatBots()[i].GetType().Name);

                            if (i != length - 1)
                                sb.Append(" ,");

                        }

                        return Translations.cmd_bots_list + ": " + sb.ToString();
                    }

                }
                else if (args.Length == 2)
                {
                    if (args[0].Equals("unload", StringComparison.OrdinalIgnoreCase))
                    {
                        string botName = args[1].Trim();

                        if (botName.ToLower().Equals("all", StringComparison.OrdinalIgnoreCase))
                        {
                            if (handler.GetLoadedChatBots().Count == 0)
                                return Translations.cmd_bots_noloaded;

                            handler.UnloadAllBots();
                            return Translations.cmd_bots_unloaded_all;
                        }
                        else
                        {
                            ChatBot? bot = handler.GetLoadedChatBots().Find(bot => bot.GetType().Name.ToLower() == botName.ToLower());

                            if (bot == null)
                                return string.Format(Translations.cmd_bots_notfound, botName);

                            handler.BotUnLoad(bot);
                            return string.Format(Translations.cmd_bots_unloaded, botName);
                        }
                    }
                }
            }

            return GetCmdDescTranslated();
        }
    }
}
