using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    public class Enchant : Command
    {
        public override string CmdName { get { return "enchant"; } }
        public override string CmdUsage { get { return "enchant <top|middle|bottom>"; } }
        public override string CmdDesc { get { return Translations.cmd_enchant_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("top")
                        .Executes(r => GetUsage(r.Source, "top")))
                    .Then(l => l.Literal("middle")
                        .Executes(r => GetUsage(r.Source, "middle")))
                    .Then(l => l.Literal("bottom")
                        .Executes(r => GetUsage(r.Source, "bottom")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Literal("top")
                    .Executes(r => DoEnchant(r.Source, slotId: 0)))
                .Then(l => l.Literal("middle")
                    .Executes(r => DoEnchant(r.Source, slotId: 1)))
                .Then(l => l.Literal("bottom")
                    .Executes(r => DoEnchant(r.Source, slotId: 2)))
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
                "top"       =>  GetCmdDescTranslated(),
                "middle"    =>  GetCmdDescTranslated(),
                "bottom"    =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int DoEnchant(CmdResult r, int slotId)
        {
            McClient handler = CmdResult.currentHandler!;
            Container? enchantingTable = null;

            foreach (var (id, container) in handler.GetInventories())
            {
                if (container.Type == ContainerType.Enchantment)
                {
                    enchantingTable = container;
                    break;
                }
            }

            if (enchantingTable == null)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_enchant_enchanting_table_not_opened);

            int[] emptySlots = enchantingTable.GetEmpytSlots();

            if (emptySlots.Contains(0))
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_enchant_enchanting_no_item);

            if (emptySlots.Contains(1))
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_enchant_enchanting_no_lapis);

            Item lapisSlot = enchantingTable.Items[1];

            if (lapisSlot.Type != ItemType.LapisLazuli || lapisSlot.Count < 3)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_enchant_enchanting_no_lapis);

            EnchantmentData? enchantment = handler.GetLastEnchantments();

            if (enchantment == null)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_enchant_no_enchantments);

            short requiredLevel = slotId switch
            {
                0 => enchantment.TopEnchantmentLevelRequirement,
                1 => enchantment.MiddleEnchantmentLevelRequirement,
                2 => enchantment.BottomEnchantmentLevelRequirement,
                _ => 9999
            };

            if (handler.GetLevel() < requiredLevel)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_enchant_no_levels, handler.GetLevel(), requiredLevel));
            else
            {
                if (handler.ClickContainerButton(enchantingTable.ID, slotId))
                    return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_enchant_clicked);
                else
                    return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_enchant_not_clicked);
            }
        }
    }
}
