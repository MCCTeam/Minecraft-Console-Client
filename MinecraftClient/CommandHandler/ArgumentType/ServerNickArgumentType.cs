using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class ServerNickArgumentType : ArgumentType<string>
    {
        public override string Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            return reader.ReadString();
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            var serverList = Settings.Config.Main.Advanced.ServerList;
            foreach (var server in serverList)
                builder.Suggest(server.Key);
            return builder.BuildFuture();
        }
    }
}
