using System;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class Move : Command
    {
        public override string CmdName { get { return "move"; } }
        public override string CmdUsage { get { return "move <on|off|get|up|down|east|west|north|south|center|x y z|gravity [on|off]> [-f]"; } }
        public override string CmdDesc { get { return Translations.cmd_move_desc + " \"-f\": " + Translations.cmd_move_desc_force; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("enable")
                        .Executes(r => GetUsage(r.Source, "enable")))
                    .Then(l => l.Literal("gravity")
                        .Executes(r => GetUsage(r.Source, "gravity")))
                    .Then(l => l.Literal("direction")
                        .Executes(r => GetUsage(r.Source, "direction")))
                    .Then(l => l.Literal("center")
                        .Executes(r => GetUsage(r.Source, "center")))
                    .Then(l => l.Literal("get")
                        .Executes(r => GetUsage(r.Source, "get")))
                    .Then(l => l.Literal("location")
                        .Executes(r => GetUsage(r.Source, "location")))
                    .Then(l => l.Literal("-f")
                        .Executes(r => GetUsage(r.Source, "-f")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Literal("on")
                    .Executes(r => SetMovementEnable(r.Source, enable: true)))
                .Then(l => l.Literal("off")
                    .Executes(r => SetMovementEnable(r.Source, enable: false)))
                .Then(l => l.Literal("gravity")
                    .Executes(r => SetGravityEnable(r.Source, enable: null))
                    .Then(l => l.Literal("on")
                        .Executes(r => SetGravityEnable(r.Source, enable: true)))
                    .Then(l => l.Literal("off")
                        .Executes(r => SetGravityEnable(r.Source, enable: false))))
                .Then(l => l.Literal("up")
                    .Executes(r => MoveOnDirection(r.Source, Direction.Up, false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => MoveOnDirection(r.Source, Direction.Up, true))))
                .Then(l => l.Literal("down")
                    .Executes(r => MoveOnDirection(r.Source, Direction.Down, false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => MoveOnDirection(r.Source, Direction.Down, true))))
                .Then(l => l.Literal("east")
                    .Executes(r => MoveOnDirection(r.Source, Direction.East, false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => MoveOnDirection(r.Source, Direction.East, true))))
                .Then(l => l.Literal("west")
                    .Executes(r => MoveOnDirection(r.Source, Direction.West, false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => MoveOnDirection(r.Source, Direction.West, true))))
                .Then(l => l.Literal("north")
                    .Executes(r => MoveOnDirection(r.Source, Direction.North, false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => MoveOnDirection(r.Source, Direction.North, true))))
                .Then(l => l.Literal("south")
                    .Executes(r => MoveOnDirection(r.Source, Direction.South, false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => MoveOnDirection(r.Source, Direction.South, true))))
                .Then(l => l.Literal("center")
                    .Executes(r => MoveToCenter(r.Source)))
                .Then(l => l.Literal("get")
                    .Executes(r => GetCurrentLocation(r.Source)))
                .Then(l => l.Argument("location", MccArguments.Location())
                    .Executes(r => MoveToLocation(r.Source, MccArguments.GetLocation(r, "location"), false))
                    .Then(l => l.Literal("-f")
                        .Executes(r => MoveToLocation(r.Source, MccArguments.GetLocation(r, "location"), true))))
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
                "enable"    =>  GetCmdDescTranslated(),
                "gravity"   =>  GetCmdDescTranslated(),
                "direction" =>  GetCmdDescTranslated(),
                "center"    =>  GetCmdDescTranslated(),
                "get"       =>  GetCmdDescTranslated(),
                "location"  =>  GetCmdDescTranslated(),
                "-f"        =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int SetMovementEnable(CmdResult r, bool enable)
        {
            McClient handler = CmdResult.currentHandler!;
            if (enable)
            {
                handler.SetTerrainEnabled(true);
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_move_enable);
            }
            else
            {
                handler.SetTerrainEnabled(false);
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_move_disable);
            }
        }

        private int SetGravityEnable(CmdResult r, bool? enable)
        {
            McClient handler = CmdResult.currentHandler!;
            if (enable.HasValue)
                Settings.InternalConfig.GravityEnabled = enable.Value;

            if (Settings.InternalConfig.GravityEnabled)
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_move_gravity_enabled);
            else
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_move_gravity_disabled);
        }

        private int GetCurrentLocation(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            return r.SetAndReturn(Status.Done, handler.GetCurrentLocation().ToString());
        }

        private int MoveToCenter(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location current = handler.GetCurrentLocation();
            Location currentCenter = new(Math.Floor(current.X) + 0.5, current.Y, Math.Floor(current.Z) + 0.5);
            handler.MoveTo(currentCenter, allowDirectTeleport: true);
            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_move_walk, currentCenter, current));
        }

        private int MoveOnDirection(CmdResult r, Direction direction, bool takeRisk)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location goal = Movement.Move(handler.GetCurrentLocation(), direction);

            if (!Movement.CheckChunkLoading(handler.GetWorld(), handler.GetCurrentLocation(), goal))
                return r.SetAndReturn(Status.FailChunkNotLoad, string.Format(Translations.cmd_move_chunk_not_loaded, goal.X, goal.Y, goal.Z));

            if (Movement.CanMove(handler.GetWorld(), handler.GetCurrentLocation(), direction))
            {
                if (handler.MoveTo(goal, allowUnsafe: takeRisk))
                    return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_move_moving, direction.ToString()));
                else
                    return r.SetAndReturn(Status.Fail, takeRisk ? Translations.cmd_move_dir_fail : Translations.cmd_move_suggestforce);
            }
            else
            {
                return r.SetAndReturn(Status.Fail, Translations.cmd_move_dir_fail);
            }
        }

        private int MoveToLocation(CmdResult r, Location goal, bool takeRisk)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location current = handler.GetCurrentLocation(), currentCenter = current.ToCenter();
            goal.ToAbsolute(current);

            if (!Movement.CheckChunkLoading(handler.GetWorld(), current, goal))
                return r.SetAndReturn(Status.FailChunkNotLoad, string.Format(Translations.cmd_move_chunk_not_loaded, goal.X, goal.Y, goal.Z));

            if (takeRisk || Movement.PlayerFitsHere(handler.GetWorld(), goal))
            {
                if (current.ToFloor() == goal.ToFloor())
                    handler.MoveTo(goal, allowDirectTeleport: true);
                else if (!handler.MoveTo(goal, allowUnsafe: takeRisk))
                    return r.SetAndReturn(Status.Fail, takeRisk ? string.Format(Translations.cmd_move_fail, goal) : string.Format(Translations.cmd_move_suggestforce, goal));
                return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_move_walk, goal, current));
            }
            else
            {
                return r.SetAndReturn(Status.Fail, string.Format(Translations.cmd_move_suggestforce, goal));
            }
        }
    }
}
