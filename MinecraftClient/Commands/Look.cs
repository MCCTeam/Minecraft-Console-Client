using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Look : Command
    {
        public override string CmdName { get { return "look"; } }
        public override string CmdUsage { get { return "look <x y z|yaw pitch|up|down|east|west|north|south>"; } }
        public override string CmdDesc { get { return "cmd.look.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetTerrainEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length == 0)
                {
                    const double maxDistance = 8.0;
                Retry:
                    Location? target = RaycastHelper.RaycastBlock(handler, maxDistance, false);
                    if (target == null)
                        return Translations.Get("cmd.look.noinspection", maxDistance);
                    else
                    {
                        Location target_loc = (Location)target;
                        Block block = handler.GetWorld().GetBlock(target_loc);
                        if (block.Type == Material.Air)
                            goto Retry;
                        Location current = handler.GetCurrentLocation(), target_center = target_loc.ToCenter();
                        return Translations.Get("cmd.look.inspection", block.Type, target_loc!.X, target_loc.Y, target_loc.Z,
                            current.Distance(target_center), current.EyesLocation().Distance(target_center));
                    }
                }
                else if (args.Length == 1)
                {
                    string dirStr = getArg(command).Trim().ToLower();
                    Direction direction;
                    switch (dirStr)
                    {
                        case "up": direction = Direction.Up; break;
                        case "down": direction = Direction.Down; break;
                        case "east": direction = Direction.East; break;
                        case "west": direction = Direction.West; break;
                        case "north": direction = Direction.North; break;
                        case "south": direction = Direction.South; break;
                        default: return Translations.Get("cmd.look.unknown", dirStr);
                    }

                    handler.UpdateLocation(handler.GetCurrentLocation(), direction);
                    return "Looking " + dirStr;
                }
                else if (args.Length == 2)
                {
                    try
                    {
                        float yaw = Single.Parse(args[0]);
                        float pitch = Single.Parse(args[1]);

                        handler.UpdateLocation(handler.GetCurrentLocation(), yaw, pitch);
                        return Translations.Get("cmd.look.at", yaw.ToString("0.00"), pitch.ToString("0.00"));
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else if (args.Length == 3)
                {
                    try
                    {
                        Location current = handler.GetCurrentLocation();
                        Location block = Location.Parse(current, args[0], args[1], args[2]);
                        handler.UpdateLocation(current, block);

                        return Translations.Get("cmd.look.block", block);
                    }
                    catch (FormatException) { return CmdUsage; }
                    
                }
                else return GetCmdDescTranslated();
            }
            else return Translations.Get("extra.terrainandmovement_required");
        }
    }
}
