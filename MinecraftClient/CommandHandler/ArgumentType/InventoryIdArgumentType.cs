using System;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class InventoryIdArgumentType : ArgumentType<int>
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
                var invList = client.GetInventories();
                foreach (var inv in invList)
                {
                    string invName = inv.Key.ToString();
                    if (invName.StartsWith(builder.RemainingLowerCase, StringComparison.InvariantCultureIgnoreCase))
                    {
                        string? invTitle = inv.Value.Title;
                        if (!string.IsNullOrWhiteSpace(invTitle))
                            builder.Suggest(invName, new SuggestionTooltip(invTitle));
                        else
                            builder.Suggest(invName);
                    }
                }
            }
            return builder.BuildFuture();
        }
    }
}
