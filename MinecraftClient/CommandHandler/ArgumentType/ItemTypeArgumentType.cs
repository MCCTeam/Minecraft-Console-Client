using System;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using MinecraftClient.Inventory;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class ItemTypeArgumentType : ArgumentType<ItemType>
    {
        public override ItemType Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            string entity = reader.ReadString();
            if (Enum.TryParse(entity, true, out ItemType itemType))
                return itemType;
            else
                throw CommandSyntaxException.BuiltInExceptions.LiteralIncorrect().CreateWithContext(reader, entity);
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            foreach (ItemType result in Enum.GetValues<ItemType>())
            {
                if (result == ItemType.Unknown || result == ItemType.Null)
                    continue;

                string name = result.ToString();
                string localName = Item.GetTypeString(result);
                bool same = true;
                for (int i = 0, j = 0; i < name.Length; ++i, ++j)
                {
                    while (j < localName.Length && localName[j] == ' ')
                        ++j;
                    if (j >= localName.Length || name[i] != localName[j])
                    {
                        same = false;
                        break;
                    }
                }
                if (same)
                    builder.Suggest(name);
                else
                    builder.Suggest(name, new SuggestionTooltip(localName));
            }
            return builder.BuildFuture();
        }
    }
}
