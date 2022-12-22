using System;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;
using MinecraftClient.ChatBots;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class MapBotMapIdArgumentType : ArgumentType<int>
    {
        public override int Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            return reader.ReadInt();
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            McClient? client = CmdResult.currentHandler;
            if (client != null)
            {
                foreach (var bot in client.GetLoadedChatBots())
                {
                    if (bot.GetType() == typeof(Map))
                    {
                        var mapList = ((Map)bot).cachedMaps;
                        foreach (var map in mapList)
                        {
                            string mapName = map.Key.ToString();
                            if (mapName.StartsWith(builder.RemainingLowerCase, StringComparison.InvariantCultureIgnoreCase))
                                builder.Suggest(mapName);
                        }
                        break;
                    }
                }
            }
            return builder.BuildFuture();
        }
    }
}
