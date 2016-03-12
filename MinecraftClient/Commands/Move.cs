using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Move : Command
    {
        public override string CMDName { get { return "move"; } }
        public override string CMDDesc { get { return "move <get|up|down|east|west|north|south|x y z>: walk or start walking."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (Settings.TerrainAndMovements)
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
                        case "get": return handler.GetCurrentLocation().ToString();
                        default: return "Unknown direction '" + dirStr + "'.";
                    }
                    if (Movement.CanMove(handler.GetWorld(), handler.GetCurrentLocation(), direction))
                    {
                        handler.MoveTo(Movement.Move(handler.GetCurrentLocation(), direction));
                        return "Moving " + dirStr + '.';
                    }
                    else return "Cannot move in that direction.";
                }
                else if (args.Length == 3)
                {
                    try
                    {
                        int x = int.Parse(args[0]);
                        int y = int.Parse(args[1]);
                        int z = int.Parse(args[2]);
                        Location goal = new Location(x, y, z);
                        if (handler.MoveTo(goal))
                            return "Walking to " + goal;
                        return "Failed to compute path to " + goal;
                    }
                    catch (FormatException) { return CMDDesc; }
                }
                else return CMDDesc;
            }
            else return "Please enable terrainandmovements in config to use this command.";
        }
    }
}
