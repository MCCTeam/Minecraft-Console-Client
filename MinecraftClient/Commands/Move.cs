using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Move : Command
    {
        public override string CmdName { get { return "move"; } }
        public override string CmdUsage { get { return "move <on|off|get|up|down|east|west|north|south|x y z>"; } }
        public override string CmdDesc { get { return "walk or start walking."; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            string[] args = getArgs(command.ToLower());

            if (args[0] == "on")
            {
                handler.SetTerrainEnabled(true);
                return Translations.Get("cmd.move.enable");
            }
            else if (args[0] == "off")
            {
                handler.SetTerrainEnabled(false);
                return Translations.Get("cmd.move.disable");
            }
            else if (handler.GetTerrainEnabled())
            {
                if (args.Length == 1 || (args.Length == 2 && args.Contains("-f")))
                {
                    Direction direction;
                    switch (args[0])
                    {
                        case "up": direction = Direction.Up; break;
                        case "down": direction = Direction.Down; break;
                        case "east": direction = Direction.East; break;
                        case "west": direction = Direction.West; break;
                        case "north": direction = Direction.North; break;
                        case "south": direction = Direction.South; break;
                        case "get": return handler.GetCurrentLocation().ToString();
                        default: return Translations.Get("cmd.look.unknown", args[0]);
                    }
                    if (Movement.CanMove(handler.GetWorld(), handler.GetCurrentLocation(), direction))
                    {
                        if (args.Contains("-f"))
                            handler.MoveTo(Movement.Move(handler.GetCurrentLocation(), direction), allowUnsafe: true);
                        else
                            handler.MoveTo(Movement.Move(handler.GetCurrentLocation(), direction));

                        return Translations.Get("cmd.move.moving", args[0]);
                    }
                    else return Translations.Get("cmd.move.dir_fail");
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
                            return Translations.Get("cmd.move.walk", goal);
                        return Translations.Get("cmd.move.fail", goal);
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else return GetCmdDescTranslated();
            }
            else return Translations.Get("extra.terrainandmovement_required");
        }
    }
}
