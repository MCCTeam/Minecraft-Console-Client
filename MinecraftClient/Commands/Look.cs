﻿using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Look : Command
    {
        public override string CmdName { get { return "look"; } }
        public override string CmdUsage { get { return "look <x y z|yaw pitch|up|down|east|west|north|south>"; } }
        public override string CmdDesc { get { return "cmd.look.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (handler.GetTerrainEnabled())
            {
                string[] args = GetArgs(command);
                if (args.Length == 1)
                {
                    string dirStr = GetArg(command).Trim().ToLower();
                    Direction direction;
                    switch (dirStr)
                    {
                        case "up": direction = Direction.Up; break;
                        case "down": direction = Direction.Down; break;
                        case "east": direction = Direction.East; break;
                        case "west": direction = Direction.West; break;
                        case "north": direction = Direction.North; break;
                        case "south": direction = Direction.South; break;
                        default: return Translations.Get("cmd.look.unknown", dirStr);
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
                        return Translations.Get("cmd.look.at", yaw.ToString("0.00"), pitch.ToString("0.00"));
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else if (args.Length == 3)
                {
                    try
                    {
                        Location current = handler.GetCurrentLocation();
                        Location block = Location.Parse(current, args[0], args[1], args[2]);
                        handler.UpdateLocation(current, block);

                        return Translations.Get("cmd.look.block", block);
                    }
                    catch (FormatException) { return CmdUsage; }

                }
                else return GetCmdDescTranslated();
            }
            else return Translations.Get("extra.terrainandmovement_required");
        }
    }
}
