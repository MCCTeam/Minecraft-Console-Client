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
        public override string CMDDesc { get { return "move <on|off|get|up|down|east|west|north|south|x y z>: walk or start walking."; } }

        public override string Run(McTcpClient handler, string command)
        {
            string[] args = getArgs(command);
            string argStr = getArg(command).Trim().ToLower();

            if (argStr == "on")
            {
                handler.SetTerrainEnabled(true);
                return "Enabling Terrain and Movements on next server login, respawn or world change.";
            }
            else if (argStr == "off")
            {
                handler.SetTerrainEnabled(false);
                return "Disabling Terrain and Movements.";
            }
            else if (handler.GetTerrainEnabled())
            {
                if (args.Length == 1)
                {
                    Direction direction;
                    switch (argStr)
                    {
                        case "up": direction = Direction.Up; break;
                        case "down": direction = Direction.Down; break;
                        case "east": direction = Direction.East; break;
                        case "west": direction = Direction.West; break;
                        case "north": direction = Direction.North; break;
                        case "south": direction = Direction.South; break;
                        case "get": return handler.GetCurrentLocation().ToString();
                        default: return "Unknown direction '" + argStr + "'.";
                    }
                    if (Movement.CanMove(handler.GetWorld(), handler.GetCurrentLocation(), direction))
                    {
                        handler.MoveTo(Movement.Move(handler.GetCurrentLocation(), direction));
                        return "Moving " + argStr + '.';
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
            else return "Please enable terrainandmovements to use this command.";
        }
    }
}
