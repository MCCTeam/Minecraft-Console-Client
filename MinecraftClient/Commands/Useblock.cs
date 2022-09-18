using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Useblock : Command
    {
        public override string CmdName { get { return "useblock"; } }
        public override string CmdUsage { get { return "useblock <x> <y> <z>"; } }
        public override string CmdDesc { get { return "cmd.useblock.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (!handler.GetTerrainEnabled())
                return Translations.Get("extra.terrainandmovement_required");
            else if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length >= 3)
                {
                    Location current = handler.GetCurrentLocation();
                    double x = args[0].StartsWith('~') ? current.X + (args[0].Length > 1 ? double.Parse(args[0][1..]) : 0) : double.Parse(args[0]);
                    double y = args[1].StartsWith('~') ? current.Y + (args[1].Length > 1 ? double.Parse(args[1][1..]) : 0) : double.Parse(args[1]);
                    double z = args[2].StartsWith('~') ? current.Z + (args[2].Length > 1 ? double.Parse(args[2][1..]) : 0) : double.Parse(args[2]);
                    Location block = new Location(x, y, z).ToFloor(), blockCenter = block.ToCenter();
                    bool res = handler.PlaceBlock(block, Direction.Down);
                    return Translations.Get("cmd.useblock.use", blockCenter.X, blockCenter.Y, blockCenter.Z, res ? "succeeded" : "failed");
                }
                else
                    return GetCmdDescTranslated();
            }
            else
                return GetCmdDescTranslated();
        }
    }
}
