using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class Upgrade : Command
    {
        public override string CmdName { get { return "upgrade"; } }
        public override string CmdUsage { get { return "upgrade [-f|check|cancel|download]"; } }
        public override string CmdDesc { get { return string.Empty; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string[] args = GetArgs(command);
                return args[0] switch
                {
                    "-f" => DownloadUpdate(force: true),
                    "-force" => DownloadUpdate(force: true),
                    "cancel" => CancelDownloadUpdate(),
                    "check" => CheckUpdate(),
                    "download" => DownloadUpdate(force: args.Length > 1 && (args[1] == "-f" || args[1] == "-force")),
                    _ => GetCmdDescTranslated(),
                };
            }
            else
            {
                return DownloadUpdate(force: false);
            }
        }

        private static string DownloadUpdate(bool force)
        {
            UpgradeHelper.DownloadLatestBuild(force);
            return Translations.mcc_update_start;
        }

        private static string CancelDownloadUpdate()
        {
            UpgradeHelper.CancelDownloadUpdate();
            return Translations.mcc_update_cancel;
        }

        private static string CheckUpdate()
        {
            UpgradeHelper.CheckUpdate(forceUpdate: true);
            return Translations.mcc_update_start;
        }
    }
}
