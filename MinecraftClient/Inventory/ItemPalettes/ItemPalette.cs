using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory.ItemPalettes
{
    public abstract class ItemPalette
    {
        protected abstract Dictionary<int, ItemType> GetDict();
        private readonly Dictionary<ItemType, int> DictReverse = new Dictionary<ItemType, int>();

        public ItemPalette()
        {
            // Index reverse mappings for use in ToId()
            foreach (KeyValuePair<int, ItemType> entry in GetDict())
                DictReverse.Add(entry.Value, entry.Key);

            DictReverse[ItemType.Unknown] = -2;
            DictReverse[ItemType.Null] = -1;
        }

        public ItemType FromId(int id)
        {
            // Unknown item types may appear of Forge servers for custom items
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
