using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class BedCommand : Command
    {
        public override string CmdName { get { return "bed"; } }
        public override string CmdUsage { get { return "bed leave|sleep <x> <y> <z>|sleep <radius>"; } }
        public override string CmdDesc { get { return Translations.cmd_bed_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("leave")
                        .Executes(r => GetUsage(r.Source, "leave")))
                    .Then(l => l.Literal("sleep")
                        .Executes(r => GetUsage(r.Source, "sleep")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Literal("leave")
                    .Executes(r => DoLeaveBed(r.Source)))
                .Then(l => l.Literal("sleep")
                    .Then(l => l.Argument("Location", MccArguments.Location())
                        .Executes(r => DoSleepBedWithLocation(r.Source, MccArguments.GetLocation(r, "Location"))))
                    .Then(l => l.Argument("Radius", Arguments.Double())
                        .Executes(r => DoSleepBedWithRadius(r.Source, Arguments.GetDouble(r, "Radius")))))
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
                "leave"     =>  GetCmdDescTranslated(),
                "sleep"     =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private static int DoLeaveBed(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            return r.SetAndReturn(Translations.cmd_bed_leaving, handler.SendEntityAction(Protocol.EntityActionType.LeaveBed));
        }

        private static int DoSleepBedWithRadius(CmdResult r, double radius)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            handler.Log.Info(string.Format(Translations.cmd_bed_searching, radius));

            Location current = handler.GetCurrentLocation();
            Location bedLocation = current;

            Material[] bedMaterialList = new Material[]
            {
                Material.BlackBed,
                Material.BlueBed,
                Material.BrownBed,
                Material.CyanBed,
                Material.GrayBed,
                Material.GreenBed,
                Material.LightBlueBed,
                Material.LightGrayBed,
                Material.LimeBed,
                Material.MagentaBed,
                Material.OrangeBed,
                Material.PinkBed,
                Material.PurpleBed,
                Material.RedBed,
                Material.WhiteBed,
                Material.YellowBed
            };

            bool found = false;
            foreach (Material material in bedMaterialList)
            {
                List<Location> beds = handler.GetWorld().FindBlock(current, material, radius);

                if (beds.Count > 0)
                {
                    found = true;
                    bedLocation = beds.First();
                    break;
                }
            }

            if (!found)
                return r.SetAndReturn(Status.Fail, Translations.cmd_bed_bed_not_found);

            handler.Log.Info(string.Format(Translations.cmd_bed_found_a_bed_at, bedLocation.X, bedLocation.Y, bedLocation.Z));

            if (!Movement.CheckChunkLoading(handler.GetWorld(), current, bedLocation))
                return r.SetAndReturn(Status.FailChunkNotLoad,
                    string.Format(Translations.cmd_move_chunk_not_loaded, bedLocation.X, bedLocation.Y, bedLocation.Z));

            if (handler.MoveTo(bedLocation))
            {
                Task.Factory.StartNew(() =>
                {
                    bool atTheLocation = false;
                    DateTime timeout = DateTime.Now.AddSeconds(60);

                    while (DateTime.Now < timeout)
                    {
                        if (handler.GetCurrentLocation() == bedLocation || handler.GetCurrentLocation().Distance(bedLocation) <= 2.0)
                        {
                            atTheLocation = true;
                            break;
                        }
                    }

                    if (!atTheLocation)
                    {
                        handler.Log.Info(string.Format(Translations.cmd_bed_failed_to_reach_in_time, bedLocation.X, bedLocation.Y, bedLocation.Z));
                        return;
                    }

                    handler.Log.Info(string.Format(Translations.cmd_bed_moving, bedLocation.X, bedLocation.Y, bedLocation.Z));

                    bool res = handler.PlaceBlock(bedLocation, Direction.Down);

                    handler.Log.Info(string.Format(
                        Translations.cmd_bed_trying_to_use,
                        bedLocation.X,
                        bedLocation.Y,
                        bedLocation.Z,
                        res ? Translations.cmd_bed_in : Translations.cmd_bed_not_in
                    ));
                });

                return r.SetAndReturn(Status.Done);
            }
            else
            {
                return r.SetAndReturn(Status.Fail, Translations.cmd_bed_cant_reach_safely);
            }
        }

        private static int DoSleepBedWithLocation(CmdResult r, Location block)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            block.ToAbsolute(handler.GetCurrentLocation());
            Location blockCenter = block.ToCenter();

            if (!handler.GetWorld().GetBlock(block).Type.IsBed())
                return r.SetAndReturn(Status.Fail,
                    string.Format(Translations.cmd_bed_not_a_bed, blockCenter.X, blockCenter.Y, blockCenter.Z));

            return r.SetAndReturn(Status.Done, string.Format(
                Translations.cmd_bed_trying_to_use,
                blockCenter.X,
                blockCenter.Y,
                blockCenter.Z,
                handler.PlaceBlock(block, Direction.Down) ? Translations.cmd_bed_in : Translations.cmd_bed_not_in
            ));
        }
    }
}