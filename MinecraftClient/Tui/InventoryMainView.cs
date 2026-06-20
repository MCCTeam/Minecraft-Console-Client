using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MinecraftClient.Tui
{
    public class PlayerInventoryView : ContainerViewBase
    {
        private readonly PlayerInventoryViewModel _playerVm;
        private int _topGap;

        public PlayerInventoryView(McClient handler, int windowId)
            : base(new PlayerInventoryViewModel(handler, windowId))
        {
            _playerVm = (PlayerInventoryViewModel)_vm;
            Initialize();
        }

        protected override int GetTotalSlotRows()
        {
            return 6;
        }

        protected override void RebuildUi()
        {
            int availW = 0;
            try { availW = System.Console.WindowWidth - 26; } catch { availW = 94; }
            int slotW = System.Math.Clamp(availW / 9, 8, 18);
            int topUsedW = slotW * 4 + 8 + slotW * 2 + 4 + slotW;
            _topGap = System.Math.Max(2, (slotW * 9 - topUsedW) / 2);

            base.RebuildUi();
        }

        protected override Control BuildContainerSpecificArea()
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var offPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 1, 0),
            };
            offPanel.Children.Add(new TextBlock
            {
                Text = Translations.tui_inventory_offhand,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            offPanel.Children.Add(CreateSlotCell(_playerVm.OffhandSlot, 0, 0));
            row.Children.Add(offPanel);

            var equipGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto"),
            };

            void AddEquipSlot(int r, int gc, string label, int eqIdx)
            {
                var lbl = MakeLabel(label);
                Grid.SetRow(lbl, r); Grid.SetColumn(lbl, gc);
                equipGrid.Children.Add(lbl);
                var btn = CreateSlotCell(_playerVm.EquipmentSlots[eqIdx], r, gc / 2);
                Grid.SetRow(btn, r); Grid.SetColumn(btn, gc + 1);
                equipGrid.Children.Add(btn);
            }

            AddEquipSlot(0, 0, Translations.tui_inventory_equip_head, 0);
            AddEquipSlot(0, 2, Translations.tui_inventory_equip_body, 1);
            AddEquipSlot(1, 0, Translations.tui_inventory_equip_legs, 2);
            AddEquipSlot(1, 2, Translations.tui_inventory_equip_feet, 3);

            row.Children.Add(equipGrid);
            row.Children.Add(new Border { Width = _topGap });

            var craftGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto"),
            };

            for (int ci = 0; ci < 4; ci++)
            {
                int cr = ci / 2, cc = ci % 2;
                var cs = CreateSlotCell(_playerVm.CraftingInputSlots[ci], cr, cc);
                Grid.SetRow(cs, cr);
                Grid.SetColumn(cs, cc);
                craftGrid.Children.Add(cs);
            }

            var arrowTb = new TextBlock
            {
                Text = "=>",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(1, 0),
            };
            Grid.SetRow(arrowTb, 1); Grid.SetColumn(arrowTb, 2);
            craftGrid.Children.Add(arrowTb);

            var craftOutPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            craftOutPanel.Children.Add(new TextBlock
            {
                Text = Translations.tui_inventory_output,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            craftOutPanel.Children.Add(CreateSlotCell(_playerVm.CraftingOutputSlot, 0, 1));
            Grid.SetRow(craftOutPanel, 0); Grid.SetColumn(craftOutPanel, 3);
            Grid.SetRowSpan(craftOutPanel, 2);
            craftGrid.Children.Add(craftOutPanel);

            row.Children.Add(craftGrid);
            return row;
        }
    }
}
