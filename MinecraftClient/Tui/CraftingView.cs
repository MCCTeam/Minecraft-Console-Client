using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class CraftingView : ContainerViewBase
    {
        private readonly CraftingViewModel _craftVm;

        public CraftingView(McClient handler, int windowId)
            : base(new CraftingViewModel(handler, windowId))
        {
            _craftVm = (CraftingViewModel)_vm;
            Initialize();
        }

        protected override int GetTotalSlotRows()
        {
            return 3 + 3 + 1;
        }

        protected override Control BuildContainerSpecificArea()
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var gridPanel = new StackPanel { Spacing = 0 };
            gridPanel.Children.Add(new TextBlock
            {
                Text = Translations.tui_crafting_grid,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            gridPanel.Children.Add(BuildSlotGrid(_craftVm.CraftingGridSlots, 3));
            row.Children.Add(gridPanel);

            row.Children.Add(new TextBlock
            {
                Text = " \u2192 ",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var outPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            outPanel.Children.Add(new TextBlock
            {
                Text = Translations.tui_inventory_output,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            outPanel.Children.Add(CreateSlotCell(_craftVm.OutputSlot, 0, 0));
            row.Children.Add(outPanel);

            return row;
        }
    }

    public class CraftingViewModel : ContainerViewModel
    {
        public ObservableCollection<SlotViewModel> CraftingGridSlots { get; } = new();
        public SlotViewModel OutputSlot { get; private set; } = null!;

        public CraftingViewModel(McClient handler, int windowId)
            : base(handler, windowId, ContainerType.Crafting)
        {
            OutputSlot = SlotMap[0];
        }

        protected override void InitializeSlots()
        {
            SlotMap.Clear();

            var output = new SlotViewModel(0);
            SlotMap[0] = output;

            for (int i = 1; i <= 9; i++)
            {
                var slot = new SlotViewModel(i);
                CraftingGridSlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 10; i <= 36; i++)
            {
                var slot = new SlotViewModel(i);
                MainInventorySlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 37; i <= 45; i++)
            {
                int hotbarIdx = i - 37;
                var slot = new SlotViewModel(i, isHotbar: true, hotbarIndex: hotbarIdx);
                HotbarSlots.Add(slot);
                SlotMap[i] = slot;
            }
        }
    }
}
