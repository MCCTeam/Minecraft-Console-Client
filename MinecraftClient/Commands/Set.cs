using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Set : Command
    {
        public override string CmdName { get { return "set"; } }
        public override string CmdUsage { get { return "set varname=value"; } }
        public override string CmdDesc { get { return Translations.cmd_set_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("Expression", Arguments.GreedyString())
                    .Executes(r => DoSetVar(r.Source, Arguments.GetString(r, "Expression"))))
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

        private int DoSetVar(CmdResult r, string command)
        {
            string[] temp = command.Trim().Split('=');
            if (temp.Length > 1)
            {
                if (Settings.Config.AppVar.SetVar(temp[0], command[(temp[0].Length + 1)..]))
                {
                    return r.SetAndReturn(CmdResult.Status.Done); //Success
                }
                else
                {
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_set_format);
                }
            }
            else
            {
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_set_format);
            }
        }
    }
}
