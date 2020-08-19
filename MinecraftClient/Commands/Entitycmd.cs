using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;
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
                        if (entityID != 0)
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
                            Dictionary<int, Mapping.Entity> entities = handler.GetEntities();
                            string actionst = "Entity attacked";
                            int actioncount = 0;
                            foreach (var entity2 in entities)
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
                        if (entity2.Value.Type == EntityType.Item || entity2.Value.Type == EntityType.ItemFrame || entity2.Value.Type == Mapping.EntityType.EyeOfEnder || entity2.Value.Type == Mapping.EntityType.Egg || entity2.Value.Type == Mapping.EntityType.EnderPearl || entity2.Value.Type == Mapping.EntityType.Potion || entity2.Value.Type == Mapping.EntityType.Fireball || entity2.Value.Type == Mapping.EntityType.FireworkRocket)
                            response.Add(String.Format(" #{0}: Type: {1}, Item: {2}, Location: {3}", entity2.Key, entity2.Value.Type, entity2.Value.Item.Type, entity2.Value.Location));
                        else if (entity2.Value.Type == Mapping.EntityType.Player && entity2.Value.Name != string.Empty)
                            response.Add(String.Format(" #{0}: Type: {1}, Nickname: {2}, Latency: {3}, Health: {4}, Pose: {5}, Location: {6}", entity2.Key, entity2.Value.Type, entity2.Value.Name, entity2.Value.Latency, entity2.Value.Health, entity2.Value.Pose, entity2.Value.Location));
                        else
                            response.Add(String.Format(" #{0}: Type: {1}, Health: {2}, Location: {3}", entity2.Key, entity2.Value.Type, entity2.Value.Health, entity2.Value.Location));
                    }
                    response.Add(CMDDesc);
                    return String.Join("\n", response);
                }
            }
            else return "Please enable entityhandling in config to use this command.";
        }
    }
}
