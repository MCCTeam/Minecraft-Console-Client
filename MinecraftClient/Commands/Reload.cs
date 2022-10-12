using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class Reload : Command
    {
        public override string CmdName { get { return "reload"; } }
        public override string CmdUsage { get { return "reload"; } }
        public override string CmdDesc { get { return "cmd.reload.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            handler.Log.Info(Translations.TryGet("cmd.reload.started"));
            handler.ReloadSettings();
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning1"));
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning2"));
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning3"));
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning4"));

            return Translations.TryGet("cmd.reload.finished");
        }
    }
}
