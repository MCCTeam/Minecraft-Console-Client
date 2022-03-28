using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Move : Command
    {
        public override string CmdName { get { return "move"; } }
        public override string CmdUsage { get { return "move <on|off|get|up|down|east|west|north|south|x y z|gravity [on|off]> [-f]"; } }
        public override string CmdDesc { get { return "walk or start walking. \"-f\": force unsafe movements like falling or touching fire"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            List<string> args = getArgs(command.ToLower()).ToList();
            bool takeRisk = false;

            if (args.Count < 1)
                return GetCmdDescTranslated();

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
            else if (args[0] == "gravity")
            {
                if (args.Count >= 2)
                    Settings.GravityEnabled = (args[1] == "on");
                if (Settings.GravityEnabled)
                    return Translations.Get("cmd.move.gravity.enabled");
                else return Translations.Get("cmd.move.gravity.disabled");
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
                        if (handler.MoveTo(Movement.Move(handler.GetCurrentLocation(), direction), allowUnsafe: takeRisk))
                            return Translations.Get("cmd.move.moving", args[0]);
                        else return takeRisk ? Translations.Get("cmd.move.dir_fail") : Translations.Get("cmd.move.suggestforce");
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

                        if (handler.MoveTo(goal, allowUnsafe: takeRisk))
                            return Translations.Get("cmd.move.walk", goal);
                        else return takeRisk ? Translations.Get("cmd.move.fail", goal) : Translations.Get("cmd.move.suggestforce", goal);
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else return GetCmdDescTranslated();
            }
            else return Translations.Get("extra.terrainandmovement_required");
        }
    }
}
