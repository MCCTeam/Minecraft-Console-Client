using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using static MinecraftClient.Settings.ConsoleConfigHealper.ConsoleConfig;

namespace MinecraftClient.Commands
{
    public class Tryout : Command
    {
        public override string CmdName => "tryout";
        public override string CmdUsage => "tryout [list|tui]";
        public override string CmdDesc => Translations.cmd_tryout_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => ListTryouts(r.Source))
                .Then(l => l.Literal("list")
                    .Executes(r => ListTryouts(r.Source)))
                .Then(l => l.Literal("tui")
                    .Executes(r => EnableTuiMode(r.Source)))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r)
        {
            return r.SetAndReturn(GetCmdDescTranslated());
        }

        private int ListTryouts(CmdResult r)
        {
            return r.SetAndReturn(string.Join('\n',
                GetCmdDescTranslated(),
                string.Empty,
                Translations.cmd_tryout_list_header,
                $" - {Translations.cmd_tryout_list_tui}"));
        }

        private int EnableTuiMode(CmdResult r)
        {
            var previousMode = Settings.Config.Console.General.ConsoleMode;
            if (previousMode == ConsoleModeType.tui)
            {
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_tryout_tui_already_enabled);
            }

            Settings.Config.Console.General.ConsoleMode = ConsoleModeType.tui;
            Program.WriteBackSettings();

            return r.SetAndReturn(CmdResult.Status.Done,
                string.Format(Translations.cmd_tryout_tui_enabled, previousMode, ConsoleModeType.tui));
        }
    }
}
