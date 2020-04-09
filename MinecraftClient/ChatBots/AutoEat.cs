using MinecraftClient.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    class AutoEat : ChatBot
    {
        byte LastSlot = 0;
        byte CurrentSlot;
        public static bool Eating = false;
        private int HungerThreshold = 6;

        public AutoEat(int Threshold)
        {
            HungerThreshold = Threshold;
        }

        public override void OnHealthUpdate(float health, int food)
        {
            if (food <= HungerThreshold || (food < 20 && health < 20))
            {
                Eating = true;
                FindFoodAndEat();
            }
            // keep eating until full
            if (food < 20 && Eating)
            {
                FindFoodAndEat();
            }
            if (food >= 20 && Eating)
            {
                Eating = false;
                ChangeSlot(LastSlot);
            }
        }

        public override void OnHeldItemChange(byte slot)
        {
            CurrentSlot = slot;
        }
        /// <summary>
        /// Try to find food in the hotbar and eat it
        /// </summary>
        /// <returns>True if found</returns>
        public bool FindFoodAndEat()
        {
            Container inventory = GetPlayerInventory();
            bool found = false;
            LastSlot = CurrentSlot;
            if (inventory.Items.ContainsKey(CurrentSlot + 36) && inventory.Items[CurrentSlot + 36].Type.IsFood())
            {
                // no need to change slot
                found = true;
            }
            else
            {
                for (int i = 36; i <= 44; i++)
                {
                    if (!inventory.Items.ContainsKey(i)) continue;
                    if (inventory.Items[i].Type.IsFood())
                    {
                        int slot = i - 36;
                        ChangeSlot((short)slot);
                        found = true;
                        break;
                    }
                }
            }
            if (found) UseItemOnHand();
            return found;
        }
    }
}
