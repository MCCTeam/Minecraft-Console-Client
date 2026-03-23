using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class InventoryMainView : UserControl
    {
        private static readonly IBrush BrSlotEmpty = new SolidColorBrush(Color.FromRgb(50, 50, 50));
        private static readonly IBrush BrSlotFill = new SolidColorBrush(Color.FromRgb(70, 70, 80));
        private static readonly IBrush BrSlotHover = new SolidColorBrush(Color.FromRgb(100, 100, 140));
        private static readonly IBrush BrEmptyBorder = new SolidColorBrush(Color.FromRgb(90, 90, 90));
        private static readonly IBrush BrName = Brushes.White;
        private static readonly IBrush BrCount = Brushes.Yellow;
        private static readonly IBrush BrDim = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        private static readonly IBrush BrEquipLbl = Brushes.DarkCyan;
        private static readonly IBrush BrInfoHighlight = new SolidColorBrush(Color.FromRgb(40, 40, 60));
        private static readonly IBrush BrHeldItemBg = new SolidColorBrush(Color.FromRgb(60, 50, 80));
        private static readonly IBrush BrHeldItemBorder = Brushes.Yellow;

        private readonly int _slotW;
        private readonly int _slotH;
        private readonly int _nameMaxLen;
        private readonly int _nameLines;
        private readonly int _topGap;
        private readonly int _termW;

        private readonly InventoryViewModel _vm;
        private readonly TextBlock _titleText;
        private readonly Border _infoDetailBorder;
        private readonly TextBlock _infoDetailText;
        private readonly TextBlock _cursorItemText;
        private readonly TextBlock _helpText;
        private readonly TextBlock _statusBar;

        private readonly TextBlock[] _hotbarIndicators = new TextBlock[9];
        private int _currentHotbarSlot = -1;

        private Button? _lastHoveredButton;

        private readonly Canvas _overlayCanvas;
        private readonly Border _heldItemFloater;
        private readonly TextBlock _heldItemFloaterName;
        private readonly TextBlock _heldItemFloaterCount;

        public InventoryMainView()
        {
            var handler = InventoryTuiHost.ActiveHandler
                ?? throw new InvalidOperationException("No active McClient");
            int windowId = InventoryTuiHost.ActiveWindowId;

            _vm = new InventoryViewModel(handler, windowId);
            _currentHotbarSlot = handler.GetCurrentSlot();

            int termH;
            try
            {
                _termW = System.Console.WindowWidth;
                termH = System.Console.WindowHeight;
            }
            catch
            {
                _termW = 120;
                termH = 40;
            }

            int availW = _termW - 30;
            _slotW = Math.Clamp(availW / 9, 8, 18);
            _nameMaxLen = _slotW - 1;

            int topUsedW = _slotW * 4 + 8 + _slotW * 2 + 4 + _slotW;
            _topGap = Math.Max(2, (_slotW * 9 - topUsedW) / 2);

            _slotH = Math.Max(2, (termH - 8) / 6);
            _nameLines = Math.Max(1, _slotH - 1);

            _vm.SetSlotDisplayParams(_nameMaxLen, _nameLines);

            _titleText = new TextBlock
            {
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Cyan,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            _infoDetailText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
            };

            _infoDetailBorder = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                Child = _infoDetailText,
            };

            _cursorItemText = new TextBlock
            {
                Foreground = Brushes.Yellow,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
            };

            _helpText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                Text =
                    "LClick  Pick/Place\n" +
                    "RClick  Half/Place1\n" +
                    "Shift+C QuickMove\n" +
                    "Q       Drop x1\n" +
                    "Ctrl+Q  Drop Stack\n" +
                    "R       Refresh\n" +
                    "ESC     Exit",
            };

            _statusBar = new TextBlock
            {
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            _heldItemFloaterName = new TextBlock
            {
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
            };
            _heldItemFloaterCount = new TextBlock
            {
                Foreground = BrCount,
                FontWeight = FontWeight.Bold,
            };
            _heldItemFloater = new Border
            {
                Background = BrHeldItemBg,
                BorderBrush = BrHeldItemBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1, 0),
                IsVisible = false,
                MaxWidth = 24,
                Child = new StackPanel
                {
                    Children = { _heldItemFloaterName, _heldItemFloaterCount },
                },
            };

            _overlayCanvas = new Canvas
            {
                IsHitTestVisible = false,
            };
            _overlayCanvas.Children.Add(_heldItemFloater);

            Content = BuildRootLayout();
            UpdateTitle();
            UpdateInfoPanel();
            UpdateStatusBar();
        }

        private Control BuildRootLayout()
        {
            DockPanel.SetDock(_titleText, Dock.Top);
            DockPanel.SetDock(_statusBar, Dock.Bottom);

            var mainContent = new DockPanel
            {
                Children = { _titleText, _statusBar, BuildMainArea() }
            };

            return new Panel
            {
                Children = { mainContent, _overlayCanvas }
            };
        }

        private Control BuildMainArea()
        {
            var infoPanel = BuildInfoPanel();
            DockPanel.SetDock(infoPanel, Dock.Right);

            return new DockPanel
            {
                Children = { infoPanel, BuildInventoryPanel() }
            };
        }

        private Control BuildInfoPanel()
        {
            return new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Padding = new Thickness(1),
                Width = 24,
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "[ Item Info ]", FontWeight = FontWeight.Bold, Foreground = Brushes.Cyan },
                        _infoDetailBorder,
                        new TextBlock { Text = "[ Held Item ]", FontWeight = FontWeight.Bold, Foreground = Brushes.Yellow, Margin = new Thickness(0, 1, 0, 0) },
                        _cursorItemText,
                        new TextBlock { Text = "[ Controls ]", FontWeight = FontWeight.Bold, Foreground = Brushes.Green, Margin = new Thickness(0, 1, 0, 0) },
                        _helpText,
                    }
                }
            };
        }

        private Control BuildInventoryPanel()
        {
            var root = new StackPanel
            {
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            root.Children.Add(BuildTopSection());
            root.Children.Add(MakeSeparator());
            root.Children.Add(BuildSlotGrid(_vm.MainInventorySlots, 9));
            root.Children.Add(MakeSeparator());
            root.Children.Add(BuildHotbarSection());

            return new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Child = root,
            };
        }

        private Control BuildTopSection()
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
                Text = "Off",
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            offPanel.Children.Add(CreateSlotButton(_vm.OffhandSlot));
            row.Children.Add(offPanel);

            var equipGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto"),
            };

            var headLbl = MakeLabel("Hd");
            Grid.SetRow(headLbl, 0); Grid.SetColumn(headLbl, 0);
            equipGrid.Children.Add(headLbl);

            var headBtn = CreateSlotButton(_vm.EquipmentSlots[0]);
            Grid.SetRow(headBtn, 0); Grid.SetColumn(headBtn, 1);
            equipGrid.Children.Add(headBtn);

            var bodyLbl = MakeLabel("Bd");
            Grid.SetRow(bodyLbl, 0); Grid.SetColumn(bodyLbl, 2);
            equipGrid.Children.Add(bodyLbl);

            var bodyBtn = CreateSlotButton(_vm.EquipmentSlots[1]);
            Grid.SetRow(bodyBtn, 0); Grid.SetColumn(bodyBtn, 3);
            equipGrid.Children.Add(bodyBtn);

            var legsLbl = MakeLabel("Lg");
            Grid.SetRow(legsLbl, 1); Grid.SetColumn(legsLbl, 0);
            equipGrid.Children.Add(legsLbl);

            var legsBtn = CreateSlotButton(_vm.EquipmentSlots[2]);
            Grid.SetRow(legsBtn, 1); Grid.SetColumn(legsBtn, 1);
            equipGrid.Children.Add(legsBtn);

            var feetLbl = MakeLabel("Ft");
            Grid.SetRow(feetLbl, 1); Grid.SetColumn(feetLbl, 2);
            equipGrid.Children.Add(feetLbl);

            var feetBtn = CreateSlotButton(_vm.EquipmentSlots[3]);
            Grid.SetRow(feetBtn, 1); Grid.SetColumn(feetBtn, 3);
            equipGrid.Children.Add(feetBtn);

            row.Children.Add(equipGrid);

            row.Children.Add(new Border { Width = _topGap });

            var craftGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto"),
            };

            var c1 = CreateSlotButton(_vm.CraftingInputSlots[0]);
            Grid.SetRow(c1, 0); Grid.SetColumn(c1, 0);
            craftGrid.Children.Add(c1);

            var c2 = CreateSlotButton(_vm.CraftingInputSlots[1]);
            Grid.SetRow(c2, 0); Grid.SetColumn(c2, 1);
            craftGrid.Children.Add(c2);

            var c3 = CreateSlotButton(_vm.CraftingInputSlots[2]);
            Grid.SetRow(c3, 1); Grid.SetColumn(c3, 0);
            craftGrid.Children.Add(c3);

            var c4 = CreateSlotButton(_vm.CraftingInputSlots[3]);
            Grid.SetRow(c4, 1); Grid.SetColumn(c4, 1);
            craftGrid.Children.Add(c4);

            int arrowPadTop = Math.Max(0, _slotH - 1);
            var arrowTb = new TextBlock
            {
                Text = "=>",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(1, arrowPadTop, 1, 0),
            };
            Grid.SetRow(arrowTb, 0);
            Grid.SetColumn(arrowTb, 2);
            Grid.SetRowSpan(arrowTb, 2);
            craftGrid.Children.Add(arrowTb);

            var craftOut = CreateSlotButton(_vm.CraftingOutputSlot);
            Grid.SetRow(craftOut, 0);
            Grid.SetColumn(craftOut, 3);
            Grid.SetRowSpan(craftOut, 2);
            craftGrid.Children.Add(craftOut);

            row.Children.Add(craftGrid);

            return row;
        }

        private Control BuildHotbarSection()
        {
            var panel = new StackPanel { Spacing = 0 };

            var indicatorRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            for (int i = 0; i < 9; i++)
            {
                var indicator = new TextBlock
                {
                    Text = i == _currentHotbarSlot ? "▼" : " ",
                    Width = _slotW,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.LightGreen,
                    FontWeight = FontWeight.Bold,
                };
                _hotbarIndicators[i] = indicator;
                indicatorRow.Children.Add(indicator);
            }
            panel.Children.Add(indicatorRow);

            var numberRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            for (int i = 0; i < 9; i++)
            {
                numberRow.Children.Add(new TextBlock
                {
                    Text = (i + 1).ToString(),
                    Width = _slotW,
                    TextAlignment = TextAlignment.Center,
                    Foreground = i == _currentHotbarSlot ? Brushes.LightGreen : Brushes.DarkCyan,
                    FontWeight = FontWeight.Bold,
                });
            }
            panel.Children.Add(numberRow);
            panel.Children.Add(BuildSlotGrid(_vm.HotbarSlots, 9));
            return panel;
        }

        private TextBlock MakeLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = BrEquipLbl,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(1, 0, 0, 0),
                FontWeight = FontWeight.Bold,
            };
        }

        private static Control MakeSeparator()
        {
            return new Border
            {
                Height = 1,
                Margin = new Thickness(1, 0),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            };
        }

        private Control BuildSlotGrid(ObservableCollection<SlotViewModel> slots, int columns)
        {
            var grid = new Grid();
            int rows = (slots.Count + columns - 1) / columns;

            for (int r = 0; r < rows; r++)
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int c = 0; c < columns; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            for (int i = 0; i < slots.Count; i++)
            {
                var btn = CreateSlotButton(slots[i]);
                Grid.SetRow(btn, i / columns);
                Grid.SetColumn(btn, i % columns);
                grid.Children.Add(btn);
            }

            return grid;
        }

        private Button CreateSlotButton(SlotViewModel slot)
        {
            var nameTb = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };

            var countTb = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = BrCount,
                FontWeight = FontWeight.Bold,
            };

            ApplySlotVisual(slot, nameTb, countTb);

            // Inner border: visible outline for empty slots, transparent for filled
            var innerBorder = new Border
            {
                BorderBrush = slot.IsEmpty ? BrEmptyBorder : Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Child = new StackPanel
                {
                    Width = _slotW - 2,
                    Children = { nameTb, countTb },
                },
            };

            var button = new Button
            {
                Content = innerBorder,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                MinWidth = 0,
                MinHeight = 0,
                Height = _slotH,
                Width = _slotW,
                Tag = slot,
                Background = slot.IsEmpty ? BrSlotEmpty : BrSlotFill,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
            };

            button.AddHandler(PointerPressedEvent, OnSlotPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            button.PointerEntered += OnSlotPointerEnter;
            button.PointerExited += OnSlotPointerExit;
            button.PointerMoved += OnSlotPointerMoved;

            slot.PropertyChanged += (_, _) =>
            {
                ApplySlotVisual(slot, nameTb, countTb);
                button.Background = slot.IsEmpty ? BrSlotEmpty : BrSlotFill;
                innerBorder.BorderBrush = slot.IsEmpty ? BrEmptyBorder : Brushes.Transparent;
            };

            return button;
        }

        private void ApplySlotVisual(SlotViewModel slot, TextBlock nameTb, TextBlock countTb)
        {
            if (slot.IsEmpty)
            {
                nameTb.Text = "";
                nameTb.Foreground = BrDim;
                countTb.Text = "";
            }
            else
            {
                nameTb.Text = slot.ItemDisplayText;
                nameTb.Foreground = BrName;
                countTb.Text = slot.CountDisplay;
            }
        }

        private void OnSlotPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not SlotViewModel slot)
                return;

            SetHover(btn, slot);

            var point = e.GetCurrentPoint(btn);
            bool isShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

            WindowActionType action;
            if (point.Properties.IsRightButtonPressed)
                action = isShift ? WindowActionType.ShiftRightClick : WindowActionType.RightClick;
            else
                action = isShift ? WindowActionType.ShiftClick : WindowActionType.LeftClick;

            _vm.PerformAction(slot.SlotId, action);
            UpdateInfoPanel();
            UpdateStatusBar();
            UpdateHeldItemFloater(e);

            e.Handled = true;
        }

        private void OnSlotPointerEnter(object? sender, PointerEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SlotViewModel slot)
            {
                SetHover(btn, slot);
                UpdateHeldItemFloater(e);
            }
        }

        private void OnSlotPointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SlotViewModel slot)
            {
                SetHover(btn, slot);
                UpdateHeldItemFloater(e);
            }
        }

        private void OnSlotPointerExit(object? sender, PointerEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SlotViewModel slot)
                btn.Background = slot.IsEmpty ? BrSlotEmpty : BrSlotFill;
        }

        private void SetHover(Button btn, SlotViewModel slot)
        {
            if (_lastHoveredButton != null && _lastHoveredButton != btn)
            {
                if (_lastHoveredButton.Tag is SlotViewModel oldSlot)
                    _lastHoveredButton.Background = oldSlot.IsEmpty ? BrSlotEmpty : BrSlotFill;
            }

            _lastHoveredButton = btn;
            btn.Background = BrSlotHover;
            _vm.HoveredSlot = slot;
            UpdateInfoPanel();
        }

        private void UpdateHeldItemFloater(PointerEventArgs e)
        {
            if (!_vm.HasCursorItem)
            {
                _heldItemFloater.IsVisible = false;
                return;
            }

            _heldItemFloaterName.Text = _vm.CursorItemInfo;
            _heldItemFloaterCount.Text = "";

            try
            {
                var pos = e.GetPosition(_overlayCanvas);
                double left = pos.X + 2;
                double remainingW = _termW - left - 2; // 2 for border
                int maxW = Math.Max(8, (int)remainingW);
                _heldItemFloater.MaxWidth = maxW;
                Canvas.SetLeft(_heldItemFloater, left);
                Canvas.SetTop(_heldItemFloater, pos.Y);
            }
            catch
            {
                _heldItemFloater.MaxWidth = 24;
                Canvas.SetLeft(_heldItemFloater, 0);
                Canvas.SetTop(_heldItemFloater, 0);
            }

            _heldItemFloater.IsVisible = true;
        }

        private void UpdateInfoPanel()
        {
            _infoDetailText.Text = _vm.HoveredSlotDetailText;

            bool hasHoveredItem = _vm.HoveredSlot != null && !_vm.HoveredSlot.IsEmpty;
            _infoDetailBorder.Background = hasHoveredItem ? BrInfoHighlight : Brushes.Transparent;

            if (_vm.HasCursorItem)
            {
                _cursorItemText.Text = _vm.CursorItemInfo;
                _cursorItemText.Foreground = Brushes.Yellow;
            }
            else
            {
                _cursorItemText.Text = "(empty)";
                _cursorItemText.Foreground = BrDim;
                _heldItemFloater.IsVisible = false;
            }
        }

        private void UpdateTitle()
        {
            _titleText.Text = _vm.Title;
        }

        private void UpdateStatusBar()
        {
            _statusBar.Text = $"LClick: Pick/Place | RClick: Half/Place1 | Shift+C: QuickMove | Q: Drop | ESC: Exit  ({_vm.StatusText})";
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    (Application.Current?.ApplicationLifetime as IControlledApplicationLifetime)?.Shutdown();
                    e.Handled = true;
                    break;

                case Key.C:
                    if ((e.KeyModifiers & KeyModifiers.Shift) != 0 &&
                        _vm.HoveredSlot != null && !_vm.HoveredSlot.IsEmpty)
                    {
                        _vm.PerformAction(_vm.HoveredSlot.SlotId, WindowActionType.ShiftClick);
                        UpdateInfoPanel();
                        UpdateStatusBar();
                    }
                    e.Handled = true;
                    break;

                case Key.Q:
                    if (_vm.HoveredSlot != null && !_vm.HoveredSlot.IsEmpty)
                    {
                        var action = (e.KeyModifiers & KeyModifiers.Control) != 0
                            ? WindowActionType.DropItemStack
                            : WindowActionType.DropItem;
                        _vm.PerformAction(_vm.HoveredSlot.SlotId, action);
                        UpdateInfoPanel();
                        UpdateStatusBar();
                    }
                    e.Handled = true;
                    break;

                case Key.R:
                    _vm.RefreshFromContainer();
                    _currentHotbarSlot = _vm.Handler.GetCurrentSlot();
                    UpdateHotbarIndicators();
                    UpdateInfoPanel();
                    UpdateStatusBar();
                    e.Handled = true;
                    break;
            }
        }

        private void UpdateHotbarIndicators()
        {
            for (int i = 0; i < 9; i++)
            {
                _hotbarIndicators[i].Text = i == _currentHotbarSlot ? "▼" : " ";
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Focusable = true;
            Focus();
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            Focusable = true;
        }
    }
}
