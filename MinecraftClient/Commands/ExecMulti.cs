using System;
using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    class ExecMulti : Command
    {
        public override string CmdName { get { return "execmulti"; } }
        public override string CmdUsage { get { return "execmulti <command 1> -> <command2> -> <command 3> -> ..."; } }
        public override string CmdDesc { get { return Translations.cmd_execmulti_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("Commands", Arguments.GreedyString())
                    .Executes(r => HandleCommand(r.Source, Arguments.GetString(r, "Commands"))))
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

        private int HandleCommand(CmdResult r, string commandsString)
        {
            McClient handler = CmdResult.currentHandler!;
            if (commandsString.Contains("execmulti", StringComparison.OrdinalIgnoreCase) || commandsString.Contains("execif", StringComparison.OrdinalIgnoreCase))
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_execmulti_prevent);

            IEnumerable<string> commands = commandsString.Split("->", StringSplitOptions.TrimEntries)
                .ToList()
                .FindAll(command => !string.IsNullOrEmpty(command));

            foreach (string cmd in commands)
            {
                CmdResult output = new();
                handler.PerformInternalCommand(cmd, ref output);
                handler.Log.Info(string.Format(Translations.cmd_execmulti_executed, cmd, string.Format(Translations.cmd_execmulti_result, output)));
            }

            return r.SetAndReturn(CmdResult.Status.Done);
        }
    }
}
