using System;
using System.Collections.Generic;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    class SetRnd : Command
    {
        public override string CmdName { get { return "setrnd"; } }
        public override string CmdUsage { get { return Translations.cmd_setrnd_format; } }
        public override string CmdDesc { get { return Translations.cmd_setrnd_desc; } }
        private static readonly Random rand = new();

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("range")
                        .Executes(r => GetUsage(r.Source, "range")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("VarName", Arguments.String())
                    .Then(l => l.Argument("Min", Arguments.Long())
                        .Then(l => l.Literal("to")
                            .Then(l => l.Argument("Max", Arguments.Long())
                                .Executes(r => DoSetRnd(r.Source, Arguments.GetString(r, "VarName"), Arguments.GetLong(r, "Min"), Arguments.GetLong(r, "Max"))))))
                    .Then(l => l.Argument("Expression", Arguments.GreedyString())
                        .Executes(r => DoSetRnd(r.Source, Arguments.GetString(r, "VarName"), Arguments.GetString(r, "Expression")))))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "range"     =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int DoSetRnd(CmdResult r, string var, string argString)
        {
            // process all arguments similar to regular terminals with quotes and escaping
            List<string> values = ParseCommandLine(argString);

            // create a variable or set it to one of the values
            if (values.Count > 0 && Settings.Config.AppVar.SetVar(var, values[rand.Next(0, values.Count)]))
                return r.SetAndReturn(CmdResult.Status.Done, string.Format("Set %{0}% to {1}.", var, Settings.Config.AppVar.GetVar(var)));
            else
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_setrndstr_format);
        }

        private int DoSetRnd(CmdResult r, string var, long min, long max)
        {
            // switch the values if they were entered in the wrong way
            if (max < min)
                (max, min) = (min, max);

            // create a variable or set it to num1 <= varlue < num2
            if (Settings.Config.AppVar.SetVar(var, rand.NextInt64(min, max)))
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format("Set %{0}% to {1}.", var, Settings.Config.AppVar.GetVar(var)));
            else
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_setrndstr_format);
        }
    }
}
