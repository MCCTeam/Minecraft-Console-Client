using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    class Upgrade : Command
    {
        public override string CmdName { get { return "upgrade"; } }
        public override string CmdUsage { get { return "upgrade [-f|check|cancel|download]"; } }
        public override string CmdDesc { get { return string.Empty; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("cancel")
                        .Executes(r => GetUsage(r.Source, "cancel")))
                    .Then(l => l.Literal("check")
                        .Executes(r => GetUsage(r.Source, "check")))
                    .Then(l => l.Literal("download")
                        .Executes(r => GetUsage(r.Source, "download")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DownloadUpdate(r.Source, force: false))
                .Then(l => l.Literal("-f")
                    .Executes(r => DownloadUpdate(r.Source, force: true)))
                .Then(l => l.Literal("download")
                    .Executes(r => DownloadUpdate(r.Source, force: false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => DownloadUpdate(r.Source, force: true))))
                .Then(l => l.Literal("check")
                    .Executes(r => CheckUpdate(r.Source)))
                .Then(l => l.Literal("cancel")
                    .Executes(r => CancelDownloadUpdate(r.Source)))
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
                "cancel"    =>  GetCmdDescTranslated(),
                "check"     =>  GetCmdDescTranslated(),
                "download"  =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private static int DownloadUpdate(CmdResult r, bool force)
        {
            if (UpgradeHelper.DownloadLatestBuild(force))
                return r.SetAndReturn(CmdResult.Status.Done, Translations.mcc_update_start);
            else
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.mcc_update_already_running);
        }

        private static int CancelDownloadUpdate(CmdResult r)
        {
            UpgradeHelper.CancelDownloadUpdate();
            return r.SetAndReturn(CmdResult.Status.Done, Translations.mcc_update_cancel);
        }

        private static int CheckUpdate(CmdResult r)
        {
            UpgradeHelper.CheckUpdate(forceUpdate: true);
            return r.SetAndReturn(CmdResult.Status.Done, Translations.mcc_update_start);
        }
    }
}
