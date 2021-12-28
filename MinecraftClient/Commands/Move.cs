using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Move : Command
    {
        public override string CmdName { get { return "move"; } }
        public override string CmdUsage { get { return "move <on|off|get|up|down|east|west|north|south|x y z> [-f]"; } }
        public override string CmdDesc { get { return "walk or start walking. Included protection can be bypassed with \"-f\"."; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            List<string> args = getArgs(command.ToLower()).ToList();
            bool takeRisk = false;

            if (args.Contains("-f"))
            {
                takeRisk = true;
                args.Remove("-f");
            }

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
                if (args.Count == 1)
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
                        return handler.MoveTo(Movement.Move(handler.GetCurrentLocation(), direction), allowUnsafe: takeRisk) ? 
                            Translations.Get("cmd.move.moving", args[0]) : Translations.Get("cmd.move.suggestforce");
                    }
                    else return Translations.Get("cmd.move.dir_fail");
                }
                else if (args.Count == 3)
                {
                    try
                    {
                        int x = int.Parse(args[0]);
                        int y = int.Parse(args[1]);
                        int z = int.Parse(args[2]);
                        Location goal = new Location(x, y, z);

                        return handler.MoveTo(goal, allowUnsafe: takeRisk) ?
                           Translations.Get("cmd.move.walk", goal) : Translations.Get("cmd.move.suggestforce");
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else return GetCmdDescTranslated();
            }
            else return Translations.Get("extra.terrainandmovement_required");
        }
    }
}
