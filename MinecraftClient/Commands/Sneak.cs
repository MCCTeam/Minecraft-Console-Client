using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Sneak : Command
    {
        public override string CmdName => "sneak";
        public override string CmdUsage => "sneak";
        public override string CmdDesc => Translations.cmd_sneak_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoSneak(r.Source))
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

        private int DoSneak(CmdResult r)
        {
            var handler = CmdResult.currentHandler!;

            if (handler.IsSneaking)
            {
                if (!handler.SendEntityAction(Protocol.EntityActionType.StopSneaking))
                    return r.SetAndReturn(CmdResult.Status.Fail);

                handler.IsSneaking = false;
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_sneak_off);
            }

            if (!handler.SendEntityAction(Protocol.EntityActionType.StartSneaking))
                return r.SetAndReturn(CmdResult.Status.Fail);

            handler.IsSneaking = true;
            return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_sneak_on);
        }
    }
}