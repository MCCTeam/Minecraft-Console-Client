using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Teams : Command
    {
        public override string CmdName => "teams";
        public override string CmdUsage => "teams";
        public override string CmdDesc => Translations.cmd_teams_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoListTeams(r.Source))
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
                _ => GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private static int DoListTeams(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            Dictionary<string, PlayerTeam> snapshot = handler.GetTeams();

            if (snapshot.Count == 0)
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_teams_no_teams);

            var sb = new StringBuilder();
            foreach (var team in snapshot.Values.OrderBy(static t => t.Name, StringComparer.Ordinal))
            {
                sb.AppendLine(string.Format(Translations.cmd_teams_team_header,
                    team.Name,
                    team.DisplayName,
                    team.Color,
                    team.Prefix,
                    team.Suffix,
                    team.NameTagVisibility,
                    team.CollisionRule,
                    team.AllowFriendlyFire,
                    team.SeeFriendlyInvisibles));

                if (team.Members.Count == 0)
                    sb.AppendLine(Translations.cmd_teams_team_no_members);
                else
                    sb.AppendLine(string.Format(Translations.cmd_teams_team_members,
                        team.Members.Count,
                        string.Join(", ", team.Members.OrderBy(static m => m, StringComparer.OrdinalIgnoreCase))));
            }

            return r.SetAndReturn(CmdResult.Status.Done, sb.ToString().TrimEnd());
        }
    }
}
