using System;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;

namespace MinecraftClient.Commands
{
    class Bots : Command
    {
        public override string CmdName { get { return "bots"; } }
        public override string CmdUsage { get { return "bots [list|unload <bot name|all>]"; } }
        public override string CmdDesc { get { return Translations.cmd_bots_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("list")
                        .Executes(r => GetUsage(r.Source, "list")))
                    .Then(l => l.Literal("unload")
                        .Executes(r => GetUsage(r.Source, "unload")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoListBot(r.Source))
                .Then(l => l.Literal("list")
                    .Executes(r => DoListBot(r.Source)))
                .Then(l => l.Literal("unload")
                    .Then(l => l.Argument("BotName", MccArguments.BotName())
                        .Executes(r => DoUnloadBot(r.Source, Arguments.GetString(r, "BotName")))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "list"      =>  GetCmdDescTranslated(),
                "unload"    =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int DoListBot(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            int length = handler.GetLoadedChatBots().Count;
            if (length == 0)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_bots_noloaded);

            StringBuilder sb = new();
            for (int i = 0; i < length; i++)
            {
                sb.Append(handler.GetLoadedChatBots()[i].GetType().Name);
                if (i != length - 1)
                    sb.Append(" ,");
            }

            return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_bots_list + ": " + sb.ToString());
        }

        private int DoUnloadBot(CmdResult r, string botName)
        {
            McClient handler = CmdResult.currentHandler!;
            if (botName.ToLower().Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (handler.GetLoadedChatBots().Count == 0)
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_bots_noloaded);
                else
                {
                    handler.UnloadAllBots();
                    return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_bots_unloaded_all);
                }
            }
            else
            {
                ChatBot? bot = handler.GetLoadedChatBots().Find(bot => bot.GetType().Name.ToLower() == botName.ToLower());
                if (bot == null)
                    return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_bots_notfound, botName));
                else
                {
                    handler.BotUnLoad(bot);
                    return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_bots_unloaded, botName));
                }
            }
        }
    }
}
