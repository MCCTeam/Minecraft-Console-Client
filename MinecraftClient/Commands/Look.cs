using System;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class Look : Command
    {
        public override string CmdName { get { return "look"; } }
        public override string CmdUsage { get { return "look <x y z|yaw pitch|up|down|east|west|north|south>"; } }
        public override string CmdDesc { get { return Translations.cmd_look_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("direction")
                        .Executes(r => GetUsage(r.Source, "direction")))
                    .Then(l => l.Literal("angle")
                        .Executes(r => GetUsage(r.Source, "angle")))
                    .Then(l => l.Literal("location")
                        .Executes(r => GetUsage(r.Source, "location")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => LogCurrentLooking(r.Source))
                .Then(l => l.Literal("up")
                    .Executes(r => LookAtDirection(r.Source, Direction.Up)))
                .Then(l => l.Literal("down")
                    .Executes(r => LookAtDirection(r.Source, Direction.Down)))
                .Then(l => l.Literal("east")
                    .Executes(r => LookAtDirection(r.Source, Direction.East)))
                .Then(l => l.Literal("west")
                    .Executes(r => LookAtDirection(r.Source, Direction.West)))
                .Then(l => l.Literal("north")
                    .Executes(r => LookAtDirection(r.Source, Direction.North)))
                .Then(l => l.Literal("south")
                    .Executes(r => LookAtDirection(r.Source, Direction.South)))
                .Then(l => l.Argument("Yaw", Arguments.Float())
                    .Then(l => l.Argument("Pitch", Arguments.Float())
                        .Executes(r => LookAtAngle(r.Source, Arguments.GetFloat(r, "Yaw"), Arguments.GetFloat(r, "Pitch")))))
                .Then(l => l.Argument("Location", MccArguments.Location())
                    .Executes(r => LookAtLocation(r.Source, MccArguments.GetLocation(r, "Location"))))
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
                "direction"  =>  GetCmdDescTranslated(),
                "angle"      =>  GetCmdDescTranslated(),
                "location"   =>  GetCmdDescTranslated(),
                _            =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int LogCurrentLooking(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            const double maxDistance = 8.0;
            (bool hasBlock, Location target, Block block) = RaycastHelper.RaycastBlock(handler, maxDistance, false);
            if (!hasBlock)
            {
                return r.SetAndReturn(Status.Fail, string.Format(Translations.cmd_look_noinspection, maxDistance));
            }
            else
            {
                Location current = handler.GetCurrentLocation(), target_center = target.ToCenter();
                return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_look_inspection, block.Type, target.X, target.Y, target.Z,
                    current.Distance(target_center), current.EyesLocation().Distance(target_center)));
            }
        }

        private int LookAtDirection(CmdResult r, Direction direction)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            handler.UpdateLocation(handler.GetCurrentLocation(), direction);
            return r.SetAndReturn(Status.Done, "Looking " + direction.ToString());
        }

        private int LookAtAngle(CmdResult r, float yaw, float pitch)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            handler.UpdateLocation(handler.GetCurrentLocation(), yaw, pitch);
            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_look_at, yaw.ToString("0.00"), pitch.ToString("0.00")));
        }

        private int LookAtLocation(CmdResult r, Location location)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location current = handler.GetCurrentLocation();
            handler.UpdateLocation(current, location);
            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_look_block, location));
        }
    }
}
