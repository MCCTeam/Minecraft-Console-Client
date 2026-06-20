using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    class UseItem : Command
    {
        public override string CmdName { get { return "useitem"; } }
        public override string CmdUsage { get { return "useitem [mainhand|offhand] | useitem [x] [y] [z] [mainhand|offhand]"; } }
        public override string CmdDesc { get { return Translations.cmd_useitem_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoUseItem(r.Source))
                .Then(l => l.Literal("mainhand")
                    .Executes(r => DoUseItem(r.Source, Hand.MainHand)))
                .Then(l => l.Literal("offhand")
                    .Executes(r => DoUseItem(r.Source, Hand.OffHand)))
                .Then(l => l.Argument("Location", MccArguments.Location())
                    .Executes(r => DoUseItemAtLocation(r.Source, MccArguments.GetLocation(r, "Location"), Hand.MainHand))
                    .Then(l => l.Literal("mainhand")
                        .Executes(r => DoUseItemAtLocation(r.Source, MccArguments.GetLocation(r, "Location"), Hand.MainHand)))
                    .Then(l => l.Literal("offhand")
                        .Executes(r => DoUseItemAtLocation(r.Source, MccArguments.GetLocation(r, "Location"), Hand.OffHand))))
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

        private static bool ShouldUseOffhandFood(McClient handler)
        {
            Container? inventory = handler.GetInventory(0);
            if (inventory is null)
                return false;

            if (!inventory.Items.TryGetValue(45, out Item? offhandItem)
                || offhandItem.IsEmpty
                || !offhandItem.Type.IsFood())
                return false;

            int mainHandSlot = 36 + handler.GetCurrentSlot();
            return !inventory.Items.TryGetValue(mainHandSlot, out Item? mainHandItem)
                || mainHandItem.IsEmpty
                || !mainHandItem.Type.IsFood();
        }

        private int DoUseItem(CmdResult r, Hand? requestedHand = null)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(Status.FailNeedInventory);

            Hand hand = requestedHand ?? (ShouldUseOffhandFood(handler) ? Hand.OffHand : Hand.MainHand);
            bool useOffhandFood = !requestedHand.HasValue && hand == Hand.OffHand;

            if (!useOffhandFood && handler.GetTerrainEnabled())
            {
                const double maxDistance = 4.5;
                var raycast = RaycastHelper.RaycastBlock(handler, maxDistance, false);
                if (raycast.Item1 && raycast.Item3.Type != Material.Air)
                {
                    handler.PlaceBlock(raycast.Item2, Direction.Up, hand, lookAtBlock: true);
                    handler.DoAnimation((int)hand);
                    return r.SetAndReturn(Status.Done, Translations.cmd_useitem_use);
                }
            }

            if (hand == Hand.OffHand)
                handler.UseItemOnLeftHand();
            else
                handler.UseItemOnHand();

            return r.SetAndReturn(Status.Done, Translations.cmd_useitem_use);
        }

        private int DoUseItemAtLocation(CmdResult r, Location block, Hand hand)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location current = handler.GetCurrentLocation();
            block = block.ToAbsolute(current).ToFloor();
            handler.PlaceBlock(block, Direction.Up, hand, lookAtBlock: true);
            handler.DoAnimation((int)hand);
            return r.SetAndReturn(Status.Done, Translations.cmd_useitem_use);
        }

    }
}
