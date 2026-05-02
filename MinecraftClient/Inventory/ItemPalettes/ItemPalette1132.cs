using System.Collections.Generic;

namespace MinecraftClient.Inventory.ItemPalettes
{
    /// <summary>
    /// Minimal flattened item palette for 1.13.2 book handling.
    /// </summary>
    public class ItemPalette1132 : ItemPalette
    {
        private static readonly Dictionary<int, ItemType> mappings = new()
        {
            [0] = ItemType.Air,
            [692] = ItemType.WritableBook,
            [693] = ItemType.WrittenBook
        };

        protected override Dictionary<int, ItemType> GetDict()
        {
            return mappings;
        }
    }
}
