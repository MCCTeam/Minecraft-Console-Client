using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;
using MinecraftClient.Mapping;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class PlayerNameArgumentType : ArgumentType<string>
    {
        public override string Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            return reader.ReadString();
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            McClient? client = CmdResult.currentHandler;
            if (client != null)
            {
                var entityList = client.GetEntities().Values.ToList();
                foreach (var entity in entityList)
                {
                    if (entity.Type != EntityType.Player || string.IsNullOrWhiteSpace(entity.Name))
                        continue;
                    builder.Suggest(entity.Name);
                }
                builder.Suggest(client.GetUsername());
            }
            return builder.BuildFuture();
        }
    }
}
