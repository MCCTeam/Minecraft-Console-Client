using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    public class Item
    {
        public int ID;
        public int Count;
        public int SlotID = -1; // which slot is this item at, -1 = not specified
        public Dictionary<string, object> NBT;

        public Item(int ID,int Count,int SlotID, Dictionary<string,object> NBT)
        {
            this.ID = ID;
            this.Count = Count;
            this.SlotID = SlotID;
            this.NBT = NBT;
        }
        public Item(int ID, int Count, int SlotID)
        {
            this.ID = ID;
            this.Count = Count;
            this.SlotID = SlotID;
        }
        public Item(int ID, int Count)
        {
            this.ID = ID;
            this.Count = Count;
        }
    }
}
