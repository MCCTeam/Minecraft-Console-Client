using System;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    class Tps : Command
    {
        public override string CmdName { get { return "tps"; } }
        public override string CmdUsage { get { return "tps"; } }
        public override string CmdDesc { get { return Translations.cmd_tps_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoLogTps(r.Source))
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
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int DoLogTps(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            var tps = Math.Round(handler.GetServerTPS(), 2);
            string color;
            if (tps < 10)
                color = "§c";  // Red
            else if (tps < 15)
                color = "§e";  // Yellow
            else
                color = "§a"; // Green
            return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_tps_current + ": " + color + tps);
        }
    }
}
