using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class BedCommand : Command
    {
        public override string CmdName { get { return "bed"; } }
        public override string CmdUsage { get { return "bed leave|sleep <x> <y> <z>|sleep <radius>"; } }
        public override string CmdDesc { get { return Translations.cmd_bed_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            string[] args = GetArgs(command);

            if (args.Length >= 1)
            {
                string subcommand = args[0].ToLower().Trim();

                if (subcommand.Equals("leave") || subcommand.Equals("l"))
                {
                    handler.SendEntityAction(Protocol.EntityActionType.LeaveBed);
                    return Translations.cmd_bed_leaving;
                }

                if (subcommand.Equals("sleep") || subcommand.Equals("s"))
                {
                    if (!handler.GetTerrainEnabled())
                        return Translations.error_terrain_not_enabled;

                    if (args.Length == 2)
                    {
                        if (!int.TryParse(args[1], NumberStyles.Any, CultureInfo.CurrentCulture, out int radius))
                            return CmdUsage;

                        handler.GetLogger().Info(string.Format(Translations.cmd_bed_searching, radius));

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
                            return Translations.cmd_bed_bed_not_found;

                        handler.Log.Info(string.Format(Translations.cmd_bed_found_a_bed_at, bedLocation.X, bedLocation.Y, bedLocation.Z));

                        if (!Movement.CheckChunkLoading(handler.GetWorld(), current, bedLocation))
                            return string.Format(Translations.cmd_move_chunk_not_loaded, bedLocation.X, bedLocation.Y, bedLocation.Z);

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

                            return "";
                        }

                        return Translations.cmd_bed_cant_reach_safely;
                    }

                    if (args.Length >= 3)
                    {
                        Location block = Location.Parse(handler.GetCurrentLocation(), args[1], args[2], args[3]).ToFloor();
                        Location blockCenter = block.ToCenter();

                        if (!handler.GetWorld().GetBlock(block).Type.IsBed())
                            return string.Format(Translations.cmd_bed_not_a_bed, blockCenter.X, blockCenter.Y, blockCenter.Z);

                        bool res = handler.PlaceBlock(block, Direction.Down);

                        return string.Format(
                            Translations.cmd_bed_trying_to_use,
                            blockCenter.X,
                            blockCenter.Y,
                            blockCenter.Z,
                            res ? Translations.cmd_bed_in : Translations.cmd_bed_not_in
                        );
                    }
                }
            }

            return CmdUsage;
        }
    }
}