using System;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class List : Command
    {
        public override string CmdName { get { return "list"; } }
        public override string CmdUsage { get { return "list"; } }
        public override string CmdDesc { get { return Translations.cmd_list_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoListPlayers(r.Source))
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

        private int DoListPlayers(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_list_players, String.Join(", ", handler.GetOnlinePlayers())));
        }
    }
}

