using System;
using System.Collections.Generic;
using Brigadier.NET;
using Brigadier.NET.Builder;

namespace MinecraftClient.Commands
{
    public class Animation : Command
    {
        public override string CmdName { get { return "animation"; } }
        public override string CmdUsage { get { return "animation <mainhand|offhand>"; } }
        public override string CmdDesc { get { return "cmd.animation.desc"; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
            dispatcher.Register(l =>
                l.Literal("help").Then(l =>
                    l.Literal(CmdName).Executes(c => {
                        LogUsage(handler.Log);
                        return 1;
                    })
                )
            );

            dispatcher.Register(l =>
                l.Literal(CmdName).Then(l =>
                    l.Literal("mainhand")
                        .Executes(c => {
                            return LogExecuteResult(handler.Log, handler.DoAnimation(0));
                        })
                )
            );
            dispatcher.Register(l =>
                l.Literal(CmdName).Then(l =>
                    l.Literal("0") 
                        .Redirect(dispatcher.GetRoot().GetChild(CmdName).GetChild("mainhand"))
                )
            );

            dispatcher.Register(l =>
                l.Literal(CmdName).Then(l =>
                    l.Literal("offhand")
                        .Executes(c => {
                            return LogExecuteResult(handler.Log, handler.DoAnimation(1));
                        })
                )
            );
            dispatcher.Register(l =>
                l.Literal(CmdName).Then(l =>
                    l.Literal("1")
                        .Redirect(dispatcher.GetRoot().GetChild(CmdName).GetChild("offhand"))
                )
            );
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string[] args = GetArgs(command);
                if (args.Length > 0)
                {
                    if (args[0] == "mainhand" || args[0] == "0")
                    {
                        handler.DoAnimation(0);
                        return Translations.Get("general.done");
                    }
                    else if (args[0] == "offhand" || args[0] == "1")
                    {
                        handler.DoAnimation(1);
                        return Translations.Get("general.done");
                    }
                    else
                    {
                        return GetCmdDescTranslated();
                    }
                }
                else
                {
                    return GetCmdDescTranslated();
                }
            }
            else return GetCmdDescTranslated();
        }
    }
}
