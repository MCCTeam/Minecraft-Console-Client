using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Dig : Command
    {
        public override string CMDName { get { return "dig"; } }
        public override string CMDDesc { get { return "dig <x> <y> <z>: attempt to break a block"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (!handler.GetTerrainEnabled())
                return "Please enable Terrain and Movements to use this command.";

            if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length == 3)
                {
                    try
                    {
                        int x = int.Parse(args[0]);
                        int y = int.Parse(args[1]);
                        int z = int.Parse(args[2]);
                        Location blockToBreak = new Location(x, y, z);
                        if (blockToBreak.DistanceSquared(handler.GetCurrentLocation()) > 25)
                            return "You are too far away from this block.";
                        if (handler.GetWorld().GetBlock(blockToBreak).Type == Material.Air)
                            return "No block at this location (Air)";
                        if (handler.DigBlock(blockToBreak))
                            return String.Format("Attempting to dig block at {0} {1} {2}", x, y, z);
                        else return "Failed to start digging block.";
                    }
                    catch (FormatException) { return CMDDesc; }
                }
                else return CMDDesc;
            }
            else return CMDDesc;
        }
    }
}
