using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class ConsoleChat : Command
    {
        public override string CmdName => "console-chat";
        public override string CmdUsage => "console-chat [on|off]";
        public override string CmdDesc => Translations.cmd_console_chat_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => SetChatVisibility(r.Source, null))
                .Then(l => l.Literal("on")
                    .Executes(r => SetChatVisibility(r.Source, true)))
                .Then(l => l.Literal("off")
                    .Executes(r => SetChatVisibility(r.Source, false)))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult result)
        {
            return result.SetAndReturn(GetCmdDescTranslated());
        }

        private int SetChatVisibility(CmdResult result, bool? visible)
        {
            ConsoleIO.ChatVisible = visible ?? !ConsoleIO.ChatVisible;

            return result.SetAndReturn(
                CmdResult.Status.Done,
                ConsoleIO.ChatVisible
                    ? Translations.cmd_console_chat_state_on
                    : Translations.cmd_console_chat_state_off);
        }
    }
}
