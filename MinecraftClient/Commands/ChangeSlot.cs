using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    class ChangeSlot : Command
    {
        public override string CmdName { get { return "changeslot"; } }
        public override string CmdUsage { get { return "changeslot <1-9>"; } }
        public override string CmdDesc { get { return Translations.cmd_changeSlot_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("Slot", MccArguments.HotbarSlot())
                    .Executes(r => DoChangeSlot(r.Source, Arguments.GetInteger(r, "Slot"))))
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

        private int DoChangeSlot(CmdResult r, int slot)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(Status.FailNeedInventory);

            if (handler.ChangeSlot((short)(slot - 1)))
                return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_changeSlot_changed, slot));
            else
                return r.SetAndReturn(Status.Fail, Translations.cmd_changeSlot_fail);
        }
    }
}
