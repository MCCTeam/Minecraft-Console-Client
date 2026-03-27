using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class BrewingStandView : ContainerViewBase
    {
        private readonly BrewingViewModel _brewVm;

        public BrewingStandView(McClient handler, int windowId)
            : base(new BrewingViewModel(handler, windowId))
        {
            _brewVm = (BrewingViewModel)_vm;
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
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var topRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 0,
            };

            var fuelCol = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            fuelCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_brewing_fuel,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            fuelCol.Children.Add(CreateSlotCell(_brewVm.FuelSlot, 0, 0));
            topRow.Children.Add(fuelCol);

            topRow.Children.Add(new Border { Width = 2 });

            var ingredientCol = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            ingredientCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_brewing_ingredient,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            ingredientCol.Children.Add(CreateSlotCell(_brewVm.IngredientSlot, 0, 1));
            topRow.Children.Add(ingredientCol);

            panel.Children.Add(topRow);

            panel.Children.Add(new TextBlock
            {
                Text = "\u25bc",
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            var bottleRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 0,
            };
            for (int i = 0; i < 3; i++)
            {
                var bottlePanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                };
                bottlePanel.Children.Add(new TextBlock
                {
                    Text = string.Format(Translations.tui_brewing_bottle, i + 1),
                    Foreground = BrEquipLbl,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                bottlePanel.Children.Add(CreateSlotCell(_brewVm.BottleSlots[i], 1, i));
                bottleRow.Children.Add(bottlePanel);
            }

            panel.Children.Add(bottleRow);

            return panel;
        }
    }

    public class BrewingViewModel : ContainerViewModel
    {
        public ObservableCollection<SlotViewModel> BottleSlots { get; } = new();
        public SlotViewModel IngredientSlot { get; private set; } = null!;
        public SlotViewModel FuelSlot { get; private set; } = null!;

        public BrewingViewModel(McClient handler, int windowId)
            : base(handler, windowId, ContainerType.BrewingStand)
        {
            IngredientSlot = SlotMap[3];
            FuelSlot = SlotMap[4];
        }

        protected override void InitializeSlots()
        {
            SlotMap.Clear();

            for (int i = 0; i <= 2; i++)
            {
                var slot = new SlotViewModel(i);
                BottleSlots.Add(slot);
                SlotMap[i] = slot;
            }

            SlotMap[3] = new SlotViewModel(3);
            SlotMap[4] = new SlotViewModel(4);

            for (int i = 5; i <= 31; i++)
            {
                var slot = new SlotViewModel(i);
                MainInventorySlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 32; i <= 40; i++)
            {
                int hotbarIdx = i - 32;
                var slot = new SlotViewModel(i, isHotbar: true, hotbarIndex: hotbarIdx);
                HotbarSlots.Add(slot);
                SlotMap[i] = slot;
            }
        }
    }
}
