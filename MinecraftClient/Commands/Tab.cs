using Avalonia.Threading;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Tui;

namespace MinecraftClient.Commands
{
    public class Tab : Command
    {
        public override string CmdName => "tab";
        public override string CmdUsage => "tab";
        public override string CmdDesc => Translations.cmd_tab_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source)))
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => ShowTab(r.Source))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r) => r.SetAndReturn(GetCmdDescTranslated());

        private static int ShowTab(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            var snapshot = handler.GetTabListSnapshot();

            if (ConsoleIO.Backend is TuiConsoleBackend)
            {
                var view = TuiConsoleBackend.Instance?.GetView();
                if (view is null)
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_tab_tui_unavailable);

                Dispatcher.UIThread.Post(() => view.ShowOverlay(new TabListOverlay(handler)));
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_tab_tui_opened);
            }

            return r.SetAndReturn(CmdResult.Status.Done, TabListFormatter.Render(snapshot));
        }
    }
}
