using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    class Reload : Command
    {
        public override string CmdName { get { return "reload"; } }
        public override string CmdUsage { get { return "reload"; } }
        public override string CmdDesc { get { return Translations.cmd_reload_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoReload(r.Source))
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

        private int DoReload(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            handler.Log.Info(Translations.cmd_reload_started);
            handler.ReloadSettings();
            handler.Log.Warn(Translations.cmd_reload_warning1);
            handler.Log.Warn(Translations.cmd_reload_warning2);
            handler.Log.Warn(Translations.cmd_reload_warning3);
            handler.Log.Warn(Translations.cmd_reload_warning4);

            return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_reload_finished);
        }
    }
}
