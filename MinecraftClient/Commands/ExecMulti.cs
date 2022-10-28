using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecraftClient.Commands
{
    class ExecMulti : Command
    {
        public override string CmdName { get { return "execmulti"; } }
        public override string CmdUsage { get { return "execmulti <command 1> -> <command2> -> <command 3> -> ..."; } }
        public override string CmdDesc { get { return Translations.cmd_execmulti_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string commandsString = GetArg(command);

                if (commandsString.Contains("execmulti", StringComparison.OrdinalIgnoreCase) || commandsString.Contains("execif", StringComparison.OrdinalIgnoreCase))
                    return Translations.cmd_execmulti_prevent;

                IEnumerable<string> commands = commandsString.Split("->", StringSplitOptions.TrimEntries)
                    .ToList()
                    .FindAll(command => !string.IsNullOrEmpty(command));

                foreach (string cmd in commands)
                {
                    string? output = "";
                    handler.PerformInternalCommand(cmd, ref output);

                    string log = string.Format(
                        Translations.cmd_execmulti_executed, cmd,
                        string.IsNullOrEmpty(output) ? Translations.cmd_execmulti_no_result : string.Format(Translations.cmd_execmulti_result, output));

                    if (output != null && output.Contains("unknown command", StringComparison.OrdinalIgnoreCase))
                        handler.Log.Error(log);
                    else
                        handler.Log.Info(log);
                }

                return "";
            }

            return GetCmdDescTranslated();
        }
    }
}
