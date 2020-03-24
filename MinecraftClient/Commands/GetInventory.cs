﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class GetInventory : Command
    {
        public override string CMDName { get { return "getinventory"; } }
        public override string CMDDesc { get { return "getinventory: Show your inventory."; } }

        public override string Run(McTcpClient handler, string command)
        {
            Dictionary<int,Item> items = handler.GetPlayerInventory().Items;
            foreach(KeyValuePair<int,Item> a in items)
            {
                ConsoleIO.WriteLine("Slot: "+a.Key+" ItemID: " + a.Value.ID + ", Count: " + a.Value.Count);
            }
            return "";
        }
    }
}
