using System.Collections.Generic;

namespace MinecraftClient.Inventory.ItemPalettes
{
    /// <summary>
    /// Minimal flattened item palette for 1.13-1.14 book handling.
    /// </summary>
    public class ItemPalette113 : ItemPalette
    {
        private static readonly Dictionary<int, ItemType> mappings = new()
        {
            [0] = ItemType.Air,
            [687] = ItemType.WritableBook,
            [688] = ItemType.WrittenBook
        };

        protected override Dictionary<int, ItemType> GetDict()
        {
            return mappings;
        }
    }
}
