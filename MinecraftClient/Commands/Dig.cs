using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Dig : Command
    {
        public override string CmdName { get { return "dig"; } }
        public override string CmdUsage { get { return "dig <x> <y> <z>"; } }
        public override string CmdDesc { get { return "cmd.dig.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (!handler.GetTerrainEnabled())
                return Translations.Get("extra.terrainandmovement_required");

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
                        if (blockToBreak.DistanceSquared(handler.GetCurrentLocation().EyesLocation()) > 25)
                            return Translations.Get("cmd.dig.too_far");
                        if (handler.GetWorld().GetBlock(blockToBreak).Type == Material.Air)
                            return Translations.Get("cmd.dig.no_block");
                        if (handler.DigBlock(blockToBreak))
                            return Translations.Get("cmd.dig.dig", x, y, z);
                        else return "cmd.dig.fail";
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else return GetCmdDescTranslated();
            }
            else return GetCmdDescTranslated();
        }
    }
}
