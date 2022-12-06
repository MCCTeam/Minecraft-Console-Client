using System;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using MinecraftClient.Mapping;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class EntityTypeArgumentType : ArgumentType<EntityType>
    {
        public override EntityType Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            string entity = reader.ReadString();
            if (Enum.TryParse(entity, true, out EntityType entityType))
                return entityType;
            else
                throw CommandSyntaxException.BuiltInExceptions.LiteralIncorrect().CreateWithContext(reader, entity);
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            foreach (var result in Enum.GetNames(typeof(EntityType)))
                builder.Suggest(result);
            return builder.BuildFuture();
        }
    }
}
