using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class GrindstoneView : ContainerViewBase
    {
        private readonly GrindstoneViewModel _grindVm;

        public GrindstoneView(McClient handler, int windowId)
            : base(new GrindstoneViewModel(handler, windowId))
        {
            _grindVm = (GrindstoneViewModel)_vm;
            Initialize();
        }

        protected override int GetTotalSlotRows()
        {
            return 2 + 3 + 1;
        }

        protected override Control BuildContainerSpecificArea()
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 0,
            };

            var inputCol = new StackPanel
            {
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Center,
            };

            inputCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_grindstone_input1,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            inputCol.Children.Add(CreateSlotCell(_grindVm.Input1Slot, 0, 0));

            inputCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_grindstone_input2,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            inputCol.Children.Add(CreateSlotCell(_grindVm.Input2Slot, 1, 0));

            row.Children.Add(inputCol);

            row.Children.Add(new TextBlock
            {
                Text = "=>",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(1, 0),
            });

            var outCol = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            outCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_inventory_output,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            outCol.Children.Add(CreateSlotCell(_grindVm.OutputSlot, 0, 1));
            row.Children.Add(outCol);

            return row;
        }
    }

    public class GrindstoneViewModel : ContainerViewModel
    {
        public SlotViewModel Input1Slot { get; private set; } = null!;
        public SlotViewModel Input2Slot { get; private set; } = null!;
        public SlotViewModel OutputSlot { get; private set; } = null!;

        public GrindstoneViewModel(McClient handler, int windowId)
            : base(handler, windowId, ContainerType.Grindstone)
        {
            Input1Slot = SlotMap[0];
            Input2Slot = SlotMap[1];
            OutputSlot = SlotMap[2];
        }

        protected override void InitializeSlots()
        {
            SlotMap.Clear();

            SlotMap[0] = new SlotViewModel(0);
            SlotMap[1] = new SlotViewModel(1);
            SlotMap[2] = new SlotViewModel(2);

            for (int i = 3; i <= 29; i++)
            {
                var slot = new SlotViewModel(i);
                MainInventorySlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 30; i <= 38; i++)
            {
                int hotbarIdx = i - 30;
                var slot = new SlotViewModel(i, isHotbar: true, hotbarIndex: hotbarIdx);
                HotbarSlots.Add(slot);
                SlotMap[i] = slot;
            }
        }
    }
}
