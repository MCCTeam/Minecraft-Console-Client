using System.Collections.Generic;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Script : Command
    {
        public override string CmdName { get { return "script"; } }
        public override string CmdUsage { get { return "script <scriptname>"; } }
        public override string CmdDesc { get { return Translations.cmd_script_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("Script", MccArguments.ScriptName())
                    .Executes(r => DoExecuteScript(r.Source, Arguments.GetString(r, "Script"), null)))
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

        private int DoExecuteScript(CmdResult r, string command, Dictionary<string, object>? localVars)
        {
            McClient handler = CmdResult.currentHandler!;
            handler.BotLoad(new ChatBots.Script(command.Trim(), null, localVars));
            return r.SetAndReturn(CmdResult.Status.Done);
        }
    }
}
