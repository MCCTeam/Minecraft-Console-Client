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

            string[] args = GetArgs(command);
            if (args.Length == 0)
            {
                (bool hasBlock, Location blockLoc, Block block) = RaycastHelper.RaycastBlock(handler, 4.5, false);
                if (!hasBlock)
                    return Translations.Get("cmd.dig.too_far");
                else if (block.Type == Material.Air)
                    return Translations.Get("cmd.dig.no_block");
                else if (handler.DigBlock(blockLoc, lookAtBlock: false))
                    return Translations.Get("cmd.dig.dig", blockLoc.X, blockLoc.Y, blockLoc.Z, block.Type);
                else
                    return Translations.Get("cmd.dig.fail");
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
                        return Translations.Get("cmd.dig.fail");
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
