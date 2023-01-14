using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class BotNameArgumentType : ArgumentType<string>
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
                var botList = client.GetLoadedChatBots();
                foreach (var bot in botList)
                    builder.Suggest(bot.GetType().Name);
            }
            return builder.BuildFuture();
        }
    }
}
