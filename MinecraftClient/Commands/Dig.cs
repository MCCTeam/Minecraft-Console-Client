using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Dig : Command
    {
        public override string CmdName { get { return "dig"; } }
        public override string CmdUsage { get { return "dig <x> <y> <z>"; } }
        public override string CmdDesc { get { return "cmd.dig.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
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
                        Location current = handler.GetCurrentLocation();
                        Location blockToBreak = Location.Parse(current, args[0], args[1], args[2]);
                        if (blockToBreak.DistanceSquared(current.EyesLocation()) > 25)
                            return Translations.Get("cmd.dig.too_far");
                        if (handler.GetWorld().GetBlock(blockToBreak).Type == Material.Air)
                            return Translations.Get("cmd.dig.no_block");
                        if (handler.DigBlock(blockToBreak))
                            return Translations.Get("cmd.dig.dig", blockToBreak.X, blockToBreak.Y, blockToBreak.Z);
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
