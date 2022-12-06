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
            foreach (var result in Enum.GetNames(typeof(ItemType)))
                builder.Suggest(result);
            return builder.BuildFuture();
        }
    }
}
