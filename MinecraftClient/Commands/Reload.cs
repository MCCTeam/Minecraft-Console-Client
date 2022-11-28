using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class Reload : Command
    {
        public override string CmdName { get { return "reload"; } }
        public override string CmdUsage { get { return "reload"; } }
        public override string CmdDesc { get { return Translations.cmd_reload_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            handler.Log.Info(Translations.cmd_reload_started);
            handler.ReloadSettings();
            handler.Log.Warn(Translations.cmd_reload_warning1);
            handler.Log.Warn(Translations.cmd_reload_warning2);
            handler.Log.Warn(Translations.cmd_reload_warning3);
            handler.Log.Warn(Translations.cmd_reload_warning4);

            return Translations.cmd_reload_finished;
        }
    }
}
