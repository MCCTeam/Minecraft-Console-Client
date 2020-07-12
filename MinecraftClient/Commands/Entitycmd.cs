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
        public override string CMDDesc { get { return "entity id <list|attack|use"; } }

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
                        if (entity2.Value.Type == Mapping.EntityType.Player)
                            response.Add(String.Format(" #{0}: {1} | {2}", entity2.Key, entity2.Value.Type, entity2.Value.Name));
                        else
                            response.Add(String.Format(" #{0}: {1}", entity2.Key, entity2.Value.Type));
                    }
                    response.Add(CMDDesc);
                    return String.Join("\n", response);
                }
            }
            else return "Please enable entityhandling in config to use this command.";
        }
    }
}
