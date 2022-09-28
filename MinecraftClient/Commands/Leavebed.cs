using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;
using PInvoke;

namespace MinecraftClient.Commands
{
    public class BedCommand : Command
    {
        public override string CmdName { get { return "bed"; } }
        public override string CmdUsage { get { return "bed leave|sleep <x> <y> <z>"; } }
        public override string CmdDesc { get { return "cmd.bed.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            string[] args = getArgs(command);

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
                    if (args.Length >= 3)
                    {
                        Location current = handler.GetCurrentLocation();
                        double x = args[1].StartsWith('~') ? current.X + (args[1].Length > 1 ? double.Parse(args[1][1..]) : 0) : double.Parse(args[1]);
                        double y = args[2].StartsWith('~') ? current.Y + (args[2].Length > 1 ? double.Parse(args[2][1..]) : 0) : double.Parse(args[2]);
                        double z = args[3].StartsWith('~') ? current.Z + (args[3].Length > 1 ? double.Parse(args[3][1..]) : 0) : double.Parse(args[3]);

                        Location block = new Location(x, y, z).ToFloor(), blockCenter = block.ToCenter();

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