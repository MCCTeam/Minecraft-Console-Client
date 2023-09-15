using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    public class NameItem : Command
    {
        public override string CmdName => "nameitem";
        public override string CmdUsage => "nameitem <item name>";

        public override string CmdDesc => Translations.cmd_nameitem_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Argument("any", Arguments.GreedyString())
                    .Executes(r => DoSetItemName(r.Source, Arguments.GetString(r, "any"))))
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

        private int DoSetItemName(CmdResult r, string itemName)
        {
            var handler = CmdResult.currentHandler!;

            if (itemName.Trim().Length == 0)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_nameitem_item_name_empty);

            var currentInventory = handler.GetInventories().Count == 0
                ? null
                : handler.GetInventories().Values.ToList().Last();

            if (currentInventory is not { Type: ContainerType.Anvil })
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_nameitem_no_anvil_inventory_open);

            if (currentInventory.Items[0].IsEmpty)
                return r.SetAndReturn(CmdResult.Status.Fail,
                    Translations.cmd_nameitem_first_slot_empty);

            return handler.SendRenameItem(itemName)
                ? r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_nameitem_successful)
                : r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_nameitem_failed);
        }
    }
}