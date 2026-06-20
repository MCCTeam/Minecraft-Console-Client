using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class FurnaceView : ContainerViewBase
    {
        private readonly FurnaceViewModel _furnaceVm;

        public FurnaceView(McClient handler, int windowId, ContainerType type)
            : base(new FurnaceViewModel(handler, windowId, type))
        {
            _furnaceVm = (FurnaceViewModel)_vm;
            Initialize();
        }

        protected override int GetTotalSlotRows()
        {
            return 3 + 3 + 1;
        }

        protected override Control BuildContainerSpecificArea()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 0,
            };

            var leftCol = new StackPanel
            {
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Center,
            };

            leftCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_furnace_input,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            leftCol.Children.Add(CreateSlotCell(_furnaceVm.InputSlot, 0, 0));

            leftCol.Children.Add(new TextBlock
            {
                Text = "\u2592\u2592\u2592",
                Foreground = new SolidColorBrush(Color.FromRgb(180, 100, 40)),
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            leftCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_furnace_fuel,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            leftCol.Children.Add(CreateSlotCell(_furnaceVm.FuelSlot, 1, 0));

            panel.Children.Add(leftCol);

            panel.Children.Add(new TextBlock
            {
                Text = " \u2192 ",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var rightCol = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            rightCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_furnace_output,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            rightCol.Children.Add(CreateSlotCell(_furnaceVm.OutputSlot, 0, 1));

            panel.Children.Add(rightCol);

            return panel;
        }
    }

    public class FurnaceViewModel : ContainerViewModel
    {
        public SlotViewModel InputSlot { get; private set; } = null!;
        public SlotViewModel FuelSlot { get; private set; } = null!;
        public SlotViewModel OutputSlot { get; private set; } = null!;

        public FurnaceViewModel(McClient handler, int windowId, ContainerType type)
            : base(handler, windowId, type)
        {
            InputSlot = SlotMap[0];
            FuelSlot = SlotMap[1];
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
