using System.Collections.Generic;

namespace MinecraftClient.Inventory.ItemPalettes
{
    /// <summary>
    /// Minimal flattened item palette for 1.14 book handling.
    /// </summary>
    public class ItemPalette114 : ItemPalette
    {
        private static readonly Dictionary<int, ItemType> mappings = new()
        {
            [0] = ItemType.Air,
            [757] = ItemType.WritableBook,
            [758] = ItemType.WrittenBook
        };

        protected override Dictionary<int, ItemType> GetDict()
        {
            return mappings;
        }
    }
}
