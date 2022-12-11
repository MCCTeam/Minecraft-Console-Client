using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Exit : Command
    {
        public override string CmdName { get { return "exit"; } }
        public override string CmdUsage { get { return "exit"; } }
        public override string CmdDesc { get { return Translations.cmd_exit_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            var exit = dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoExit(r.Source, 0))
                .Then(l => l.Argument("ExitCode", Arguments.Integer())
                    .Executes(r => DoExit(r.Source, Arguments.GetInteger(r, "ExitCode"))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );

            dispatcher.Register(l => l.Literal("quit")
                .Executes(r => DoExit(r.Source, 0))
                .Redirect(exit)
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

        private int DoExit(CmdResult r, int code = 0)
        {
            Program.Exit(code);
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        internal static string DoExit(string command)
        {
            Program.Exit();
            return string.Empty;
        }
    }
}
