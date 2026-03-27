using Avalonia.Controls;
using Avalonia.Layout;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class HopperView : ContainerViewBase
    {
        public HopperView(McClient handler, int windowId)
            : base(new ContainerViewModel(handler, windowId, ContainerType.Hopper))
        {
            Initialize();
        }

        protected override int GetTotalSlotRows()
        {
            return 1 + 3 + 1;
        }

        protected override Control BuildContainerSpecificArea()
        {
            var grid = BuildSlotGrid(_vm.ContainerSlots, 5);
            return new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { grid },
            };
        }
    }
}
