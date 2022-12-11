using System;
using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class InventorySlotArgumentType : ArgumentType<int>
    {
        public override int Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            return reader.ReadInt();
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            McClient? client = CmdResult.currentHandler;
            if (client != null && context.Nodes.Count >= 2)
            {
                string invName = context.Nodes[1].Range.Get(builder.Input);
                if (!int.TryParse(invName, out int invId))
                    invId = invName switch
                    {
                        "creativegive" => 0,
                        "creativedelete" => 0,
                        "player" => 0,
                        "container" => client.GetInventories().Keys.ToList().Max(),
                        _ => -1,
                    };

                Inventory.Container? inventory = client.GetInventory(invId);
                if (inventory != null)
                {
                    foreach ((int slot, Inventory.Item item) in inventory.Items)
                    {
                        if (item != null && item.Count > 0)
                        {
                            string slotStr = slot.ToString();
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
