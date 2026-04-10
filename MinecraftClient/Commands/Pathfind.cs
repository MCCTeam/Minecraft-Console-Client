using System;
using System.Threading;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Goals;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class Pathfind : Command
    {
        public override string CmdName => "pathfind";
        public override string CmdUsage => "pathfind <x y z>";
        public override string CmdDesc => Translations.cmd_pathfind_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source)))
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("location", MccArguments.Location())
                    .Executes(r => DoPathfind(r.Source, MccArguments.GetLocation(r, "location"))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r)
        {
            return r.SetAndReturn(GetCmdDescTranslated());
        }

        private int DoPathfind(CmdResult r, Location goal)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location current = handler.GetCurrentLocation();
            goal.ToAbsolute(current);

            int startX = (int)Math.Floor(current.X);
            int startY = (int)Math.Floor(current.Y);
            int startZ = (int)Math.Floor(current.Z);
            int goalX = (int)Math.Floor(goal.X);
            int goalY = (int)Math.Floor(goal.Y);
            int goalZ = (int)Math.Floor(goal.Z);

            handler.Log.Info($"[Pathfind] Planning from ({startX},{startY},{startZ}) to ({goalX},{goalY},{goalZ})");

            var ctx = new CalculationContext(
                handler.GetWorld(),
                canSprint: true,
                maxFallHeight: 3);

            var finder = new AStarPathFinder();
            finder.DebugLog = msg => handler.Log.Info(msg);

            var goalObj = new GoalBlock(goalX, goalY, goalZ);

            Task.Run(() =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var result = finder.Calculate(ctx, startX, startY, startZ, goalObj, cts.Token, timeoutMs: 10000);

                handler.Log.Info($"[Pathfind] Result: {result.Status}, {result.Path.Count} nodes, " +
                    $"{result.NodesExplored} explored, {result.ElapsedMs}ms");

                if (result.Path.Count > 0)
                {
                    handler.Log.Info("[Pathfind] Path waypoints:");
                    for (int i = 0; i < result.Path.Count; i++)
                    {
                        var n = result.Path[i];
                        handler.Log.Info($"  [{i}] ({n.X},{n.Y},{n.Z}) via {n.MoveUsed}");
                    }

                    handler.Log.Info("[Pathfind] Beginning movement along path...");
                    FollowPath(handler, result);
                }
                else
                {
                    handler.Log.Warn("[Pathfind] No path found!");
                }
            });

            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_pathfind_started, goalX, goalY, goalZ));
        }

        private static void FollowPath(McClient handler, PathResult result)
        {
            for (int i = 1; i < result.Path.Count; i++)
            {
                var node = result.Path[i];
                var target = new Location(node.X + 0.5, node.Y, node.Z + 0.5);

                handler.Log.Info($"[Pathfind] Moving to waypoint [{i}]: ({node.X},{node.Y},{node.Z}) via {node.MoveUsed}");

                bool success = handler.MoveTo(target, allowUnsafe: true, allowDirectTeleport: false, timeout: TimeSpan.FromSeconds(10));
                if (!success)
                {
                    handler.Log.Warn($"[Pathfind] Old pathfinder failed to plan sub-path to ({node.X},{node.Y},{node.Z}), trying direct teleport");
                    handler.MoveTo(target, allowUnsafe: true, allowDirectTeleport: true);
                }

                int maxWaitTicks = 200;
                int waited = 0;
                while (handler.ClientIsMoving() && waited < maxWaitTicks)
                {
                    Thread.Sleep(50);
                    waited++;
                }

                var cur = handler.GetCurrentLocation();
                double dx = cur.X - target.X;
                double dz = cur.Z - target.Z;
                double horizDistSq = dx * dx + dz * dz;

                handler.Log.Info($"[Pathfind] Arrived near waypoint [{i}], pos=({cur.X:F2},{cur.Y:F2},{cur.Z:F2}), horizDist={Math.Sqrt(horizDistSq):F2}");
            }

            handler.Log.Info("[Pathfind] Path execution complete!");
        }
    }
}
