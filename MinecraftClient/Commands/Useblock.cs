using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Useblock : Command
    {
        public override string CMDName { get { return "useblock"; } }
        public override string CMDDesc { get { return "useblock <x> <y> <z>: use block"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (!handler.GetTerrainEnabled()) return "Please enable TerrainHandling in the config file first.";
            if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length >= 3)
                {
                    int x = Convert.ToInt32(args[0]);
                    int y = Convert.ToInt32(args[1]);
                    int z = Convert.ToInt32(args[2]);
                    handler.PlaceBlock(new Location(x, y, z), 0);
                }
                else { return CMDDesc;  }
            }
            return CMDDesc;
        }
    }
}
