using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    class EntityNear : Command
    {
        public override string CmdName { get { return "entitynear"; } }
        public override string CmdUsage { get { return "entitynear <entitytype> <attack|use>"; } }
        public override string CmdDesc { get { return string.Empty; } }

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
                    if (Enum.TryParse<EntityType>(args[0], true, out EntityType entity_type))
                    {
                        Dictionary<int, Entity> entities = handler.GetEntities();
                        Entity? closest = null;
                        double closest_distance = double.PositiveInfinity;
                        foreach (var entity in entities)
                        {
                            if (entity.Value.Type != entity_type)
                                continue;

                            double distance = Math.Round(entity.Value.Location.Distance(handler.GetCurrentLocation()), 2);
                            if (distance < closest_distance)
                            {
                                closest = entity.Value;
                                closest_distance = distance;
                            }
                        }

                        if (closest == null)
                            return Translations.cmd_entityCmd_not_found;

                        string action = args.Length > 1
                            ? args[1].ToLower()
                            : "list";

                        switch (action)
                        {
                            case "attack":
                                handler.InteractEntity(closest.ID, InteractType.Attack);
                                return Translations.cmd_entityCmd_attacked;
                            case "use":
                                handler.InteractEntity(closest.ID, InteractType.Interact);
                                return Translations.cmd_entityCmd_used;
                            default:
                                StringBuilder response = new();
                                GetEntityInfoDetailed(response, handler, closest);
                                return response.ToString();
                        }
                    } else {
                        return GetCmdDescTranslated();
                    }
                }
                else
                {
                    Dictionary<int, Entity> entities = handler.GetEntities();
                    Entity? closest = null;
                    double closest_distance = double.PositiveInfinity;
                    foreach (var entity in entities)
                    {
                        double distance = Math.Round(entity.Value.Location.Distance(handler.GetCurrentLocation()), 2);
                        if (distance < closest_distance)
                        {
                            closest = entity.Value;
                            closest_distance = distance;
                        }
                    }

                    if (closest == null)
                        return Translations.cmd_entityCmd_not_found;

                    StringBuilder response = new();
                    GetEntityInfoDetailed(response, handler, closest);
                    return response.ToString();
                }
            }
            else return Translations.extra_entity_required;
        }
    }
}
