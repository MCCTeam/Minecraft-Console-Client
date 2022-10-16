using System.Collections.Generic;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class BlockInfo : Command
    {
        public override string CmdName { get { return "blockinfo"; } }
        public override string CmdUsage { get { return "blockinfo <x> <y> <z> [-s]"; } }
        public override string CmdDesc { get { return "cmd.blockinfo.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            string[] args = GetArgs(command);

            if (args.Length < 3)
                return CmdUsage;

            bool reportSurrounding = args.Length >= 4 && args[3].Equals("-s", System.StringComparison.OrdinalIgnoreCase);

            Location current = handler.GetCurrentLocation();
            Location targetBlockLocation = Location.Parse(current, args[0], args[1], args[2]);

            Block block = handler.GetWorld().GetBlock(targetBlockLocation);

            handler.Log.Info(Translations.TryGet("cmd.blockinfo.BlockType") + ": " + block.Type);

            if (reportSurrounding)
            {
                StringBuilder sb = new();
                sb.AppendLine(Translations.TryGet("cmd.blockinfo.BlocksAround") + ":");

                Block blockXPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X + 1, targetBlockLocation.Y, targetBlockLocation.Z));
                Block blockXNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X - 1, targetBlockLocation.Y, targetBlockLocation.Z));
                Block blockYPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y + 1, targetBlockLocation.Z));
                Block blockYNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y - 1, targetBlockLocation.Z));
                Block blockZPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y, targetBlockLocation.Z + 1));
                Block blockZNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y, targetBlockLocation.Z - 1));

                sb.AppendLine("[X " + Translations.TryGet("cmd.blockinfo.Positive") + "] " + Translations.TryGet("cmd.blockinfo.BlockType") + ": " + blockXPositive.Type);
                sb.AppendLine("[X " + Translations.TryGet("cmd.blockinfo.Negative") + "] " + Translations.TryGet("cmd.blockinfo.BlockType") + ": " + blockXNegative.Type);
                sb.AppendLine(" ");
                sb.AppendLine("[Y " + Translations.TryGet("cmd.blockinfo.Positive") + "] " + Translations.TryGet("cmd.blockinfo.BlockType") + ": " + blockYPositive.Type);
                sb.AppendLine("[Y " + Translations.TryGet("cmd.blockinfo.Negative") + "] " + Translations.TryGet("cmd.blockinfo.BlockType") + ": " + blockYNegative.Type);
                sb.AppendLine(" ");
                sb.AppendLine("[Z " + Translations.TryGet("cmd.blockinfo.Positive") + "] " + Translations.TryGet("cmd.blockinfo.BlockType") + ": " + blockZPositive.Type);
                sb.AppendLine("[Z " + Translations.TryGet("cmd.blockinfo.Negative") + "] " + Translations.TryGet("cmd.blockinfo.BlockType") + ": " + blockZNegative.Type);

                handler.Log.Info(sb.ToString());
            }


            return "";
        }
    }
}
