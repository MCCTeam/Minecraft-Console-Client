using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Look : Command
    {
        public override string CMDName { get { return "look"; } }
        public override string CMDDesc { get { return "look <x y z|yaw pitch|up|down|east|west|north|south>: look at direction or coordinates."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (handler.GetTerrainEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length == 1)
                {
                    string dirStr = getArg(command).Trim().ToLower();
                    Direction direction;
                    switch (dirStr)
                    {
                        case "up": direction = Direction.Up; break;
                        case "down": direction = Direction.Down; break;
                        case "east": direction = Direction.East; break;
                        case "west": direction = Direction.West; break;
                        case "north": direction = Direction.North; break;
                        case "south": direction = Direction.South; break;
                        default: return "Unknown direction '" + dirStr + "'.";
                    }

                    handler.UpdateLocation(handler.GetCurrentLocation(), direction);
                    return "Looking " + dirStr;
                }
                else if (args.Length == 2)
                {
                    try
                    {
                        float yaw = Single.Parse(args[0]);
                        float pitch = Single.Parse(args[1]);

                        handler.UpdateLocation(handler.GetCurrentLocation(), yaw, pitch);
                        return String.Format("Looking at YAW: {0} PITCH: {1}", yaw.ToString("0.00"), pitch.ToString("0.00"));
                    }
                    catch (FormatException) { return CMDDesc; }
                }
                else if (args.Length == 3)
                {
                    try
                    {
                        int x = int.Parse(args[0]);
                        int y = int.Parse(args[1]);
                        int z = int.Parse(args[2]);

                        Location block = new Location(x, y, z);
                        handler.UpdateLocation(handler.GetCurrentLocation(), block);

                        return "Looking at " + block;
                    }
                    catch (FormatException) { return CMDDesc; }
                    
                }
                else return CMDDesc;
            }
            else return "Please enable terrainandmovements in config to use this command.";
        }
    }
}
