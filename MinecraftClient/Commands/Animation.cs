using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Animation : Command
    {
        public override string CmdName { get { return "animation"; } }
        public override string CmdUsage { get { return "animation <mainhand|offhand>"; } }
        public override string CmdDesc { get { return Translations.cmd_animation_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("mainhand")
                        .Executes(r => GetUsage(r.Source, "mainhand")))
                    .Then(l => l.Literal("offhand")
                        .Executes(r => GetUsage(r.Source, "offhand")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoAnimation(r.Source, handler, mainhand: true))
                .Then(l => l.Literal("mainhand")
                    .Executes(r => DoAnimation(r.Source, handler, mainhand: true)))
                .Then(l => l.Literal("offhand")
                    .Executes(r => DoAnimation(r.Source, handler, mainhand: false)))
                .Then(l => l.Literal("_help")
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "mainhand"  =>  GetCmdDescTranslated(),
                "offhand"   =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private static int DoAnimation(CmdResult r, McClient handler, bool mainhand)
        {
            return r.SetAndReturn(handler.DoAnimation(mainhand ? 1 : 0));
        }
    }
}
