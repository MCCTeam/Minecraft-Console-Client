using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class Inventory : Command
    {
        public override string CmdName { get { return "inventory"; } }
        public override string CmdUsage { get { return GetBasicUsage(); } }
        public override string CmdDesc { get { return "cmd.inventory.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length >= 1)
                {
                    try
                    {
                        int inventoryId;
                        if (args[0].ToLower() == "creativegive")
                        {
                            if (args.Length >= 4)
                            {
                                int slot = int.Parse(args[1]);
                                ItemType itemType = ItemType.Stone;
                                if (Enum.TryParse(args[2], true, out itemType))
                                {
                                    if (handler.GetGamemode() == 1)
                                    {
                                        int count = int.Parse(args[3]);
                                        if (handler.DoCreativeGive(slot, itemType, count, null))
                                            return Translations.Get("cmd.inventory.creative_done", itemType, count, slot);
                                        else return Translations.Get("cmd.inventory.creative_fail");
                                    }
                                    else return Translations.Get("cmd.inventory.need_creative");
                                }
                                else
                                {
                                    return GetCmdDescTranslated();
                                }
                            }
                            else return GetCmdDescTranslated();
                        }
                        else if (args[0].ToLower().StartsWith("p"))
                        {
                            // player inventory is always ID 0
                            inventoryId = 0;
                        }
                        else if (args[0].ToLower().StartsWith("c"))
                        {
                            List<int> availableIds = handler.GetInventories().Keys.ToList();
                            availableIds.Remove(0); // remove player inventory ID from list
                            if (availableIds.Count == 1)
                                inventoryId = availableIds[0]; // one container, use it
                            else return Translations.Get("cmd.inventory.container_not_found");
                        }
                        else if (args[0].ToLower() == "help")
                        {
                            if (args.Length >= 2)
                            {
                                return GetSubCommandHelp(args[1]);
                            }
                            else return GetHelp();
                        }
                        else inventoryId = int.Parse(args[0]);
                        string action = args.Length > 1
                            ? args[1].ToLower()
                            : "list";
                        switch (action)
                        {
                            case "close":
                                if (handler.CloseInventory(inventoryId))
                                    return Translations.Get("cmd.inventory.close", inventoryId);
                                else return Translations.Get("cmd.inventory.close_fail", inventoryId);
                            case "list":
                                Container inventory = handler.GetInventory(inventoryId);
                                if(inventory==null)
                                    return Translations.Get("cmd.inventory.not_exist", inventoryId);
                                List<string> response = new List<string>();
                                response.Add(Translations.Get("cmd.inventory.inventory") + " #" + inventoryId + " - " + inventory.Title + "§8");
                                foreach (KeyValuePair<int, Item> item in inventory.Items)
                                {
                                    string displayName = item.Value.DisplayName;
                                    if (String.IsNullOrEmpty(displayName))
                                    {
                                        if (item.Value.Damage != 0)
                                            response.Add(String.Format(" #{0}: {1} x{2} | {3}: {4}", item.Key, item.Value.Type, item.Value.Count, Translations.Get("cmd.inventory.damage"), item.Value.Damage));
                                        else
                                            response.Add(String.Format(" #{0}: {1} x{2}", item.Key, item.Value.Type, item.Value.Count));
                                    }
                                    else
                                    {
                                        if (item.Value.Damage != 0)
                                            response.Add(String.Format(" #{0}: {1} x{2} - {3}§8 | {4}: {5}", item.Key, item.Value.Type, item.Value.Count, displayName, Translations.Get("cmd.inventory.damage"), item.Value.Damage));
                                        else
                                            response.Add(String.Format(" #{0}: {1} x{2} - {3}§8", item.Key, item.Value.Type, item.Value.Count, displayName));
                                    }
                                }
                                if (inventoryId == 0) response.Add(Translations.Get("cmd.inventory.hotbar", (handler.GetCurrentSlot() + 1)));
                                return String.Join("\n", response.ToArray());
                            case "click":
                                if (args.Length >= 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    WindowActionType actionType = WindowActionType.LeftClick;
                                    string keyName = "cmd.inventory.left";
                                    if (args.Length >= 4)
                                    {
                                        string b = args[3];
                                        if (b.ToLower()[0] == 'r')
                                        {
                                            actionType = WindowActionType.RightClick;
                                            keyName = "cmd.inventory.right";
                                        }
                                        if (b.ToLower()[0] == 'm')
                                        {
                                            actionType = WindowActionType.MiddleClick;
                                            keyName = "cmd.inventory.middle";
                                        }
                                    }
                                    handler.DoWindowAction(inventoryId, slot, actionType);
                                    return Translations.Get("cmd.inventory.clicking", Translations.Get(keyName), slot, inventoryId);
                                }
                                else return CmdUsage;
                            case "drop":
                                if (args.Length >= 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    // check item exist
                                    if (!handler.GetInventory(inventoryId).Items.ContainsKey(slot))
                                        return Translations.Get("cmd.inventory.no_item", slot);
                                    WindowActionType actionType = WindowActionType.DropItem;
                                    if (args.Length >= 4)
                                    {
                                        if (args[3].ToLower() == "all")
                                        {
                                            actionType = WindowActionType.DropItemStack;
                                        }
                                    }
                                    if (handler.DoWindowAction(inventoryId, slot, actionType))
                                    {
                                        if (actionType == WindowActionType.DropItemStack)
                                            return Translations.Get("cmd.inventory.drop_stack", slot);
                                        else return Translations.Get("cmd.inventory.drop", slot);
                                    }
                                    else
                                    {
                                        return "Failed";
                                    }
                                }
                                else return GetCmdDescTranslated();
                            default:
                                return GetCmdDescTranslated();
                        }
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else
                {
                    Dictionary<int, Container> inventories = handler.GetInventories();
                    List<string> response = new List<string>();
                    response.Add(Translations.Get("cmd.inventory.inventories") + ":");
                    foreach (KeyValuePair<int, Container> inventory in inventories)
                    {
                        response.Add(String.Format(" #{0}: {1}", inventory.Key, inventory.Value.Title + "§8"));
                    }
                    response.Add(CmdUsage);
                    return String.Join("\n", response);
                }
            }
            else return Translations.Get("extra.inventory_required");
        }

        #region Methods for commands help
        private string GetCommandDesc()
        {
            return GetBasicUsage() + " Type \"/inventory help\" for more help";
        }

        private string GetAvailableActions()
        {
            return Translations.Get("cmd.inventory.help.available") + ": list, close, click, drop.";
        }

        private string GetBasicUsage()
        {
            return Translations.Get("cmd.inventory.help.basic") + ": /inventory <player|container|<id>> <action>.";
        }

        private string GetHelp()
        {
            return Translations.Get("cmd.inventory.help.help", GetAvailableActions(), GetCreativeGiveHelp());
        }

        private string GetCreativeGiveHelp()
        {
            return Translations.Get("cmd.inventory.help.usage") + ": /inventory creativegive <slot> <itemtype> <count>";
        }

        private string GetSubCommandHelp(string cmd)
        {
            switch (cmd)
            {
                case "list":
                    return Translations.Get("cmd.inventory.help.list") + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> list";
                case "close":
                    return Translations.Get("cmd.inventory.help.close") + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> close";
                case "click":
                    return Translations.Get("cmd.inventory.help.click") + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> click <slot> [left|right|middle]. \nDefault is left click";
                case "drop":
                    return Translations.Get("cmd.inventory.help.drop") + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> drop <slot> [all]. \nAll means drop full stack";
                case "help":
                    return GetHelp();
                default:
                    return Translations.Get("cmd.inventory.help.unknown") + GetAvailableActions();
            }
        }
        #endregion
    }
}
