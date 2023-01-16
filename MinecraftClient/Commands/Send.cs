using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Send : Command
    {
        public override string CmdName { get { return "send"; } }
        public override string CmdUsage { get { return "send <text>"; } }
        public override string CmdDesc { get { return Translations.cmd_send_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("any", Arguments.GreedyString())
                    .Executes(r => DoSendText(r.Source, Arguments.GetString(r, "any"))))
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

        private int DoSendText(CmdResult r, string command)
        {
            McClient handler = CmdResult.currentHandler!;
            handler.SendText(command);
            return r.SetAndReturn(CmdResult.Status.Done);
        }
    }
}
