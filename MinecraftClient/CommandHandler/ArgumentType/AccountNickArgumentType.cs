using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class AccountNickArgumentType : ArgumentType<string>
    {
        public override string Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            return reader.ReadString();
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            var accountList = Settings.Config.Main.Advanced.AccountList;
            foreach (var account in accountList)
                builder.Suggest(account.Key);
            return builder.BuildFuture();
        }
    }
}