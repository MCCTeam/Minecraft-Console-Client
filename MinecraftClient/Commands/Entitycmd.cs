using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    class Entitycmd : Command
    {
        public override string CmdName { get { return "entity"; } }
        public override string CmdUsage { get { return "entity [near] <id|entitytype> <attack|use>"; } }
        public override string CmdDesc { get { return string.Empty; } }

        private enum ActionType { Attack, Use, List };

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("near")
                        .Executes(r => GetUsage(r.Source, "near")))
                    .Then(l => l.Literal("attack")
                        .Executes(r => GetUsage(r.Source, "attack")))
                    .Then(l => l.Literal("use")
                        .Executes(r => GetUsage(r.Source, "use")))
                    .Then(l => l.Literal("list")
                        .Executes(r => GetUsage(r.Source, "list")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => GetFullEntityList(r.Source))
                .Then(l => l.Literal("near")
                    .Executes(r => GetClosetEntityList(r.Source))
                    .Then(l => l.Argument("EntityID", Arguments.Integer())
                        .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.List))
                        .Then(l => l.Literal("attack")
                            .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.Attack)))
                        .Then(l => l.Literal("use")
                            .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.Use)))
                        .Then(l => l.Literal("list")
                            .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.List))))
                    .Then(l => l.Argument("EntityType", MccArguments.EntityType())
                        .Executes(r => OperateWithType(r.Source, true, MccArguments.GetEntityType(r, "EntityType"), ActionType.List))
                        .Then(l => l.Literal("attack")
                            .Executes(r => OperateWithType(r.Source, near: true, MccArguments.GetEntityType(r, "EntityType"), ActionType.Attack)))
                        .Then(l => l.Literal("use")
                            .Executes(r => OperateWithType(r.Source, near: true, MccArguments.GetEntityType(r, "EntityType"), ActionType.Use)))
                        .Then(l => l.Literal("list")
                            .Executes(r => OperateWithType(r.Source, near: true, MccArguments.GetEntityType(r, "EntityType"), ActionType.List)))))
                .Then(l => l.Argument("EntityID", Arguments.Integer())
                    .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.List))
                    .Then(l => l.Literal("attack")
                        .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.Attack)))
                    .Then(l => l.Literal("use")
                        .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.Use)))
                    .Then(l => l.Literal("list")
                        .Executes(r => OperateWithId(r.Source, Arguments.GetInteger(r, "EntityID"), ActionType.List))))
                .Then(l => l.Argument("EntityType", MccArguments.EntityType())
                    .Executes(r => OperateWithType(r.Source, true, MccArguments.GetEntityType(r, "EntityType"), ActionType.List))
                    .Then(l => l.Literal("attack")
                        .Executes(r => OperateWithType(r.Source, near: false, MccArguments.GetEntityType(r, "EntityType"), ActionType.Attack)))
                    .Then(l => l.Literal("use")
                        .Executes(r => OperateWithType(r.Source, near: false, MccArguments.GetEntityType(r, "EntityType"), ActionType.Use)))
                    .Then(l => l.Literal("list")
                        .Executes(r => OperateWithType(r.Source, near: false, MccArguments.GetEntityType(r, "EntityType"), ActionType.List))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "near"      =>  GetCmdDescTranslated(),
                "attack"    =>  GetCmdDescTranslated(),
                "use"       =>  GetCmdDescTranslated(),
                "list"      =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int GetFullEntityList(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetEntityHandlingEnabled())
                return r.SetAndReturn(Status.FailNeedEntity);

            Dictionary<int, Entity> entities = handler.GetEntities();
            StringBuilder response = new();
            response.AppendLine(Translations.cmd_entityCmd_entities);
            foreach (var entity2 in entities)
                response.AppendLine(GetEntityInfoShort(entity2.Value));
            response.Append(GetCmdDescTranslated());
            handler.Log.Info(response.ToString());

            return r.SetAndReturn(Status.Done);
        }

        private int GetClosetEntityList(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetEntityHandlingEnabled())
                return r.SetAndReturn(Status.FailNeedEntity);

            if (TryGetClosetEntity(handler.GetEntities(), handler.GetCurrentLocation(), null, out Entity? closest))
            {
                handler.Log.Info(GetEntityInfoDetailed(handler, closest));
                return r.SetAndReturn(Status.Done);
            }
            else
                return r.SetAndReturn(Status.Fail, Translations.cmd_entityCmd_not_found);
        }

        private int OperateWithId(CmdResult r, int entityID, ActionType action)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetEntityHandlingEnabled())
                return r.SetAndReturn(Status.FailNeedEntity);

            if (handler.GetEntities().TryGetValue(entityID, out Entity? entity))
            {
                handler.Log.Info(InteractionWithEntity(handler, entity, action));
                return r.SetAndReturn(Status.Done);
            }
            else
                return r.SetAndReturn(Status.Fail, Translations.cmd_entityCmd_not_found);
        }

        private int OperateWithType(CmdResult r, bool near, EntityType entityType, ActionType action)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetEntityHandlingEnabled())
                return r.SetAndReturn(Status.FailNeedEntity);

            if (near)
            {
                if (TryGetClosetEntity(handler.GetEntities(), handler.GetCurrentLocation(), entityType, out Entity? closest))
                {
                    handler.Log.Info(InteractionWithEntity(handler, closest, action));
                    return r.SetAndReturn(Status.Done);
                }
                else
                    return r.SetAndReturn(Status.Fail, Translations.cmd_entityCmd_not_found);
            }
            else
            {
                if (action == ActionType.Attack || action == ActionType.Use)
                {
                    string actionst = Translations.cmd_entityCmd_attacked;
                    int actioncount = 0;
                    foreach (var entity2 in handler.GetEntities())
                    {
                        if (entity2.Value.Type == entityType)
                        {
                            if (action == ActionType.Attack)
                            {
                                handler.InteractEntity(entity2.Key, InteractType.Attack);
                                actionst = Translations.cmd_entityCmd_attacked;
                            }
                            else if (action == ActionType.Use)
                            {
                                handler.InteractEntity(entity2.Key, InteractType.Interact);
                                actionst = Translations.cmd_entityCmd_used;
                            }
                            actioncount++;
                        }
                    }
                    handler.Log.Info(actioncount + " " + actionst);
                    return r.SetAndReturn(Status.Done);
                }
                else
                {
                    StringBuilder response = new();
                    response.AppendLine(Translations.cmd_entityCmd_entities);
                    foreach (var entity2 in handler.GetEntities())
                    {
                        if (entity2.Value.Type == entityType)
                        {
                            response.AppendLine(GetEntityInfoShort(entity2.Value));
                        }
                    }
                    response.Append(GetCmdDescTranslated());
                    handler.Log.Info(response.ToString());
                    return r.SetAndReturn(Status.Done);
                }
            }
        }

        private static string GetEntityInfoShort(Entity entity)
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
                return $" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_item}: {item.GetTypeString()}, {Translations.cmd_entityCmd_location}: {location}";
            else if (type == EntityType.Player && !string.IsNullOrEmpty(nickname))
                return $" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_nickname}: §8{nickname}§8, {Translations.cmd_entityCmd_latency}: {latency}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_pose}: {pose}, {Translations.cmd_entityCmd_location}: {location}";
            else if (type == EntityType.Player && !string.IsNullOrEmpty(customname))
                return $" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_customname}: §8{customname.Replace("&", "§")}§8, {Translations.cmd_entityCmd_latency}: {latency}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_pose}: {pose}, {Translations.cmd_entityCmd_location}: {location}";
            else
                return $" #{id}: {Translations.cmd_entityCmd_type}: {entity.GetTypeString()}, {Translations.cmd_entityCmd_health}: {health}, {Translations.cmd_entityCmd_location}: {location}";
        }

        private static string GetEntityInfoDetailed(McClient handler, Entity entity)
        {
            StringBuilder sb = new();
            int id = entity.ID;
            float health = entity.Health;
            int latency = entity.Latency;
            Item item = entity.Item;
            string? nickname = entity.Name;
            string? customname = entity.CustomName;
            EntityPose pose = entity.Pose;
            EntityType type = entity.Type;
            double distance = Math.Round(entity.Location.Distance(handler.GetCurrentLocation()), 2);
            string location = $"X:{Math.Round(entity.Location.X, 2)}, Y:{Math.Round(entity.Location.Y, 2)}, Z:{Math.Round(entity.Location.Z, 2)}";

            string color = "§a"; // Green
            if (health < 10)
                color = "§c";  // Red
            else if (health < 15)
                color = "§e";  // Yellow

            sb.Append($"{Translations.cmd_entityCmd_entity}: {id}\n [MCC] Type: {entity.GetTypeString()}");

            if (!string.IsNullOrEmpty(nickname))
                sb.Append($"\n [MCC] {Translations.cmd_entityCmd_nickname}: {nickname}");
            else if (!string.IsNullOrEmpty(customname))
                sb.Append($"\n [MCC] {Translations.cmd_entityCmd_customname}: {customname.Replace("&", "§")}§8");

            if (type == EntityType.Player)
            {
                sb.Append($"\n [MCC] {Translations.cmd_entityCmd_latency}: {latency}");
            }
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

            return sb.ToString();
        }

        private static bool TryGetClosetEntity(Dictionary<int, Entity> entities, Location location, EntityType? entityType, [NotNullWhen(true)] out Entity? closest)
        {
            closest = null;
            bool find = false;
            double closest_distance = double.PositiveInfinity;
            foreach (var entity in entities)
            {
                if (entityType.HasValue && entity.Value.Type != entityType)
                    continue;

                double distance = Math.Round(entity.Value.Location.Distance(location), 2);
                if (distance < closest_distance)
                {
                    find = true;
                    closest = entity.Value;
                    closest_distance = distance;
                }
            }
            return find;
        }

        private static string InteractionWithEntity(McClient handler, Entity entity, ActionType action)
        {
            switch (action)
            {
                case ActionType.Attack:
                    handler.InteractEntity(entity.ID, InteractType.Attack);
                    return Translations.cmd_entityCmd_attacked;
                case ActionType.Use:
                    bool shouldInteractAt = entity.Type == EntityType.ArmorStand ||
                                            entity.Type == EntityType.ChestMinecart ||
                                            entity.Type == EntityType.ChestBoat;
                    
                    handler.InteractEntity(entity.ID, shouldInteractAt ? InteractType.InteractAt : InteractType.Interact);
                    return Translations.cmd_entityCmd_used;
                case ActionType.List:
                    return GetEntityInfoDetailed(handler, entity);
                default:
                    goto case ActionType.List;
            }
        }
    }
}