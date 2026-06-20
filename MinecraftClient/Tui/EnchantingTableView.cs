using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class EnchantingTableView : ContainerViewBase
    {
        private readonly EnchantingViewModel _enchantVm;
        private readonly TextBlock[] _enchantNameLabels = new TextBlock[3];
        private readonly TextBlock[] _enchantCostLabels = new TextBlock[3];

        public EnchantingTableView(McClient handler, int windowId)
            : base(new EnchantingViewModel(handler, windowId))
        {
            _enchantVm = (EnchantingViewModel)_vm;
            Initialize();
        }

        private void RefreshEnchantOptions()
        {
            var container = _vm.Handler.GetInventory(_vm.WindowId);
            if (container == null) return;

            int protocolVersion = _vm.Handler.GetProtocolVersion();

            for (int i = 0; i < 3; i++)
            {
                if (_enchantNameLabels[i] == null) continue;

                short levelReq = container.Properties.TryGetValue(i, out var lr) ? lr : (short)0;
                short enchantId = container.Properties.TryGetValue(i + 4, out var eid) ? eid : (short)-1;
                short enchantLevel = container.Properties.TryGetValue(i + 7, out var el) ? el : (short)0;

                if (levelReq > 0 && enchantId >= 0)
                {
                    try
                    {
                        var enchant = EnchantmentMapping.GetEnchantmentById(protocolVersion, enchantId);
                        string name = EnchantmentMapping.GetEnchantmentName(enchant);
                        string roman = EnchantmentMapping.ConvertLevelToRomanNumbers(enchantLevel);
                        _enchantNameLabels[i].Text = $"{name} {roman}";
                        _enchantCostLabels[i].Text = $" ({levelReq})";
                    }
                    catch
                    {
                        _enchantNameLabels[i].Text = string.Format(Translations.tui_enchanting_option_slot, i + 1);
                        _enchantCostLabels[i].Text = levelReq > 0 ? $" ({levelReq})" : "";
                    }
                }
                else
                {
                    _enchantNameLabels[i].Text = string.Format(Translations.tui_enchanting_option_slot, i + 1);
                    _enchantCostLabels[i].Text = "";
                }
            }
        }

        protected override void OnContainerDataChanged()
        {
            RefreshEnchantOptions();
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

            var slotsCol = new StackPanel
            {
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Center,
            };

            slotsCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_enchanting_item,
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            slotsCol.Children.Add(CreateSlotCell(_enchantVm.ItemSlot, 0, 0));

            slotsCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_enchanting_lapis,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 80, 200)),
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            slotsCol.Children.Add(CreateSlotCell(_enchantVm.LapisSlot, 1, 0));

            panel.Children.Add(slotsCol);

            var optionsCol = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0),
            };

            optionsCol.Children.Add(new TextBlock
            {
                Text = Translations.tui_enchanting_options,
                Foreground = Brushes.Magenta,
                FontWeight = FontWeight.Bold,
            });

            int optionWidth = System.Math.Max(_slotW * 4, 30);

            for (int i = 0; i < 3; i++)
            {
                var nameLabel = new TextBlock
                {
                    Text = string.Format(Translations.tui_enchanting_option_slot, i + 1),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 70)),
                    TextWrapping = TextWrapping.NoWrap,
                };
                _enchantNameLabels[i] = nameLabel;

                var costLabel = new TextBlock
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 70)),
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                _enchantCostLabels[i] = costLabel;

                var content = new DockPanel();
                DockPanel.SetDock(costLabel, Dock.Right);
                content.Children.Add(costLabel);
                content.Children.Add(nameLabel);

                optionsCol.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(55, 50, 40)),
                    MinWidth = optionWidth,
                    MinHeight = _slotH,
                    Padding = new Thickness(1, 0),
                    Child = content,
                });
            }

            RefreshEnchantOptions();

            panel.Children.Add(optionsCol);

            return panel;
        }
    }

    public class EnchantingViewModel : ContainerViewModel
    {
        public SlotViewModel ItemSlot { get; private set; } = null!;
        public SlotViewModel LapisSlot { get; private set; } = null!;

        public EnchantingViewModel(McClient handler, int windowId)
            : base(handler, windowId, ContainerType.Enchantment)
        {
            ItemSlot = SlotMap[0];
            LapisSlot = SlotMap[1];
        }

        protected override void InitializeSlots()
        {
            SlotMap.Clear();

            SlotMap[0] = new SlotViewModel(0);
            SlotMap[1] = new SlotViewModel(1);

            for (int i = 2; i <= 28; i++)
            {
                var slot = new SlotViewModel(i);
                MainInventorySlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 29; i <= 37; i++)
            {
                int hotbarIdx = i - 29;
                var slot = new SlotViewModel(i, isHotbar: true, hotbarIndex: hotbarIdx);
                HotbarSlots.Add(slot);
                SlotMap[i] = slot;
            }
        }
    }
}
