using System;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    internal class Help : Command
    {
        public override string CmdName => throw new NotImplementedException();
        public override string CmdDesc => throw new NotImplementedException();
        public override string CmdUsage => throw new NotImplementedException();

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l =>
                l.Literal("help")
                    .Executes(r => LogHelp(r.Source, dispatcher))
            );
        }

        private int LogHelp(CmdResult r, CommandDispatcher<CmdResult> dispatcher)
        {
            McClient handler = CmdResult.currentHandler!;
            var usage = dispatcher.GetSmartUsage(dispatcher.GetRoot(), CmdResult.Empty);
            StringBuilder sb = new();
            foreach (var item in usage)
                sb.AppendLine(Settings.Config.Main.Advanced.InternalCmdChar.ToChar() + item.Value);
            handler.Log.Info(string.Format(Translations.icmd_list, sb.ToString(), Settings.Config.Main.Advanced.InternalCmdChar.ToChar()));
            return r.SetAndReturn(CmdResult.Status.Done);
        }
    }
}
