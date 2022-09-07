using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Reload : Command
    {
        public override string CmdName { get { return "reload"; } }
        public override string CmdUsage { get { return "reload"; } }
        public override string CmdDesc { get { return "cmd.reload.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            bool hard = false;

            if (hasArg(command))
            {
                string[] args = getArg(command).Split(' ');

                if (args.Length > 0)
                {
                    if (args[0].Equals("hard", StringComparison.OrdinalIgnoreCase))
                        hard = true;
                }
            }

            handler.Log.Info(Translations.TryGet("cmd.reload.started"));
            handler.ReloadSettings(hard);
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning1"));
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning2"));
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning3"));
            handler.Log.Warn(Translations.TryGet("cmd.reload.warning4"));

            return Translations.TryGet("cmd.reload.finished");
        }
    }
}
