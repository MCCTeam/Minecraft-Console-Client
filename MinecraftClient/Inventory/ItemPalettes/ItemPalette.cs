using System.Collections.Generic;

namespace MinecraftClient.Inventory.ItemPalettes
{
    public abstract class ItemPalette
    {
        protected abstract Dictionary<int, ItemType> GetDict();
        private readonly Dictionary<ItemType, int> DictReverse = new();

        public ItemPalette()
        {
            // Index reverse mappings for use in ToId()
            foreach (KeyValuePair<int, ItemType> entry in GetDict())
            {
                if (DictReverse.ContainsKey(entry.Value))
                    continue;
                
                DictReverse.Add(entry.Value, entry.Key);
            }

            // Hardcoded placeholder types for internal and network use
            DictReverse[ItemType.Unknown] = (int)ItemType.Unknown;
            DictReverse[ItemType.Null] = (int)ItemType.Null;
        }

        public ItemType FromId(int id)
        {
            // Unknown item types may appear on Forge servers for custom items
            if (!GetDict().ContainsKey(id))
                return ItemType.Unknown;

            return GetDict()[id];
        }

        public int ToId(ItemType itemType)
        {
            return DictReverse[itemType];
        }
    }
}
