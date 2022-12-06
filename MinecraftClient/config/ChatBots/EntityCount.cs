//MCCScript 1.0

MCC.LoadBot(new EntityCount());

//MCCScript Extensions

class EntityCount : ChatBot
{
        public override void Initialize()
        {
            LogToConsole("Entity Count chat bot loaded!");
            RegisterChatBotCommand("entitycount", "Counts entities of a provided type", "entitycount <type> [<x> <y> <z>]", OnCommand);
        }

        public string OnCommand(string cmd, string[] args)
        {
            if (args.Length < 1)
                return "Invalid usage! Usage: /entitycount <type> [<x> <y> <z>]";

            if (!Enum.TryParse<EntityType>(args[0], out EntityType entityType))
                return "Invalid entity type provided!\nSee: https://bit.ly/3NgSIFu";

            Location? location = null;

            if (args.Length >= 4)
            {
                if (!Location.TryParse(GetCurrentLocation().ToFloor(), args[1], args[2], args[3], out location))
                    return "Invalid location provided, check your input!";
            }

            int counter = 0;

            foreach (var (id, entity) in GetEntities())
            {
                if (entity.Type == entityType)
                {
                    if (location != null)
                    {
                        if (entity.Location.ToFloor() != ((Location)location).ToFloor())
                            continue;
                    }

                    counter++;
                }
            }

            return $"Found {counter} of entity type: {args[0]}";
        }
}
