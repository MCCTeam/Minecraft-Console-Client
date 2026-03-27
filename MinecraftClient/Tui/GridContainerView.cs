using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class GridContainerView : ContainerViewBase
    {
        private readonly int _gridRows;
        private readonly int _gridCols;

        public GridContainerView(McClient handler, int windowId, ContainerType type, int rows, int cols)
            : base(new ContainerViewModel(handler, windowId, type))
        {
            _gridRows = rows;
            _gridCols = cols;
            Initialize();
        }

        protected override int GetTotalSlotRows()
        {
            return _gridRows + 3 + 1;
        }

        protected override Control BuildContainerSpecificArea()
        {
            return BuildSlotGrid(_vm.ContainerSlots, _gridCols);
        }
    }
}
