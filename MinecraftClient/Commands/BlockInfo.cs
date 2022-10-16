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

            handler.Log.Info("Block Type: " + block.Type);

            if (reportSurrounding)
            {
                StringBuilder sb = new();
                sb.AppendLine("Blocks around:");

                Block blockXPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X + 1, targetBlockLocation.Y, targetBlockLocation.Z));
                Block blockXNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X - 1, targetBlockLocation.Y, targetBlockLocation.Z));
                Block blockYPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y + 1, targetBlockLocation.Z));
                Block blockYNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y - 1, targetBlockLocation.Z));
                Block blockZPositive = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y, targetBlockLocation.Z + 1));
                Block blockZNegative = handler.GetWorld().GetBlock(new Location(targetBlockLocation.X, targetBlockLocation.Y, targetBlockLocation.Z - 1));

                sb.AppendLine("\t[X Positive] Block Type: " + blockXPositive.Type);
                sb.AppendLine("\t[X Negative] Block Type: " + blockXNegative.Type);
                sb.AppendLine(" ");
                sb.AppendLine("\t[Y Positive] Block Type: " + blockYPositive.Type);
                sb.AppendLine("\t[Y Negative] Block Type: " + blockYNegative.Type);
                sb.AppendLine(" ");
                sb.AppendLine("\t[Z Positive] Block Type: " + blockZPositive.Type);
                sb.AppendLine("\t[Z Negative] Block Type: " + blockZNegative.Type);

                handler.Log.Info(sb.ToString());
            }


            return "";
        }
    }
}
