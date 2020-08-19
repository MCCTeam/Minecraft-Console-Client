using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class Entitycmd : Command
    {
        public override string CMDName { get { return "entity"; } }
        public override string CMDDesc { get { return "entity <id> <list|attack|use>"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetEntityHandlingEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length >= 1)
                {
                    try
                    {
                        int entityID;
                        entityID = int.Parse(args[0]);
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
                    catch (FormatException) { return CMDDesc; }
                }
                else
                {
                    Dictionary<int, Mapping.Entity> entities = handler.GetEntities();
                    List<string> response = new List<string>();
                    response.Add("Entities:");
                    foreach (var entity2 in entities)
                    {
                        if (entity2.Value.Type == Mapping.EntityType.Item || entity2.Value.Type == Mapping.EntityType.ItemFrame)
                            response.Add(String.Format(" #{0}: Type: {1}, Item: {2}", entity2.Key, entity2.Value.Type, entity2.Value.Item.Type));
                        else if (entity2.Value.Type == Mapping.EntityType.Player && entity2.Value.Name != string.Empty)
                            response.Add(String.Format(" #{0}: Type: {1}, Nickname: {2}, Latency: {3}, Health: {4}, Pose: {5}", entity2.Key, entity2.Value.Type, entity2.Value.Name, entity2.Value.Latency, entity2.Value.Health, entity2.Value.Pose));
                        else
                            response.Add(String.Format(" #{0}: Type: {1}, Health: {2}", entity2.Key, entity2.Value.Type, entity2.Value.Health));
                    }
                    response.Add(CMDDesc);
                    return String.Join("\n", response);
                }
            }
            else return "Please enable entityhandling in config to use this command.";
        }
    }
}
