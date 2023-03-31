using System;
using System.Collections.Generic;
using Brigadier.NET;
using Brigadier.NET.Builder;
using DynamicExpresso;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    class ExecIf : Command
    {
        public override string CmdName { get { return "execif"; } }
        public override string CmdUsage { get { return "execif \"<condition/expression>\" \"<command>\""; } }
        public override string CmdDesc { get { return Translations.cmd_execif_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("Condition", Arguments.String())
                    .Then(l => l.Argument("Command", Arguments.String())
                        .Executes(r => HandleCommand(r.Source, Arguments.GetString(r, "Condition"), Arguments.GetString(r, "Command")))))
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

        private int HandleCommand(CmdResult r, string expressionText, string resultCommand)
        {
            McClient handler = CmdResult.currentHandler!;
            try
            {
                var interpreter = new Interpreter();
                interpreter.SetVariable("MCC", handler);
                interpreter.SetVariable("random", new Random());

                foreach (KeyValuePair<string, object> entry in Settings.Config.AppVar.GetVariables())
                    interpreter.SetVariable(entry.Key, entry.Value);

                var result = interpreter.Eval<bool>(expressionText);
                handler.Log.Debug("[Execif] Result Type: " + result.GetType().Name);

                if (result)
                {
                    CmdResult output = new();
                    handler.PerformInternalCommand(resultCommand, ref output);

                    return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_execif_executed, expressionText, resultCommand, output));
                }
                else
                {
                    return r.SetAndReturn(CmdResult.Status.Done);
                }
            }
            catch (Exception e)
            {
                handler.Log.Error(string.Format(Translations.cmd_execif_error, e.Message));
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_execif_error_occured, expressionText + " ---> " + resultCommand));
            }
        }
    }
}
