using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class Connect : Command
    {
        public override string CmdName { get { return "connect"; } }
        public override string CmdUsage { get { return "connect <server> [account]"; } }
        public override string CmdDesc { get { return Translations.cmd_connect_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("ServerNick", MccArguments.ServerNick())
                    .Executes(r => DoConnect(r.Source, Arguments.GetString(r, "ServerNick"), string.Empty))
                    .Then(l => l.Argument("AccountNick", MccArguments.AccountNick())
                        .Executes(r => DoConnect(r.Source, Arguments.GetString(r, "ServerNick"), Arguments.GetString(r, "AccountNick")))))
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

        private int DoConnect(CmdResult r, string server, string account)
        {
            if (!string.IsNullOrWhiteSpace(account) && !Settings.Config.Main.Advanced.SetAccount(account))
                return r.SetAndReturn(Status.Fail, string.Format(Translations.cmd_connect_unknown, account));

            if (Settings.Config.Main.SetServerIP(new Settings.MainConfigHealper.MainConfig.ServerInfoConfig(server), true))
            {
                Program.Restart(keepAccountAndServerSettings: true);
                return r.SetAndReturn(Status.Done);
            }
            else
            {
                return r.SetAndReturn(Status.Fail, string.Format(Translations.cmd_connect_invalid_ip, server));
            }
        }

        internal static string DoConnect(string command)
        {
            string[] args = GetArgs(command);
            if (args.Length > 1 && !Settings.Config.Main.Advanced.SetAccount(args[1]))
                return string.Format(Translations.cmd_connect_unknown, args[1]);

            if (Settings.Config.Main.SetServerIP(new Settings.MainConfigHealper.MainConfig.ServerInfoConfig(args[0]), true))
            {
                Program.Restart(keepAccountAndServerSettings: true);
                return string.Empty;
            }
            else
            {
                return string.Format(Translations.cmd_connect_invalid_ip, args[0]);
            }
        }
    }
}
