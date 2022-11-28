using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Move : Command
    {
        public override string CmdName { get { return "move"; } }
        public override string CmdUsage { get { return "move <on|off|get|up|down|east|west|north|south|center|x y z|gravity [on|off]> [-f]"; } }
        public override string CmdDesc { get { return Translations.cmd_move_desc + " \"-f\": " + Translations.cmd_move_desc_force; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            List<string> args = GetArgs(command.ToLower()).ToList();
            bool takeRisk = false;

            if (args.Count < 1)
            {
                string desc = GetCmdDescTranslated();

                if (handler.GetTerrainEnabled())
                    handler.Log.Info(World.GetChunkLoadingStatus(handler.GetWorld()));

                return desc;
            }

            if (args.Contains("-f"))
            {
                takeRisk = true;
                args.Remove("-f");
            }

            if (args[0] == "on")
            {
                handler.SetTerrainEnabled(true);
                return Translations.cmd_move_enable;
            }
            else if (args[0] == "off")
            {
                handler.SetTerrainEnabled(false);
                return Translations.cmd_move_disable;
            }
            else if (args[0] == "gravity")
            {
                if (args.Count >= 2)
                    Settings.InternalConfig.GravityEnabled = (args[1] == "on");
                if (Settings.InternalConfig.GravityEnabled)
                    return Translations.cmd_move_gravity_enabled;
                else return Translations.cmd_move_gravity_disabled;
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
                        case "center":
                            Location current = handler.GetCurrentLocation();
                            Location currentCenter = new(Math.Floor(current.X) + 0.5, current.Y, Math.Floor(current.Z) + 0.5);
                            handler.MoveTo(currentCenter, allowDirectTeleport: true);
                            return string.Format(Translations.cmd_move_walk, currentCenter, current);
                        case "get": return handler.GetCurrentLocation().ToString();
                        default: return string.Format(Translations.cmd_look_unknown, args[0]);
                    }

                    Location goal = Movement.Move(handler.GetCurrentLocation(), direction);

                    if (!Movement.CheckChunkLoading(handler.GetWorld(), handler.GetCurrentLocation(), goal))
                        return string.Format(Translations.cmd_move_chunk_not_loaded, goal.X, goal.Y, goal.Z);

                    if (Movement.CanMove(handler.GetWorld(), handler.GetCurrentLocation(), direction))
                    {
                        if (handler.MoveTo(goal, allowUnsafe: takeRisk))
                            return string.Format(Translations.cmd_move_moving, args[0]);
                        else
                            return takeRisk ? Translations.cmd_move_dir_fail : Translations.cmd_move_suggestforce;
                    }
                    else return Translations.cmd_move_dir_fail;
                }
                else if (args.Count == 3)
                {
                    try
                    {
                        Location current = handler.GetCurrentLocation(), currentCenter = current.ToCenter();
                        Location goal = Location.Parse(current, args[0], args[1], args[2]);

                        if (!Movement.CheckChunkLoading(handler.GetWorld(), current, goal))
                            return string.Format(Translations.cmd_move_chunk_not_loaded, goal.X, goal.Y, goal.Z);

                        if (takeRisk || Movement.PlayerFitsHere(handler.GetWorld(), goal))
                        {
                            if (current.ToFloor() == goal.ToFloor())
                                handler.MoveTo(goal, allowDirectTeleport: true);
                            else if (!handler.MoveTo(goal, allowUnsafe: takeRisk))
                                return takeRisk ? string.Format(Translations.cmd_move_fail, goal) : string.Format(Translations.cmd_move_suggestforce, goal);
                            return string.Format(Translations.cmd_move_walk, goal, current);
                        }
                        else
                            return string.Format(Translations.cmd_move_suggestforce, goal);
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else return GetCmdDescTranslated();
            }
            else return Translations.extra_terrainandmovement_required;
        }
    }
}
