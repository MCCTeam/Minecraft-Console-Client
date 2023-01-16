using System;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class HotbarSlotArgumentType : ArgumentType<int>
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
                Inventory.Container? inventory = client.GetInventory(0);
                if (inventory != null)
                {
                    for (int i = 1; i <= 9; ++i)
                    {
                        if (inventory.Items.TryGetValue(i - 1 + 36, out Inventory.Item? item))
                        {
                            string slotStr = i.ToString();
                            if (slotStr.StartsWith(builder.RemainingLowerCase, StringComparison.InvariantCultureIgnoreCase))
                            {
                                string itemDesc = item.Count == 1 ? item.GetTypeString() : string.Format("{0}x{1}", item.Count, item.GetTypeString());
                                builder.Suggest(slotStr, new SuggestionTooltip(itemDesc));
                            }
                        }
                    }
                }
            }
            return builder.BuildFuture();
        }
    }
}
