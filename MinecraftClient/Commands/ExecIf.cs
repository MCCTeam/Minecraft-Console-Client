using System;
using System.Collections.Generic;
using System.Linq;
using DynamicExpresso;

namespace MinecraftClient.Commands
{
    class ExecIf : Command
    {
        public override string CmdName { get { return "execif"; } }
        public override string CmdUsage { get { return "execif <condition/expression> ---> <command>"; } }
        public override string CmdDesc { get { return "cmd.execif.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string commandsString = GetArg(command);

                if (!commandsString.Contains("--->"))
                    return GetCmdDescTranslated();

                string[] parts = commandsString.Split("--->", StringSplitOptions.TrimEntries)
                    .ToList()
                    .ConvertAll(command => command.Trim())
                    .ToArray();

                if (parts.Length == 0)
                    return GetCmdDescTranslated();

                string expressionText = parts[0];
                string resultCommand = parts[1];

                try
                {
                    var interpreter = new Interpreter();
                    interpreter.SetVariable("MCC", handler);

                    foreach (KeyValuePair<string, object> entry in Settings.Config.AppVar.GetVariables())
                        interpreter.SetVariable(entry.Key, entry.Value);

                    var result = interpreter.Eval<bool>(expressionText);

                    bool shouldExec = result;

                    /*if (result is bool)
                        shouldExec = (bool)result;
                    else if (result is string)
                        shouldExec = !string.IsNullOrEmpty((string)result) && ((string)result).Trim().Contains("true", StringComparison.OrdinalIgnoreCase);
                    else if (result is int)
                        shouldExec = (int)result > 0;
                    else if (result is double)
                        shouldExec = (double)result > 0;
                    else if (result is float)
                        shouldExec = (float)result > 0;
                    else if (result is Int16)
                        shouldExec = (Int16)result > 0;
                    else if (result is Int32)
                        shouldExec = (Int32)result > 0;
                    else if (result is Int64)
                        shouldExec = (Int64)result > 0;
                    */

                    handler.Log.Debug("[Execif] Result Type: " + result.GetType().Name);

                    if (shouldExec)
                    {
                        string? output = "";
                        handler.PerformInternalCommand(resultCommand, ref output);

                        if (string.IsNullOrEmpty(output))
                            handler.Log.Debug(Translations.TryGet("cmd.execif.executed_no_output", expressionText, resultCommand));
                        else handler.Log.Debug(Translations.TryGet("cmd.execif.executed", expressionText, resultCommand, output));

                        return "";
                    }

                    return "";
                }
                catch (Exception e)
                {
                    handler.Log.Error(Translations.TryGet("cmd.execif.error_occured", command));
                    handler.Log.Error(Translations.TryGet("cmd.execif.error", e.Message));
                    return "";
                }
            }

            return GetCmdDescTranslated();
        }


    }
}
