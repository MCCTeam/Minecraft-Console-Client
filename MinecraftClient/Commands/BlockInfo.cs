using System.Collections.Generic;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class BlockInfo : Command
    {
        public override string CmdName { get { return "blockinfo"; } }
        public override string CmdUsage { get { return "blockinfo <x> <y> <z> [-s]"; } }
        public override string CmdDesc { get { return Translations.cmd_blockinfo_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (!handler.GetTerrainEnabled())
                return Translations.error_terrain_not_enabled;

            string[] args = GetArgs(command);

            if (args.Length < 3)
                return CmdUsage;

            bool reportSurrounding = args.Length >= 4 && args[3].Equals("-s", System.StringComparison.OrdinalIgnoreCase);

            Location current = handler.GetCurrentLocation();
            Location targetBlockLocation = Location.Parse(current, args[0], args[1], args[2]);

            Block block = handler.GetWorld().GetBlock(targetBlockLocation);

            handler.Log.Info($"{Translations.cmd_blockinfo_BlockType}: {block.GetTypeString()}");

            if (reportSurrounding)
            {
                StringBuilder sb = new();
                sb.AppendLine($"{Translations.cmd_blockinfo_BlocksAround}:");

                Block blockXPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X + 1, targetBlockLocation.Y, targetBlockLocation.Z));
                Block blockXNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X - 1, targetBlockLocation.Y, targetBlockLocation.Z));
                Block blockYPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y + 1, targetBlockLocation.Z));
                Block blockYNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y - 1, targetBlockLocation.Z));
                Block blockZPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y, targetBlockLocation.Z + 1));
                Block blockZNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y, targetBlockLocation.Z - 1));

                sb.AppendLine($"[X {Translations.cmd_blockinfo_Positive}] {Translations.cmd_blockinfo_BlockType}: {blockXPositive.GetTypeString()}");
                sb.AppendLine($"[X {Translations.cmd_blockinfo_Negative}] {Translations.cmd_blockinfo_BlockType}: {blockXNegative.GetTypeString()}");

                sb.AppendLine(" ");

                sb.AppendLine($"[Y {Translations.cmd_blockinfo_Positive}] {Translations.cmd_blockinfo_BlockType}: {blockYPositive.GetTypeString()}");
                sb.AppendLine($"[Y {Translations.cmd_blockinfo_Negative}] {Translations.cmd_blockinfo_BlockType}: {blockYNegative.GetTypeString()}");

                sb.AppendLine(" ");

                sb.AppendLine($"[Z {Translations.cmd_blockinfo_Positive}] {Translations.cmd_blockinfo_BlockType}: {blockZPositive.GetTypeString()}");
                sb.AppendLine($"[Z {Translations.cmd_blockinfo_Negative}] {Translations.cmd_blockinfo_BlockType}: {blockZNegative.GetTypeString()}");

                handler.Log.Info(sb.ToString());
            }


            return "";
        }
    }
}
