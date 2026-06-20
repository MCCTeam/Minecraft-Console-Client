using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class ClearConsole : Command
    {
        public override string CmdName => "clear-console";
        public override string CmdUsage => "clear-console";
        public override string CmdDesc => Translations.cmd_clear_console_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source))
                )
            );

            var clearConsole = dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => Execute(r.Source))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );

            dispatcher.Register(l => l.Literal("cc")
                .Executes(r => Execute(r.Source))
                .Redirect(clearConsole)
            );
        }

        private int GetUsage(CmdResult result)
        {
            return result.SetAndReturn(GetCmdDescTranslated());
        }

        private int Execute(CmdResult result)
        {
            ConsoleIO.ClearConsole();
            return result.SetAndReturn(CmdResult.Status.Done);
        }
    }
}
