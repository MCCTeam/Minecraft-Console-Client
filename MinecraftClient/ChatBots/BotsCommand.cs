using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftClient.Commands
{
    class BotsCommand : Command
    {
        public override string CmdName { get { return "bots"; } }
        public override string CmdUsage { get { return "bots [list|unload <bot name|all>]"; } }
        public override string CmdDesc { get { return "cmd.bots.desc"; } }

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
                            return Translations.TryGet("cmd.bots.noloaded");

                        for (int i = 0; i < length; i++)
                        {
                            sb.Append(handler.GetLoadedChatBots()[i].GetType().Name);

                            if (i != length - 1)
                                sb.Append(" ,");

                        }

                        return Translations.Get("cmd.bots.list") + ": " + sb.ToString();
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
                                return Translations.TryGet("cmd.bots.noloaded");

                            handler.UnloadAllBots();
                            return Translations.TryGet("cmd.bots.unloaded_all");
                        }
                        else
                        {
                            ChatBot? bot = handler.GetLoadedChatBots().Find(bot => bot.GetType().Name.ToLower() == botName.ToLower());

                            if (bot == null)
                                return Translations.TryGet("cmd.bots.notfound", botName);

                            handler.BotUnLoad(bot);
                            return Translations.TryGet("cmd.bots.unloaded", botName);
                        }
                    }
                }
            }

            return GetCmdDescTranslated();
        }
    }
}
