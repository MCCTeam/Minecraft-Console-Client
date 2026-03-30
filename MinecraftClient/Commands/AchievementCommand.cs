using System.Linq;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class AchievementCommand : Command
    {
        public override string CmdName => "achievement";
        public override string CmdUsage => "achievement <list|locked|unlocked>";
        public override string CmdDesc => Translations.cmd_achievement_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("list")
                        .Executes(r => GetUsage(r.Source, "list")))
                    .Then(l => l.Literal("locked")
                        .Executes(r => GetUsage(r.Source, "locked")))
                    .Then(l => l.Literal("unlocked")
                        .Executes(r => GetUsage(r.Source, "unlocked")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => ListAchievements(r.Source, null))
                .Then(l => l.Literal("list")
                    .Executes(r => ListAchievements(r.Source, null)))
                .Then(l => l.Literal("locked")
                    .Executes(r => ListAchievements(r.Source, false)))
                .Then(l => l.Literal("unlocked")
                    .Executes(r => ListAchievements(r.Source, true)))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format
                "list"      => GetCmdDescTranslated(),
                "locked"    => GetCmdDescTranslated(),
                "unlocked"  => GetCmdDescTranslated(),
                _           => GetCmdDescTranslated(),
#pragma warning restore format
            });
        }

        /// <param name="completed">null = all, true = unlocked only, false = locked only</param>
        private static int ListAchievements(CmdResult r, bool? completed)
        {
            McClient handler = CmdResult.currentHandler!;

            Achievement[] items = completed switch
            {
                true => handler.GetUnlockedAchievements(),
                false => handler.GetLockedAchievements(),
                null => handler.GetAchievements()
            };

            if (items.Length == 0)
            {
                string msg = completed switch
                {
                    true => Translations.cmd_achievement_none_unlocked,
                    false => Translations.cmd_achievement_none_locked,
                    _ => Translations.cmd_achievement_none
                };
                return r.SetAndReturn(CmdResult.Status.Done, msg);
            }

            string header = completed switch
            {
                true => Translations.cmd_achievement_header_unlocked,
                false => Translations.cmd_achievement_header_locked,
                _ => Translations.cmd_achievement_header
            };

            StringBuilder sb = new();
            sb.AppendLine(header);

            foreach (Achievement a in items.OrderBy(static a => a.Id))
            {
                string status = a.IsCompleted
                    ? Translations.cmd_achievement_done
                    : Translations.cmd_achievement_todo;

                string display = a.Title is not null
                    ? string.Format(Translations.cmd_achievement_entry_titled, status, a.Title, a.Id, a.Type)
                    : string.Format(Translations.cmd_achievement_entry, status, a.Id, a.Type);

                sb.AppendLine(display);
            }

            handler.Log.Info(sb.ToString().TrimEnd());
            return r.SetAndReturn(CmdResult.Status.Done);
        }
    }
}
