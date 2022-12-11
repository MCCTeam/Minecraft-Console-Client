using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    class DropItem : Command
    {
        public override string CmdName { get { return "dropitem"; } }
        public override string CmdUsage { get { return "dropitem <itemtype>"; } }
        public override string CmdDesc { get { return Translations.cmd_dropItem_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("ItemType", MccArguments.ItemType())
                    .Executes(r => DoDropItem(r.Source, MccArguments.GetItemType(r, "ItemType"))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int DoDropItem(CmdResult r, ItemType itemType)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            int inventoryId;
            var inventories = handler.GetInventories();
            List<int> availableIds = inventories.Keys.ToList();
            availableIds.Remove(0); // remove player inventory ID from list
            if (availableIds.Count == 1)
                inventoryId = availableIds[0]; // one container, use it
            else
                inventoryId = 0;
            var p = inventories[inventoryId];
            int[] targetItems = p.SearchItem(itemType);
            foreach (int slot in targetItems)
                handler.DoWindowAction(inventoryId, slot, WindowActionType.DropItemStack);

            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_dropItem_dropped, Item.GetTypeString(itemType), inventoryId));
        }
    }
}
