using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Sneak : Command
    {
        private bool sneaking = false;
        public override string CmdName { get { return "sneak"; } }
        public override string CmdUsage { get { return "sneak"; } }
        public override string CmdDesc { get { return Translations.cmd_sneak_desc; } }

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
            McClient handler = CmdResult.currentHandler!;
            if (sneaking)
            {
                var result = handler.SendEntityActionAsync(Protocol.EntityActionType.StopSneaking).Result;
                if (result)
                    sneaking = false;
                if (result)
                    return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_sneak_off);
                else
                    return r.SetAndReturn(CmdResult.Status.Fail);
            }
            else
            {
                var result = handler.SendEntityActionAsync(Protocol.EntityActionType.StartSneaking).Result;
                if (result)
                    sneaking = true;
                if (result)
                    return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_sneak_on);
                else
                    return r.SetAndReturn(CmdResult.Status.Fail);
            }
        }
    }
}