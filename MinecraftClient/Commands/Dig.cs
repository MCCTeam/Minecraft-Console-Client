using System;
using System.Collections.Generic;
using Brigadier.NET;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Dig : Command
    {
        public override string CmdName { get { return "dig"; } }
        public override string CmdUsage { get { return "dig <x> <y> <z>"; } }
        public override string CmdDesc { get { return Translations.cmd_dig_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (!handler.GetTerrainEnabled())
                return Translations.extra_terrainandmovement_required;

            string[] args = GetArgs(command);
            if (args.Length == 0)
            {
                (bool hasBlock, Location blockLoc, Block block) = RaycastHelper.RaycastBlock(handler, 4.5, false);
                if (!hasBlock)
                    return Translations.cmd_dig_too_far;
                else if (block.Type == Material.Air)
                    return Translations.cmd_dig_no_block;
                else if (handler.DigBlock(blockLoc, lookAtBlock: false))
                    return string.Format(Translations.cmd_dig_dig, blockLoc.X, blockLoc.Y, blockLoc.Z, block.GetTypeString());
                else
                    return Translations.cmd_dig_fail;
            }
            else if (args.Length == 3)
            {
                try
                {
                    Location current = handler.GetCurrentLocation();
                    Location blockToBreak = Location.Parse(current.ToFloor(), args[0], args[1], args[2]);
                    if (blockToBreak.DistanceSquared(current.EyesLocation()) > 25)
                        return Translations.cmd_dig_too_far;
                    Block block = handler.GetWorld().GetBlock(blockToBreak);
                    if (block.Type == Material.Air)
                        return Translations.cmd_dig_no_block;
                    else if (handler.DigBlock(blockToBreak))
                    {
                        blockToBreak = blockToBreak.ToCenter();
                        return string.Format(Translations.cmd_dig_dig, blockToBreak.X, blockToBreak.Y, blockToBreak.Z, block.GetTypeString());
                    }
                    else
                        return Translations.cmd_dig_fail;
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
