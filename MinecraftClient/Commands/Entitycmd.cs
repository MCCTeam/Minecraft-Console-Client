using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    class Entitycmd : Command
    {
        public override string CmdName { get { return "entity"; } }
        public override string CmdUsage { get { return "entity <id|entitytype> <attack|use>"; } }
        public override string CmdDesc { get { return string.Empty; } }

        private static void GetEntityInfoShort(StringBuilder sb, Entity entity)
        {
            int id = entity.ID;
            float health = entity.Health;
            int latency = entity.Latency;
            string? nickname = entity.Name;
            string? customname = entity.CustomName;
            EntityPose pose = entity.Pose;
            EntityType type = entity.Type;
            Item item = entity.Item;
            string location = $"X:{Math.Round(entity.Location.X, 2)}, Y:{Math.Round(entity.Location.Y, 2)}, Z:{Math.Round(entity.Location.Z, 2)}";

            if (type == EntityType.Item || type == EntityType.ItemFrame || type == EntityType.EyeOfEnder || type == EntityType.Egg || type == EntityType.EnderPearl || type == EntityType.Potion || type == EntityType.Fireball || type == EntityType.FireworkRocket)
                sb.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_item}: {item.GetTypeString()}, {Translations.cmd_entityCmd_location}: {location}");
            else if (type == EntityType.Player && !string.IsNullOrEmpty(nickname))
                sb.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_nickname}: §8{nickname}§8, {Translations.cmd_entityCmd_latency}: {latency}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_pose}: {pose}, {Translations.cmd_entityCmd_location}: {location}");
            else if (type == EntityType.Player && !string.IsNullOrEmpty(customname))
                sb.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_customname}: §8{customname.Replace("&", "§")}§8, {Translations.cmd_entityCmd_latency}: {latency}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_pose}: {pose}, {Translations.cmd_entityCmd_location}: {location}");
            else
                sb.AppendLine($" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_location}: {location}");
        }

        private static void GetEntityInfoDetailed(StringBuilder sb, McClient handler, Entity entity)
        {
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
            sb.Append($"{Translations.cmd_entityCmd_entity}: {id}\n [MCC] Type: {entity.GetTypeString()}");
            if (!string.IsNullOrEmpty(nickname))
                sb.Append($"\n [MCC] {Translations.cmd_entityCmd_nickname}: {nickname}");
            else if (!string.IsNullOrEmpty(customname))
                sb.Append($"\n [MCC] {Translations.cmd_entityCmd_customname}: {customname.Replace("&", "§")}§8");
            if (type == EntityType.Player)
                sb.Append($"\n [MCC] {Translations.cmd_entityCmd_latency}: {latency}");
            else if (type == EntityType.Item || type == EntityType.ItemFrame || type == Mapping.EntityType.EyeOfEnder || type == Mapping.EntityType.Egg || type == Mapping.EntityType.EnderPearl || type == Mapping.EntityType.Potion || type == Mapping.EntityType.Fireball || type == Mapping.EntityType.FireworkRocket)
            {
                string? displayName = item.DisplayName;
                if (string.IsNullOrEmpty(displayName))
                    sb.Append($"\n [MCC] {Translations.cmd_entityCmd_item}: {item.GetTypeString()} x{item.Count}");
                else
                    sb.Append($"\n [MCC] {Translations.cmd_entityCmd_item}: {item.GetTypeString()} x{item.Count} - {displayName}§8");
            }

            if (entity.Equipment.Count >= 1 && entity.Equipment != null)
            {
                sb.Append($"\n [MCC] {Translations.cmd_entityCmd_equipment}:");
                if (entity.Equipment.ContainsKey(0) && entity.Equipment[0] != null)
                    sb.Append($"\n   [MCC] {Translations.cmd_entityCmd_mainhand}: {entity.Equipment[0].GetTypeString()} x{entity.Equipment[0].Count}");
                if (entity.Equipment.ContainsKey(1) && entity.Equipment[1] != null)
                    sb.Append($"\n   [MCC] {Translations.cmd_entityCmd_offhand}: {entity.Equipment[1].GetTypeString()} x{entity.Equipment[1].Count}");
                if (entity.Equipment.ContainsKey(5) && entity.Equipment[5] != null)
                    sb.Append($"\n   [MCC] {Translations.cmd_entityCmd_helmet}: {entity.Equipment[5].GetTypeString()} x{entity.Equipment[5].Count}");
                if (entity.Equipment.ContainsKey(4) && entity.Equipment[4] != null)
                    sb.Append($"\n   [MCC] {Translations.cmd_entityCmd_chestplate}: {entity.Equipment[4].GetTypeString()} x{entity.Equipment[4].Count}");
                if (entity.Equipment.ContainsKey(3) && entity.Equipment[3] != null)
                    sb.Append($"\n   [MCC] {Translations.cmd_entityCmd_leggings}: {entity.Equipment[3].GetTypeString()} x{entity.Equipment[3].Count}");
                if (entity.Equipment.ContainsKey(2) && entity.Equipment[2] != null)
                    sb.Append($"\n   [MCC] {Translations.cmd_entityCmd_boots}: {entity.Equipment[2].GetTypeString()} x{entity.Equipment[2].Count}");
            }
            sb.Append($"\n [MCC] {Translations.cmd_entityCmd_pose}: {pose}");
            sb.Append($"\n [MCC] {Translations.cmd_entityCmd_health}: {color}{health}§8");
            sb.Append($"\n [MCC] {Translations.cmd_entityCmd_distance}: {distance}");
            sb.Append($"\n [MCC] {Translations.cmd_entityCmd_location}: {location}");
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (handler.GetEntityHandlingEnabled())
            {
                string[] args = GetArgs(command);
                if (args.Length >= 1)
                {
                    if (int.TryParse(args[0], NumberStyles.Any, CultureInfo.CurrentCulture, out int entityID))
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
                                    StringBuilder done = new();
                                    GetEntityInfoDetailed(done, handler, entity);
                                    return done.ToString();
                            }
                        }
                        else return Translations.cmd_entityCmd_not_found;
                    }
                    else if (Enum.TryParse(args[0], true, out EntityType interacttype))
                    {
                        string action = args.Length > 1
                        ? args[1].ToLower()
                        : "list";
                        if (action == "attack" || action == "use")
                        {
                            string actionst = Translations.cmd_entityCmd_attacked;
                            int actioncount = 0;
                            foreach (var entity2 in handler.GetEntities())
                            {
                                if (entity2.Value.Type == interacttype)
                                {
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

                        StringBuilder response = new();
                        response.AppendLine(Translations.cmd_entityCmd_entities);
                        foreach (var entity2 in handler.GetEntities())
                        {
                            if (entity2.Value.Type == interacttype)
                            {
                                GetEntityInfoShort(response, entity2.Value);
                            }
                        }
                        response.Append(GetCmdDescTranslated());
                        return response.ToString();
                    }
                    else return GetCmdDescTranslated();
                }
                else
                {
                    Dictionary<int, Entity> entities = handler.GetEntities();
                    StringBuilder response = new();
                    response.AppendLine(Translations.cmd_entityCmd_entities);
                    foreach (var entity2 in entities)
                    {
                        GetEntityInfoShort(response, entity2.Value);
                    }
                    response.Append(GetCmdDescTranslated());
                    return response.ToString();
                }
            }
            else return Translations.extra_entity_required;
        }
    }
}
