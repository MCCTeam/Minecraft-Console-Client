using System;
using MinecraftClient.Inventory;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class AutoEat : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoEat";

            public bool Enabled = false;

            public int Threshold = 6;

            public void OnSettingUpdate()
            {
                if (Threshold > 20)
                    Threshold = 20;
                else if (Threshold < 0)
                    Threshold = 0;
            }
        }

        byte LastSlot = 0;
        public static bool Eating = false;
        private int DelayCounter = 0;

        public override void Update()
        {
            if (DelayCounter > 0)
            {
                DelayCounter--;
                if (DelayCounter == 0)
                {
                    Eating = FindFoodAndEat();
                    if (!Eating)
                        ChangeSlot(LastSlot);
                }
            }
        }

        public override void OnHealthUpdate(float health, int food)
        {
            if (health <= 0) return; // player dead
            if (((food <= Config.Threshold) || (food < 20 && health < 20)) && !Eating)
            {
                Eating = FindFoodAndEat();
                if (!Eating)
                    ChangeSlot(LastSlot);
            }
            // keep eating until full
            if (food < 20 && Eating)
            {
                // delay 300ms
                DelayCounter = 3;
            }
            if (food >= 20 && Eating)
            {
                Eating = false;
                ChangeSlot(LastSlot);
            }
        }

        /// <summary>
        /// Try to find food in the hotbar and eat it
        /// </summary>
        /// <returns>True if found</returns>
        public bool FindFoodAndEat()
        {
            Container inventory = GetPlayerInventory();
            bool found = false;
            byte CurrentSlot = GetCurrentSlot();
            if (!Eating)
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
            if (found) UseItemInHand();
            return found;
        }
    }
}
