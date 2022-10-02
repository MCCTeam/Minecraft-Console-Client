using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class BedCommand : Command
    {
        public override string CmdName { get { return "bed"; } }
        public override string CmdUsage { get { return "bed leave|sleep <x> <y> <z>|sleep <radius>"; } }
        public override string CmdDesc { get { return "cmd.bed.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            string[] args = GetArgs(command);

            if (args.Length >= 1)
            {
                string subcommand = args[0].ToLower().Trim();

                if (subcommand.Equals("leave") || subcommand.Equals("l"))
                {
                    handler.SendEntityAction(Protocol.EntityActionType.LeaveBed);
                    return Translations.TryGet("cmd.bed.leaving");
                }

                if (subcommand.Equals("sleep") || subcommand.Equals("s"))
                {
                    if (args.Length == 2)
                    {
                        if (!int.TryParse(args[1], out int radius))
                            return CmdUsage;

                        handler.GetLogger().Info(Translations.TryGet("cmd.bed.searching", radius));

                        Location current = handler.GetCurrentLocation();
                        Location bedLocation = current;

                        Material[] bedMaterialList = new Material[]{
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
                            return Translations.TryGet("cmd.bed.bed_not_found");

                        handler.Log.Info(Translations.TryGet("cmd.bed.found_a_bed_at", bedLocation.X, bedLocation.Y, bedLocation.Z));

                        if (!Movement.CheckChunkLoading(handler.GetWorld(), current, bedLocation))
                            return Translations.Get("cmd.move.chunk_not_loaded", bedLocation.X, bedLocation.Y, bedLocation.Z);

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
                                    handler.Log.Info(Translations.TryGet("cmd.bed.failed_to_reach_in_time", bedLocation.X, bedLocation.Y, bedLocation.Z));
                                    return;
                                }

                                handler.Log.Info(Translations.TryGet("cmd.bed.moving", bedLocation.X, bedLocation.Y, bedLocation.Z));

                                bool res = handler.PlaceBlock(bedLocation, Direction.Down);

                                handler.Log.Info(Translations.TryGet(
                                    "cmd.bed.trying_to_use",
                                    bedLocation.X,
                                    bedLocation.Y,
                                    bedLocation.Z,
                                    Translations.TryGet(res ? "cmd.bed.in" : "cmd.bed.not_in")
                                ));
                            });

                            return "";
                        }

                        return Translations.Get("cmd.bed.cant_reach_safely");
                    }

                    if (args.Length >= 3)
                    {
                        Location block = Location.Parse(handler.GetCurrentLocation(), args[1], args[2], args[3]).ToFloor();
                        Location blockCenter = block.ToCenter();

                        if (!handler.GetWorld().GetBlock(block).Type.IsBed())
                            return Translations.TryGet("cmd.bed.not_a_bed", blockCenter.X, blockCenter.Y, blockCenter.Z);

                        bool res = handler.PlaceBlock(block, Direction.Down);

                        return Translations.TryGet(
                            "cmd.bed.trying_to_use",
                            blockCenter.X,
                            blockCenter.Y,
                            blockCenter.Z,
                            Translations.TryGet(res ? "cmd.bed.in" : "cmd.bed.not_in")
                        );
                    }
                }
            }

            return CmdUsage;
        }
    }
}