using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    public static class ItemTypeExtensions
    {
        public static bool IsFood(this ItemType m)
        {
            ItemType[] t =
            {
                ItemType.Apple,
                ItemType.BakedPotato,
                ItemType.Beetroot,
                ItemType.Bread,
                ItemType.Carrot,
                ItemType.CookedChicken,
                ItemType.CookedCod,
                ItemType.CookedMutton,
                ItemType.CookedPorkchop,
                ItemType.CookedRabbit,
                ItemType.CookedSalmon,
                ItemType.Cookie,
                ItemType.DriedKelp,
                ItemType.EnchantedGoldenApple,
                ItemType.GoldenApple,
                ItemType.GoldenCarrot,
                ItemType.MelonSlice,
                ItemType.Potato,
                ItemType.PumpkinPie,
                ItemType.Beef,
                ItemType.Chicken,
                ItemType.Cod,
                ItemType.Mutton,
                ItemType.Porkchop,
                ItemType.Rabbit,
                ItemType.Salmon,
                ItemType.CookedBeef,
                ItemType.SweetBerries,
                ItemType.TropicalFish
            };
            return t.Contains(m);
        }
    }
}
