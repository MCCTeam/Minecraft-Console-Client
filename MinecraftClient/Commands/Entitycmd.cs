using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    class Entitycmd : Command
    {
        public override string CMDName { get { return "entity"; } }
        public override string CMDDesc { get { return "entity <id|entitytype> <list|attack|use>"; } }

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
                        if (entityID != 0 && handler.GetEntities().ContainsKey(entityID))
                        {
                            string action = args.Length > 1
                                ? args[1].ToLower()
                                : "list";
                            switch (action)
                            {
                                case "attack":
                                    handler.InteractEntity(entityID, 1);
                                    return "Entity attacked";
                                case "use":
                                    handler.InteractEntity(entityID, 0);
                                    return "Entity used";
                                default:
                                    return CMDDesc;
                            }
                        }
                        else
                        {
                            EntityType interacttype = EntityType.Player;
                            Enum.TryParse(args[0], out interacttype);
                            string actionst = "Entity attacked";
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
                                        actionst = "Entity attacked";
                                        actioncount++;
                                    }
                                    else if (action == "use")
                                    {
                                        handler.InteractEntity(entity2.Key, 0);
                                        actionst = "Entity used";
                                        actioncount++;
                                    }
                                    else return CMDDesc;
                                }
                            }
                            return actioncount + " " + actionst;
                        }
                    }
                    catch (FormatException) { return CMDDesc; }
                }
                else
                {
                    Dictionary<int, Mapping.Entity> entities = handler.GetEntities();
                    List<string> response = new List<string>();
                    response.Add("Entities:");
                    foreach (var entity2 in entities)
                    {
                        int id = entity2.Key;
                        float health = entity2.Value.Health;
                        int latency = entity2.Value.Latency;
                        string nickname = entity2.Value.Name;
                        string customname = entity2.Value.CustomName;
                        EntityPose pose = entity2.Value.Pose;
                        EntityType type = entity2.Value.Type;
                        string location = String.Format("X:{0}, Y:{1}, Z:{2}", Math.Round(entity2.Value.Location.X, 2), Math.Round(entity2.Value.Location.Y, 2), Math.Round(entity2.Value.Location.Y, 2));

                        if (type == EntityType.Item || type == EntityType.ItemFrame || type == Mapping.EntityType.EyeOfEnder || type == Mapping.EntityType.Egg || type == Mapping.EntityType.EnderPearl || type == Mapping.EntityType.Potion || type == Mapping.EntityType.Fireball || type == Mapping.EntityType.FireworkRocket)
                            response.Add(String.Format(" #{0}: Type: {1}, Item: {2}, Location: {3}", id, type, entity2.Value.Item.Type, location));
                        else if (type == Mapping.EntityType.Player && nickname != string.Empty && customname == string.Empty)
                            response.Add(String.Format(" #{0}: Type: {1}, Nickname: §8{2}§8, Latency: {3}, Health: {4}, Pose: {5}, Location: {6}", id, type, nickname, latency, health, pose, location));
                        else if (type == Mapping.EntityType.Player && customname != string.Empty)
                            response.Add(String.Format(" #{0}: Type: {1}, Nickname: §8{2}§8, Latency: {3}, Health: {4}, Pose: {5}, Location: {6}", id, type, customname, latency, health, pose, location));
                        else
                            response.Add(String.Format(" #{0}: Type: {1}, Health: {2}, Location: {3}", id, type, health, location));
                    }
                    response.Add(CMDDesc);
                    return String.Join("\n", response);
                }
            }
            else return "Please enable entityhandling in config to use this command.";
        }
    }
}
