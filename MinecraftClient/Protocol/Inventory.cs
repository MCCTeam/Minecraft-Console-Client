using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol
{
    public class Inventory
    {
        public byte id { get; set; }
        public InventoryType type { get; set; }
        public string title { get; set; }
        public byte slots { get; set; }
        public Dictionary<Item, int> items { get; set; }

        public Inventory(byte id, InventoryType type, string title, byte slots)
        {
            this.id = id;
            this.type = type;
            this.title = title;
            this.slots = slots;
        }

    }
}
