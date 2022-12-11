using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class Debug : Command
    {
        public override string CmdName { get { return "debug"; } }
        public override string CmdUsage { get { return "debug [on|off]"; } }
        public override string CmdDesc { get { return Translations.cmd_debug_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => SetDebugMode(r.Source, true))
                .Then(l => l.Literal("on")
                    .Executes(r => SetDebugMode(r.Source, false, true)))
                .Then(l => l.Literal("off")
                    .Executes(r => SetDebugMode(r.Source, false, false)))
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

        private int SetDebugMode(CmdResult r, bool flip, bool mode = false)
        {
            if (flip)
                Settings.Config.Logging.DebugMessages = !Settings.Config.Logging.DebugMessages;
            else
                Settings.Config.Logging.DebugMessages = mode;

            if (Settings.Config.Logging.DebugMessages)
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_debug_state_on);
            else
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_debug_state_off);
        }
    }
}
