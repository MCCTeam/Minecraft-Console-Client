using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class Goto : Command
    {
        public override string CmdName => "goto";
        public override string CmdUsage => "goto <x y z>";
        public override string CmdDesc => Translations.cmd_goto_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty)))
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("location", MccArguments.Location())
                    .Executes(r => DoGoto(r.Source, MccArguments.GetLocation(r, "location"))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(GetCmdDescTranslated());
        }

        private static int DoGoto(CmdResult r, Location goal)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location current = handler.GetCurrentLocation();
            goal.ToAbsolute(current);

            var (success, message) = handler.MoveToAStar(goal);

            return r.SetAndReturn(success ? Status.Done : Status.Fail, message);
        }
    }
}
