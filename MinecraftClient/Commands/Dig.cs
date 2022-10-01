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

            string[] args = getArgs(command);
            if (args.Length == 0)
            {
                Location? blockToBreak = RaycastHelper.RaycastBlock(handler, 4.0, false);
                if (blockToBreak == null)
                    return Translations.Get("cmd.dig.too_far");
                Location blockToBreak_loc = (Location)blockToBreak!;
                Block block = handler.GetWorld().GetBlock(blockToBreak_loc);
                if (block.Type == Material.Air)
                    return Translations.Get("cmd.dig.no_block");
                if (handler.DigBlock(blockToBreak_loc, lookAtBlock: false))
                    return Translations.Get("cmd.dig.dig", blockToBreak_loc.X, blockToBreak_loc.Y, blockToBreak_loc.Z, block.Type);
                else
                    return "cmd.dig.fail";
            }
            else if (args.Length == 3)
            {
                try
                {
                    Location current = handler.GetCurrentLocation();
                    Location blockToBreak = Location.Parse(current, args[0], args[1], args[2]);
                    if (blockToBreak.DistanceSquared(current.EyesLocation()) > 25)
                        return Translations.Get("cmd.dig.too_far");
                    Block block = handler.GetWorld().GetBlock(blockToBreak);
                    if (block.Type == Material.Air)
                        return Translations.Get("cmd.dig.no_block");
                    else if (handler.DigBlock(blockToBreak))
                        return Translations.Get("cmd.dig.dig", blockToBreak.X, blockToBreak.Y, blockToBreak.Z, block.Type);
                    else
                        return "cmd.dig.fail";
                }
                catch (FormatException) { return GetCmdDescTranslated(); }
            }
            else
            {
                return GetCmdDescTranslated();
            }
        }
    }
}
