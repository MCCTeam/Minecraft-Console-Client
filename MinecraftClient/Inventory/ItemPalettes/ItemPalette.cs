using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MinecraftClient.Inventory.ItemPalettes
{
    public abstract class ItemPalette
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<ItemType, int>> s_reverseCache = new();

        protected abstract Dictionary<int, ItemType> GetDict();
        private readonly Dictionary<ItemType, int> DictReverse;

        public ItemPalette()
        {
            DictReverse = s_reverseCache.GetOrAdd(GetType(), _ =>
            {
                var reverse = new Dictionary<ItemType, int>();
                // Index reverse mappings for use in ToId()
                foreach (KeyValuePair<int, ItemType> entry in GetDict())
                {
                    reverse.TryAdd(entry.Value, entry.Key);
                }

                // Hardcoded placeholder types for internal and network use
                reverse[ItemType.Unknown] = (int)ItemType.Unknown;
                reverse[ItemType.Null] = (int)ItemType.Null;
                return reverse;
            });
        }

        public ItemType FromId(int id)
        {
            // Unknown item types may appear on Forge servers for custom items
            return GetDict().TryGetValue(id, out ItemType itemType) ? itemType : ItemType.Unknown;
        }

        public int ToId(ItemType itemType)
        {
            return DictReverse[itemType];
        }
    }
}
