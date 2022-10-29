using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Brigadier.NET;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    class Entitycmd : Command
    {
        public override string CmdName { get { return "entity"; } }
        public override string CmdUsage { get { return "entity <id|entitytype> <attack|use>"; } }
        public override string CmdDesc { get { return string.Empty; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (handler.GetEntityHandlingEnabled())
            {
                string[] args = GetArgs(command);
                if (args.Length >= 1)
                {
                    try
                    {
                        int entityID = int.Parse(args[0], NumberStyles.Any, CultureInfo.CurrentCulture);
                        if (entityID != 0)
                        {
                            if (handler.GetEntities().ContainsKey(entityID))
                            {
                                string action = args.Length > 1
                                    ? args[1].ToLower()
                                    : "list";
                                switch (action)
                                {
                                    case "attack":
                                        handler.InteractEntity(entityID, InteractType.Attack);
                                        return Translations.cmd_entityCmd_attacked;
                                    case "use":
                                        handler.InteractEntity(entityID, InteractType.Interact);
                                        return Translations.cmd_entityCmd_used;
                                    default:
                                        Entity entity = handler.GetEntities()[entityID];
                                        int id = entity.ID;
                                        float health = entity.Health;
                                        int latency = entity.Latency;
                                        Item item = entity.Item;
                                        string? nickname = entity.Name;
                                        string? customname = entity.CustomName;
                                        EntityPose pose = entity.Pose;
                                        EntityType type = entity.Type;
                                        double distance = Math.Round(entity.Location.Distance(handler.GetCurrentLocation()), 2);

                                        string color = "§a"; // Green
                                        if (health < 10)
                                            color = "§c";  // Red
                                        else if (health < 15)
                                            color = "§e";  // Yellow

                                        string location = $"X:{Math.Round(entity.Location.X, 2)}, Y:{Math.Round(entity.Location.Y, 2)}, Z:{Math.Round(entity.Location.Z, 2)}";
                                        StringBuilder done = new();
                                        done.Append($"{Translations.cmd_entityCmd_entity}: {id}\n [MCC] Type: {entity.GetTypeString()}");
                                        if (!string.IsNullOrEmpty(nickname))
                                            done.Append($"\n [MCC] {Translations.cmd_entityCmd_nickname}: {nickname}");
                                        else if (!string.IsNullOrEmpty(customname))
                                            done.Append($"\n [MCC] {Translations.cmd_entityCmd_customname}: {customname.Replace("&", "§")}§8");
                                        if (type == EntityType.Player)
                                            done.Append($"\n [MCC] {Translations.cmd_entityCmd_latency}: {latency}");
                                        else if (type == EntityType.Item || type == EntityType.ItemFrame || type == Mapping.EntityType.EyeOfEnder || type == Mapping.EntityType.Egg || type == Mapping.EntityType.EnderPearl || type == Mapping.EntityType.Potion || type == Mapping.EntityType.Fireball || type == Mapping.EntityType.FireworkRocket)
                                        {
                                            string? displayName = item.DisplayName;
                                            if (string.IsNullOrEmpty(displayName))
                                                done.Append($"\n [MCC] {Translations.cmd_entityCmd_item}: {item.GetTypeString()} x{item.Count}");
                                            else
                                                done.Append($"\n [MCC] {Translations.cmd_entityCmd_item}: {item.GetTypeString()} x{item.Count} - {displayName}§8");
                                        }

                                        if (entity.Equipment.Count >= 1 && entity.Equipment != null)
                                        {
                                            done.Append($"\n [MCC] {Translations.cmd_entityCmd_equipment}:");
                                            if (entity.Equipment.ContainsKey(0) && entity.Equipment[0] != null)
                                                done.Append($"\n   [MCC] {Translations.cmd_entityCmd_mainhand}: {entity.Equipment[0].GetTypeString()} x{entity.Equipment[0].Count}");
                                            if (entity.Equipment.ContainsKey(1) && entity.Equipment[1] != null)
                                                done.Append($"\n   [MCC] {Translations.cmd_entityCmd_offhand}: {entity.Equipment[1].GetTypeString()} x{entity.Equipment[1].Count}");
                                            if (entity.Equipment.ContainsKey(5) && entity.Equipment[5] != null)
                                                done.Append($"\n   [MCC] {Translations.cmd_entityCmd_helmet}: {entity.Equipment[5].GetTypeString()} x{entity.Equipment[5].Count}");
                                            if (entity.Equipment.ContainsKey(4) && entity.Equipment[4] != null)
                                                done.Append($"\n   [MCC] {Translations.cmd_entityCmd_chestplate}: {entity.Equipment[4].GetTypeString()} x{entity.Equipment[4].Count}");
                                            if (entity.Equipment.ContainsKey(3) && entity.Equipment[3] != null)
                                                done.Append($"\n   [MCC] {Translations.cmd_entityCmd_leggings}: {entity.Equipment[3].GetTypeString()} x{entity.Equipment[3].Count}");
                                            if (entity.Equipment.ContainsKey(2) && entity.Equipment[2] != null)
                                                done.Append($"\n   [MCC] {Translations.cmd_entityCmd_boots}: {entity.Equipment[2].GetTypeString()} x{entity.Equipment[2].Count}");
                                        }
                                        done.Append($"\n [MCC] {Translations.cmd_entityCmd_pose}: {pose}");
                                        done.Append($"\n [MCC] {Translations.cmd_entityCmd_health}: {color}{health}§8");
                                        done.Append($"\n [MCC] {Translations.cmd_entityCmd_distance}: {distance}");
                                        done.Append($"\n [MCC] {Translations.cmd_entityCmd_location}: {location}");
                                        return done.ToString();
                                }
                            }
                            else return Translations.cmd_entityCmd_not_found;
                        }
                        else
                        {
                            EntityType interacttype = Enum.Parse<EntityType>(args[0]);
                            string actionst = Translations.cmd_entityCmd_attacked;
                            int actioncount = 0;
                            foreach (var entity2 in handler.GetEntities())
                            {
                                if (entity2.Value.Type == interacttype)
                                {
                                    string action = args.Length > 1
                                    ? args[1].ToLower()
                                    : "list";
                                    if (action == "attack")
                                    {
                                        handler.InteractEntity(entity2.Key, InteractType.Attack);
                                        actionst = Translations.cmd_entityCmd_attacked;
                                        actioncount++;
                                    }
                                    else if (action == "use")
                                    {
                                        handler.InteractEntity(entity2.Key, InteractType.Interact);
                                        actionst = Translations.cmd_entityCmd_used;
                                        actioncount++;
                                    }
                                    else return GetCmdDescTranslated();
                                }
                            }
                            return actioncount + " " + actionst;
                        }
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else
                {
                    Dictionary<int, Entity> entities = handler.GetEntities();
                    StringBuilder response = new();
                    response.AppendLine(Translations.cmd_entityCmd_entities);
                    foreach (var entity2 in entities)
                    {
                        int id = entity2.Key;
                        float health = entity2.Value.Health;
                        int latency = entity2.Value.Latency;
                        string? nickname = entity2.Value.Name;
                        string? customname = entity2.Value.CustomName;
                        EntityPose pose = entity2.Value.Pose;
                        EntityType type = entity2.Value.Type;
                        Item item = entity2.Value.Item;
                        string location = $"X:{Math.Round(entity2.Value.Location.X, 2)}, Y:{Math.Round(entity2.Value.Location.Y, 2)}, Z:{Math.Round(entity2.Value.Location.Z, 2)}";

                        if (type == EntityType.Item || type == EntityType.ItemFrame || type == EntityType.EyeOfEnder || type == EntityType.Egg || type == EntityType.EnderPearl || type == EntityType.Potion || type == EntityType.Fireball || type == EntityType.FireworkRocket)
                            response.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity2.Value.GetTypeString()}, {Translations.cmd_entityCmd_item}: {item.GetTypeString()}, {Translations.cmd_entityCmd_location}: {location}");
                        else if (type == EntityType.Player && !string.IsNullOrEmpty(nickname))
                            response.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity2.Value.GetTypeString()}, {Translations.cmd_entityCmd_nickname}: §8{nickname}§8, {Translations.cmd_entityCmd_latency}: {latency}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_pose}: {pose}, {Translations.cmd_entityCmd_location}: {location}");
                        else if (type == EntityType.Player && !string.IsNullOrEmpty(customname))
                            response.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity2.Value.GetTypeString()}, {Translations.cmd_entityCmd_customname}: §8{customname.Replace("&", "§")}§8, {Translations.cmd_entityCmd_latency}: {latency}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_pose}: {pose}, {Translations.cmd_entityCmd_location}: {location}");
                        else
                            response.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity2.Value.GetTypeString()}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_location}: {location}");
                    }
                    response.Append(GetCmdDescTranslated());
                    return response.ToString();
                }
            }
            else return Translations.extra_entity_required;
        }
    }
}
