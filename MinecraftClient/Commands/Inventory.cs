using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class Inventory : Command
    {
        public override string CmdName { get { return "inventory"; } }
        public override string CmdUsage { get { return GetBasicUsage(); } }
        public override string CmdDesc { get { return Translations.cmd_inventory_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("list")
                        .Executes(r => GetUsage(r.Source, "list")))
                    .Then(l => l.Literal("close")
                        .Executes(r => GetUsage(r.Source, "close")))
                    .Then(l => l.Literal("click")
                        .Executes(r => GetUsage(r.Source, "click")))
                    .Then(l => l.Literal("drop")
                        .Executes(r => GetUsage(r.Source, "drop")))
                    .Then(l => l.Literal("creativegive")
                        .Executes(r => GetUsage(r.Source, "creativegive")))
                    .Then(l => l.Literal("creativedelete")
                        .Executes(r => GetUsage(r.Source, "creativedelete")))
                    .Then(l => l.Literal("inventories")
                        .Executes(r => GetUsage(r.Source, "inventories")))
                    .Then(l => l.Literal("search")
                        .Executes(r => GetUsage(r.Source, "search")))
                    .Then(l => l.Literal("help")
                        .Executes(r => GetUsage(r.Source, "help")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => ListAllInventories(r.Source))
                .Then(l => l.Literal("creativegive")
                    .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                        .Then(l => l.Argument("ItemType", MccArguments.ItemType())
                            .Then(l => l.Argument("Count", Arguments.Integer(min: 1))
                                .Executes(r => DoCreativeGive(r.Source, Arguments.GetInteger(r, "Slot"), MccArguments.GetItemType(r, "ItemType"), Arguments.GetInteger(r, "Count")))))))
                .Then(l => l.Literal("creativedelete")
                    .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                        .Executes(r => DoCreativeDelete(r.Source, Arguments.GetInteger(r, "Slot")))))
                .Then(l => l.Literal("inventories")
                    .Executes(r => ListAvailableInventories(r.Source)))
                .Then(l => l.Literal("search")
                    .Then(l => l.Argument("ItemType", MccArguments.ItemType())
                        .Executes(r => SearchItem(r.Source, MccArguments.GetItemType(r, "ItemType"), null))
                        .Then(l => l.Argument("Count", Arguments.Integer(0, 64))
                            .Executes(r => SearchItem(r.Source, MccArguments.GetItemType(r, "ItemType"), Arguments.GetInteger(r, "Count"))))))
                .Then(l => l.Argument("InventoryId", MccArguments.InventoryId())
                    .Then(l => l.Literal("close")
                        .Executes(r => DoCloseAction(r.Source, Arguments.GetInteger(r, "InventoryId"))))
                    .Then(l => l.Literal("list")
                        .Executes(r => DoListAction(r.Source, Arguments.GetInteger(r, "InventoryId"))))
                    .Then(l => l.Literal("click")
                        .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                            .Executes(r => DoClickAction(r.Source, Arguments.GetInteger(r, "InventoryId"), Arguments.GetInteger(r, "Slot"), WindowActionType.LeftClick))
                            .Then(l => l.Argument("Action", MccArguments.InventoryAction())
                                .Executes(r => DoClickAction(r.Source, Arguments.GetInteger(r, "InventoryId"), Arguments.GetInteger(r, "Slot"), MccArguments.GetInventoryAction(r, "Action"))))))
                    .Then(l => l.Literal("drop")
                        .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                            .Executes(r => DoDropAction(r.Source, Arguments.GetInteger(r, "InventoryId"), Arguments.GetInteger(r, "Slot"), WindowActionType.DropItem))
                            .Then(l => l.Literal("all")
                                .Executes(r => DoDropAction(r.Source, Arguments.GetInteger(r, "InventoryId"), Arguments.GetInteger(r, "Slot"), WindowActionType.DropItemStack))))))
                .Then(l => l.Literal("player")
                    .Then(l => l.Literal("list")
                        .Executes(r => DoListAction(r.Source, inventoryId: 0)))
                    .Then(l => l.Literal("click")
                        .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                            .Executes(r => DoClickAction(r.Source, inventoryId: 0, Arguments.GetInteger(r, "Slot"), WindowActionType.LeftClick))
                            .Then(l => l.Argument("Action", MccArguments.InventoryAction())
                                .Executes(r => DoClickAction(r.Source, inventoryId: 0, Arguments.GetInteger(r, "Slot"), MccArguments.GetInventoryAction(r, "Action"))))))
                    .Then(l => l.Literal("drop")
                        .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                            .Executes(r => DoDropAction(r.Source, inventoryId: 0, Arguments.GetInteger(r, "Slot"), WindowActionType.DropItem))
                            .Then(l => l.Literal("all")
                                .Executes(r => DoDropAction(r.Source, inventoryId: 0, Arguments.GetInteger(r, "Slot"), WindowActionType.DropItemStack))))))
                .Then(l => l.Literal("container")
                    .Then(l => l.Literal("close")
                        .Executes(r => DoCloseAction(r.Source, inventoryId: null)))
                    .Then(l => l.Literal("list")
                        .Executes(r => DoListAction(r.Source, inventoryId: null)))
                    .Then(l => l.Literal("click")
                        .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                            .Executes(r => DoClickAction(r.Source, inventoryId: null, Arguments.GetInteger(r, "Slot"), WindowActionType.LeftClick))
                            .Then(l => l.Argument("Action", MccArguments.InventoryAction())
                                .Executes(r => DoClickAction(r.Source, inventoryId: null, Arguments.GetInteger(r, "Slot"), MccArguments.GetInventoryAction(r, "Action"))))))
                    .Then(l => l.Literal("drop")
                        .Then(l => l.Argument("Slot", MccArguments.InventorySlot())
                            .Executes(r => DoDropAction(r.Source, inventoryId: null, Arguments.GetInteger(r, "Slot"), WindowActionType.DropItem))
                            .Then(l => l.Literal("all")
                                .Executes(r => DoDropAction(r.Source, inventoryId: null, Arguments.GetInteger(r, "Slot"), WindowActionType.DropItemStack))))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            string usageStr = ' ' + Translations.cmd_inventory_help_usage + ": ";
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "list"           => Translations.cmd_inventory_help_list           + usageStr + "/inventory <player|container|<id>> list",
                "close"          => Translations.cmd_inventory_help_close          + usageStr + "/inventory <player|container|<id>> close",
                "click"          => Translations.cmd_inventory_help_click          + usageStr + "/inventory <player|container|<id>> click <slot> [left|right|middle|shift|shiftright]\nDefault is left click",
                "drop"           => Translations.cmd_inventory_help_drop           + usageStr + "/inventory <player|container|<id>> drop <slot> [all]\nAll means drop full stack",
                "creativegive"   => Translations.cmd_inventory_help_creativegive   + usageStr + "/inventory creativegive <slot> <itemtype> <amount>",
                "creativedelete" => Translations.cmd_inventory_help_creativedelete + usageStr + "/inventory creativedelete <slot>",
                "inventories"    => Translations.cmd_inventory_help_inventories    + usageStr + "/inventory inventories",
                "search"         => Translations.cmd_inventory_help_search         + usageStr + "/inventory search <item type> [count]",
                "help"           => GetCmdDescTranslated(),
                "action"         => Translations.cmd_inventory_help_unknown        + GetAvailableActions(),
                _                => GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int GetMaximumInventoryId(McClient handler)
        {
            List<int> availableIds = handler.GetInventories().Keys.ToList();
            return availableIds.Max(); // use foreground container
        }

        private int ListAllInventories(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
            {
                handler.Log.Info(Translations.extra_inventory_required);
                return -1;
            }
            StringBuilder response = new();
            response.Append(Translations.cmd_inventory_inventories).Append(":\n");
            foreach ((int invId, Container inv) in handler.GetInventories())
                response.AppendLine(String.Format(" #{0}: {1}§8", invId, inv.Title));
            response.Append(CmdUsage);
            handler.Log.Info(response.ToString());
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int DoCreativeGive(CmdResult r, int slot, ItemType itemType, int count)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (handler.GetGamemode() == 1)
            {
                if (handler.DoCreativeGive(slot, itemType, count, null))
                    return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_inventory_creative_done, itemType, count, slot));
                else
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_creative_fail);
            }
            else
            {
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_need_creative);
            }
        }

        private int DoCreativeDelete(CmdResult r, int slot)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (handler.GetGamemode() == 1)
            {
                if (handler.DoCreativeGive(slot, ItemType.Null, 0, null))
                    return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_inventory_creative_delete, slot));
                else
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_creative_fail);
            }
            else
            {
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_need_creative);
            }
        }

        private int ListAvailableInventories(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            Dictionary<int, Container> inventories = handler.GetInventories();
            List<int> availableIds = inventories.Keys.ToList();
            StringBuilder response = new();
            response.AppendLine(Translations.cmd_inventory_inventories_available);

            foreach (int id in availableIds)
                response.AppendLine(String.Format(" #{0} - {1}§8", id, inventories[id].Title));

            handler.Log.Info(response.ToString());
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int SearchItem(CmdResult r, ItemType itemType, int? itemCount)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            Dictionary<int, Container> inventories = handler.GetInventories();
            Dictionary<int, List<Item>> foundItems = new();

            List<Container> availableInventories = inventories.Values.ToList();

            availableInventories.ForEach(inventory =>
            {
                inventory.Items.Values
                    .ToList()
                    .FindAll(item => item.Type == itemType && (!itemCount.HasValue || item.Count == itemCount))
                    .ForEach(item =>
                    {
                        if (!foundItems.ContainsKey(inventory.ID))
                        {
                            foundItems.Add(inventory.ID, new List<Item>() { item });
                            return;
                        }

                        List<Item> invItems = foundItems[inventory.ID];
                        invItems.Add(item);
                        foundItems.Remove(inventory.ID);
                        foundItems.Add(inventory.ID, invItems);
                    });
            });

            if (foundItems.Count == 0)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_no_found_items);

            StringBuilder response = new();

            response.AppendLine(Translations.cmd_inventory_found_items + ":");

            foreach ((int invId, List<Item> itemsList) in new SortedDictionary<int, List<Item>>(foundItems))
            {
                if (itemsList.Count > 0)
                {
                    response.AppendLine(String.Format("{0} (#{1}):", inventories[invId].Title, invId));

                    foreach (Item item in itemsList)
                        response.AppendLine(String.Format("\t- {0}", item.ToFullString()));

                    response.AppendLine(" ");
                }
            }

            handler.Log.Info(response.ToString());
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int DoCloseAction(CmdResult r, int? inventoryId)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (!inventoryId.HasValue)
            {
                inventoryId = GetMaximumInventoryId(handler);
                if (inventoryId == 0)
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_container_not_found);
            }

            Container? inventory = handler.GetInventory(inventoryId.Value);
            if (inventory == null)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_inventory_not_exist, inventoryId));

            if (handler.CloseInventory(inventoryId.Value))
                return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_inventory_close, inventoryId));
            else
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_inventory_close_fail, inventoryId));
        }

        private int DoListAction(CmdResult r, int? inventoryId)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (!inventoryId.HasValue)
            {
                inventoryId = GetMaximumInventoryId(handler);
                if (inventoryId == 0)
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_container_not_found);
            }

            Container? inventory = handler.GetInventory(inventoryId.Value);
            if (inventory == null)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_inventory_not_exist, inventoryId));

            StringBuilder response = new();
            response.Append(Translations.cmd_inventory_inventory);
            response.AppendLine(String.Format(" #{0} - {1}§8", inventoryId, inventory.Title));

            string? asciiArt = inventory.Type.GetAsciiArt();
            if (asciiArt != null && Settings.Config.Main.Advanced.ShowInventoryLayout)
                response.AppendLine(asciiArt);

            int selectedHotbar = handler.GetCurrentSlot() + 1;
            foreach ((int itemId, Item item) in new SortedDictionary<int, Item>(inventory.Items))
            {
                bool isHotbar = inventory.IsHotbar(itemId, out int hotbar);
                string hotbarString = isHotbar ? (hotbar + 1).ToString() : " ";
                if ((hotbar + 1) == selectedHotbar)
                    hotbarString = ">" + hotbarString;
                response.AppendLine(String.Format("{0,2} | #{1,-2}: {2}", hotbarString, itemId, item.ToFullString()));
            }

            if (inventoryId == 0)
                response.AppendLine(string.Format(Translations.cmd_inventory_hotbar, (handler.GetCurrentSlot() + 1)));

            response.Remove(response.Length - 1, 1); // Remove last '\n'
            handler.Log.Info(response.ToString());
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int DoClickAction(CmdResult r, int? inventoryId, int slot, WindowActionType actionType)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (!inventoryId.HasValue)
            {
                inventoryId = GetMaximumInventoryId(handler);
                if (inventoryId == 0)
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_container_not_found);
            }

            Container? inventory = handler.GetInventory(inventoryId.Value);
            if (inventory == null)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_inventory_not_exist, inventoryId));

            string keyName = actionType switch
            {
                WindowActionType.LeftClick => Translations.cmd_inventory_left,
                WindowActionType.RightClick => Translations.cmd_inventory_right,
                WindowActionType.MiddleClick => Translations.cmd_inventory_middle,
                WindowActionType.ShiftClick => Translations.cmd_inventory_shiftclick,
                WindowActionType.ShiftRightClick => Translations.cmd_inventory_shiftrightclick,
                _ => "unknown",
            };

            handler.Log.Info(string.Format(Translations.cmd_inventory_clicking, keyName, slot, inventoryId));
            return r.SetAndReturn(handler.DoWindowAction(inventoryId.Value, slot, actionType));
        }

        private int DoDropAction(CmdResult r, int? inventoryId, int slot, WindowActionType actionType)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (!inventoryId.HasValue)
            {
                inventoryId = GetMaximumInventoryId(handler);
                if (inventoryId == 0)
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_inventory_container_not_found);
            }

            Container? inventory = handler.GetInventory(inventoryId.Value);
            if (inventory == null)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_inventory_not_exist, inventoryId));

            // check item exist
            if (!inventory.Items.ContainsKey(slot))
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_inventory_no_item, slot));

            if (handler.DoWindowAction(inventoryId.Value, slot, actionType))
            {
                if (actionType == WindowActionType.DropItemStack)
                    return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_inventory_drop_stack, slot));
                else
                    return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_inventory_drop, slot));
            }
            else
            {
                return r.SetAndReturn(CmdResult.Status.Fail, "Drop Failed");
            }
        }


        #region Methods for commands help

        private static string GetAvailableActions()
        {
            return Translations.cmd_inventory_help_available + ": list, close, click, drop, creativegive, creativedelete.";
        }

        private static string GetBasicUsage()
        {
            return Translations.cmd_inventory_help_basic + ": /inventory <player|container|<id>> <action>.";
        }

        #endregion
    }
}
