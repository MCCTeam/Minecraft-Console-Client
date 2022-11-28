using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    class Useblock : Command
    {
        public override string CmdName { get { return "useblock"; } }
        public override string CmdUsage { get { return "useblock <x> <y> <z>"; } }
        public override string CmdDesc { get { return Translations.cmd_useblock_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (!handler.GetTerrainEnabled())
                return Translations.extra_terrainandmovement_required;
            else if (HasArg(command))
            {
                string[] args = GetArgs(command);
                if (args.Length >= 3)
                {
                    Location block = Location.Parse(handler.GetCurrentLocation().ToFloor(), args[0], args[1], args[2]).ToFloor();
                    Location blockCenter = block.ToCenter();
                    bool res = handler.PlaceBlock(block, Direction.Down);
                    return string.Format(Translations.cmd_useblock_use, blockCenter.X, blockCenter.Y, blockCenter.Z, res ? "succeeded" : "failed");
                }
                else
                    return GetCmdDescTranslated();
            }
            else
                return GetCmdDescTranslated();
        }
    }
}
