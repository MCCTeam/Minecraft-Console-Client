using System;
namespace MinecraftClient.Protocol
{
    public class Item
    {
        public int id;
        public int count;
        public int damage;
        public byte nbtData;

        public Item(int id, int count)
        {
            this.id = id;
            this.count = count;
            this.damage = 0;
        }

        public Item(int id, int damage, int count, byte nbtData)
        {
            this.id = id;
            this.count = count;
            this.damage = damage;
            this.nbtData = nbtData;
        }

    }
}
