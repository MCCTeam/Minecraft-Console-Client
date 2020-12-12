using System;
using System.Collections.Generic;
using System.Drawing;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    class Entitycmd : Command
    {
        public override string CmdName { get { return "entity"; } }
        public override string CmdUsage { get { return "entity <id|entitytype> <attack|use>"; } }
        public override string CmdDesc { get { return ""; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetEntityHandlingEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length >= 1)
                {
                    try
                    {
                        int entityID = 0;
                        int.TryParse(args[0], out entityID);
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
                                        handler.InteractEntity(entityID, 1);
                                        return Translations.Get("cmd.entityCmd.attacked");
                                    case "use":
                                        handler.InteractEntity(entityID, 0);
                                        return Translations.Get("cmd.entityCmd.used");
                                    default:
                                        Entity entity = handler.GetEntities()[entityID];
                                        int id = entity.ID;
                                        float health = entity.Health;
                                        int latency = entity.Latency;
                                        Item item = entity.Item;
                                        string nickname = entity.Name;
                                        string customname = entity.CustomName;
                                        EntityPose pose = entity.Pose;
                                        EntityType type = entity.Type;
                                        double distance = Math.Round(entity.Location.Distance(handler.GetCurrentLocation()), 2);

                                        string color = "§a"; // Green
                                        if (health < 10)
                                            color = "§c";  // Red
                                        else if (health < 15)
                                            color = "§e";  // Yellow

                                        string location = String.Format("X:{0}, Y:{1}, Z:{2}", Math.Round(entity.Location.X, 2), Math.Round(entity.Location.Y, 2), Math.Round(entity.Location.Z, 2));
                                        string done = Translations.Replace("([cmd.entityCmd.entity]): {0}\n [MCC] Type: {1}", id, type);
                                        if (!String.IsNullOrEmpty(nickname))
                                            done += Translations.Replace("\n [MCC] ([cmd.entityCmd.nickname]): {0}", nickname);
                                        else if (!String.IsNullOrEmpty(customname))
                                            done += Translations.Replace("\n [MCC] ([cmd.entityCmd.customname]): {0}§8", customname.Replace("&", "§"));
                                        if (type == EntityType.Player)
                                            done += Translations.Replace("\n [MCC] ([cmd.entityCmd.latency]): {0}", latency);
                                        else if (type == EntityType.Item || type == EntityType.ItemFrame || type == Mapping.EntityType.EyeOfEnder || type == Mapping.EntityType.Egg || type == Mapping.EntityType.EnderPearl || type == Mapping.EntityType.Potion || type == Mapping.EntityType.Fireball || type == Mapping.EntityType.FireworkRocket)
                                        {
                                            string displayName = item.DisplayName;
                                            if (String.IsNullOrEmpty(displayName))
                                                done += Translations.Replace("\n [MCC] ([cmd.entityCmd.item]): {0} x{1}", item.Type, item.Count);
                                            else
                                                done += Translations.Replace("\n [MCC] ([cmd.entityCmd.item]): {0} x{1} - {2}§8", item.Type, item.Count, displayName);
                                        }

                                        if (entity.Equipment.Count >= 1 && entity.Equipment != null)
                                        {
                                            done += Translations.Replace("\n [MCC] ([cmd.entityCmd.equipment]):");
                                            if (entity.Equipment.ContainsKey(0) && entity.Equipment[0] != null)
                                                done += Translations.Replace("\n   [MCC] ([cmd.entityCmd.mainhand]): {0} x{1}", entity.Equipment[0].Type, entity.Equipment[0].Count);
                                            if (entity.Equipment.ContainsKey(1) && entity.Equipment[1] != null)
                                                done += Translations.Replace("\n   [MCC] ([cmd.entityCmd.offhand]): {0} x{1}", entity.Equipment[1].Type, entity.Equipment[1].Count);
                                            if (entity.Equipment.ContainsKey(5) && entity.Equipment[5] != null)
                                                done += Translations.Replace("\n   [MCC] ([cmd.entityCmd.helmet]): {0} x{1}", entity.Equipment[5].Type, entity.Equipment[5].Count);
                                            if (entity.Equipment.ContainsKey(4) && entity.Equipment[4] != null)
                                                done += Translations.Replace("\n   [MCC] ([cmd.entityCmd.chestplate]): {0} x{1}", entity.Equipment[4].Type, entity.Equipment[4].Count);
                                            if (entity.Equipment.ContainsKey(3) && entity.Equipment[3] != null)
                                                done += Translations.Replace("\n   [MCC] ([cmd.entityCmd.leggings]): {0} x{1}", entity.Equipment[3].Type, entity.Equipment[3].Count);
                                            if (entity.Equipment.ContainsKey(2) && entity.Equipment[2] != null)
                                                done += Translations.Replace("\n   [MCC] ([cmd.entityCmd.boots]): {0} x{1}", entity.Equipment[2].Type, entity.Equipment[2].Count);
                                        }
                                        done += Translations.Replace("\n [MCC] ([cmd.entityCmd.pose]): {0}", pose);
                                        done += Translations.Replace("\n [MCC] ([cmd.entityCmd.health]): {0}", color + health + "§8");
                                        done += Translations.Replace("\n [MCC] ([cmd.entityCmd.distance]): {0}", distance);
                                        done += Translations.Replace("\n [MCC] ([cmd.entityCmd.location]): {0}", location);
                                        return done;
                                }
                            }
                            else return Translations.Get("cmd.entityCmd.not_found");
                        }
                        else
                        {
                            EntityType interacttype = EntityType.Player;
                            Enum.TryParse(args[0], out interacttype);
                            string actionst = "cmd.entityCmd.attacked";
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
                                        handler.InteractEntity(entity2.Key, 1);
                                        actionst = "cmd.entityCmd.attacked";
                                        actioncount++;
                                    }
                                    else if (action == "use")
                                    {
                                        handler.InteractEntity(entity2.Key, 0);
                                        actionst = "cmd.entityCmd.used";
                                        actioncount++;
                                    }
                                    else return GetCmdDescTranslated();
                                }
                            }
                            return actioncount + " " + Translations.Get(actionst);
                        }
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else
                {
                    Dictionary<int, Entity> entities = handler.GetEntities();
                    List<string> response = new List<string>();
                    response.Add(Translations.Get("cmd.entityCmd.entities"));
                    foreach (var entity2 in entities)
                    {
                        int id = entity2.Key;
                        float health = entity2.Value.Health;
                        int latency = entity2.Value.Latency;
                        string nickname = entity2.Value.Name;
                        string customname = entity2.Value.CustomName;
                        EntityPose pose = entity2.Value.Pose;
                        EntityType type = entity2.Value.Type;
                        Item item = entity2.Value.Item;
                        string location = String.Format("X:{0}, Y:{1}, Z:{2}", Math.Round(entity2.Value.Location.X, 2), Math.Round(entity2.Value.Location.Y, 2), Math.Round(entity2.Value.Location.Z, 2));

                        if (type == EntityType.Item || type == EntityType.ItemFrame || type == EntityType.EyeOfEnder || type == EntityType.Egg || type == EntityType.EnderPearl || type == EntityType.Potion || type == EntityType.Fireball || type == EntityType.FireworkRocket)
                            response.Add(Translations.Replace(" #{0}: ([cmd.entityCmd.type]): {1}, ([cmd.entityCmd.item]): {2}, ([cmd.entityCmd.location]): {3}", id, type, item.Type, location));
                        else if (type == EntityType.Player && !String.IsNullOrEmpty(nickname))
                            response.Add(Translations.Replace(" #{0}: ([cmd.entityCmd.type]): {1}, ([cmd.entityCmd.nickname]): §8{2}§8, ([cmd.entityCmd.latency]): {3}, ([cmd.entityCmd.health]): {4}, ([cmd.entityCmd.pose]): {5}, ([cmd.entityCmd.location]): {6}", id, type, nickname, latency, health, pose, location));
                        else if (type == EntityType.Player && !String.IsNullOrEmpty(customname))
                            response.Add(Translations.Replace(" #{0}: ([cmd.entityCmd.type]): {1}, ([cmd.entityCmd.customname]): §8{2}§8, ([cmd.entityCmd.latency]): {3}, ([cmd.entityCmd.health]): {4}, ([cmd.entityCmd.pose]): {5}, ([cmd.entityCmd.location]): {6}", id, type, customname.Replace("&", "§"), latency, health, pose, location));
                        else
                            response.Add(Translations.Replace(" #{0}: ([cmd.entityCmd.type]): {1}, ([cmd.entityCmd.health]): {2}, ([cmd.entityCmd.location]): {3}", id, type, health, location));
                    }
                    response.Add(GetCmdDescTranslated());
                    return String.Join("\n", response);
                }
            }
            else return Translations.Get("extra.entity_required");
        }
    }
}
