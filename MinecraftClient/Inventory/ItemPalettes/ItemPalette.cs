using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory.ItemPalettes
{
    public abstract class ItemPalette
    {
        protected abstract Dictionary<int, ItemType> GetDict();

        public ItemType FromId(int id)
        {
            return GetDict()[id];
        }
    }
}
