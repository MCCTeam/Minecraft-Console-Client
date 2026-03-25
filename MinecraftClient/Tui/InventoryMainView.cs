using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class InventoryMainView : UserControl
    {
        private static readonly IBrush BrSlotEmptyA = new SolidColorBrush(Color.FromRgb(40, 40, 40));
        private static readonly IBrush BrSlotEmptyB = new SolidColorBrush(Color.FromRgb(55, 55, 55));
        private static readonly IBrush BrSlotFillA = new SolidColorBrush(Color.FromRgb(60, 60, 75));
        private static readonly IBrush BrSlotFillB = new SolidColorBrush(Color.FromRgb(75, 75, 90));
        private static readonly IBrush BrSlotHover = new SolidColorBrush(Color.FromRgb(100, 100, 140));
        private static readonly IBrush BrName = Brushes.White;
        private static readonly IBrush BrCount = Brushes.Yellow;
        private static readonly IBrush BrDim = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        private static readonly IBrush BrEquipLbl = Brushes.DarkCyan;
        private static readonly IBrush BrInfoHighlight = new SolidColorBrush(Color.FromRgb(40, 40, 60));
        private static readonly IBrush BrHeldItemBg = new SolidColorBrush(Color.FromRgb(60, 50, 80));
        private static readonly IBrush BrHeldItemBorder = Brushes.Yellow;

        private int _slotW;
        private int _slotH;
        private int _nameMaxLen;
        private int _nameLines;
        private int _topGap;
        private int _termW;

        private readonly InventoryViewModel _vm;
        private TextBlock _titleText = null!;
        private Border _infoDetailBorder = null!;
        private TextBlock _infoDetailText = null!;
        private TextBlock _cursorItemText = null!;
        private TextBlock _helpText = null!;

        private TextBlock[] _hotbarIndicators = new TextBlock[9];
        private int _currentHotbarSlot = -1;

        private Border? _lastHoveredSlotBorder;

        private Canvas _overlayCanvas = null!;
        private Border _heldItemFloater = null!;
        private TextBlock _heldItemFloaterName = null!;
        private TextBlock _heldItemFloaterCount = null!;

        private ScrollViewer _chatScrollViewer = null!;
        private ObservableCollection<string>? _chatLines;
        private int _lastTermW;
        private int _lastTermH;

        public InventoryMainView()
        {
            var handler = InventoryTuiHost.ActiveHandler
                ?? throw new InvalidOperationException("No active McClient");
            int windowId = InventoryTuiHost.ActiveWindowId;

            _vm = new InventoryViewModel(handler, windowId);
            _currentHotbarSlot = handler.GetCurrentSlot();

            _chatLines = TuiConsoleBackend.Instance?.GetView()?.GetRecentLogLines(50)
                ?? new ObservableCollection<string>();

            RebuildUi();
        }

        private void RebuildUi()
        {
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

            _lastTermW = _termW;
            _lastTermH = termH;

            int availW = _termW - 26;
            _slotW = Math.Clamp(availW / 9, 8, 18);
            _nameMaxLen = _slotW;

            int topUsedW = _slotW * 4 + 8 + _slotW * 2 + 4 + _slotW;
            _topGap = Math.Max(2, (_slotW * 9 - topUsedW) / 2);

            _slotH = Math.Clamp((termH - 8) / 6, 2, 5);
            _nameLines = _slotH;

            _vm.SetSlotDisplayParams(_nameMaxLen, _nameLines);

            _lastHoveredSlotBorder = null;

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
                    "E/ESC   Exit",
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

            _overlayCanvas = new Canvas { IsHitTestVisible = false };
            _overlayCanvas.Children.Add(_heldItemFloater);

            var chatLines = _chatLines!;
            chatLines.CollectionChanged += (_, _) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var sv = _chatScrollViewer;
                    if (sv.Extent.Height > sv.Viewport.Height)
                        sv.Offset = new Vector(0, sv.Extent.Height - sv.Viewport.Height);
                }, Avalonia.Threading.DispatcherPriority.Background);
            };
            var chatItemsControl = new ItemsControl
            {
                ItemsSource = chatLines,
                Focusable = false,
                ItemTemplate = new FuncDataTemplate<string>((s, _) =>
                    new TextBlock
                    {
                        Text = s,
                        Foreground = Brushes.Gray,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        TextWrapping = TextWrapping.Wrap,
                    }),
            };
            _chatScrollViewer = new ScrollViewer
            {
                Content = chatItemsControl,
                Background = Brushes.Black,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Padding = new Thickness(0),
            };

            _hotbarIndicators = new TextBlock[9];

            Content = BuildRootLayout();
            UpdateTitle();
            UpdateInfoPanel();

            _chatScrollToBottom = true;
            _chatScrollViewer.ScrollChanged += OnChatScrollChanged;
        }

        private bool _chatScrollToBottom = true;

        private void OnChatScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (!_chatScrollToBottom) return;
            var sv = _chatScrollViewer;
            if (sv.Extent.Height > sv.Viewport.Height)
            {
                sv.Offset = new Vector(0, sv.Extent.Height - sv.Viewport.Height);
                _chatScrollToBottom = false;
            }
        }

        private Control BuildRootLayout()
        {
            // Layout (top-down):
            //   Title
            //   [InfoPanel(right)] [InventoryGrid(left)]   <-- inventory area
            //   ChatScrollViewer (full width, fills remaining)

            var inventoryArea = BuildMainArea();
            DockPanel.SetDock(_titleText, Dock.Top);
            DockPanel.SetDock(inventoryArea, Dock.Top);

            var mainContent = new DockPanel
            {
                Children = { _titleText, inventoryArea, _chatScrollViewer }
            };

            return new Panel
            {
                Background = Brushes.Black,
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
            root.Children.Add(new Border { Height = 1 });
            root.Children.Add(BuildSlotGrid(_vm.MainInventorySlots, 9));
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
            offPanel.Children.Add(CreateSlotCell(_vm.OffhandSlot, 0, 0));
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
                var btn = CreateSlotCell(_vm.EquipmentSlots[eqIdx], r, gc / 2);
                Grid.SetRow(btn, r); Grid.SetColumn(btn, gc + 1);
                equipGrid.Children.Add(btn);
            }

            AddEquipSlot(0, 0, "Hd", 0);
            AddEquipSlot(0, 2, "Bd", 1);
            AddEquipSlot(1, 0, "Lg", 2);
            AddEquipSlot(1, 2, "Ft", 3);

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
                var cs = CreateSlotCell(_vm.CraftingInputSlots[ci], cr, cc);
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
                Text = "Out",
                Foreground = BrEquipLbl,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            craftOutPanel.Children.Add(CreateSlotCell(_vm.CraftingOutputSlot, 0, 1));
            Grid.SetRow(craftOutPanel, 0); Grid.SetColumn(craftOutPanel, 3);
            Grid.SetRowSpan(craftOutPanel, 2);
            craftGrid.Children.Add(craftOutPanel);

            row.Children.Add(craftGrid);
            return row;
        }

        private Control BuildHotbarSection()
        {
            var panel = new StackPanel { Spacing = 0 };

            var numberRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            for (int i = 0; i < 9; i++)
            {
                bool active = i == _currentHotbarSlot;
                string label = active ? $"{i + 1} \u25bc" : $" {i + 1} ";

                var tb = new TextBlock
                {
                    Text = label,
                    Width = _slotW,
                    TextAlignment = TextAlignment.Center,
                    Foreground = active ? Brushes.LightGreen : Brushes.DarkCyan,
                    FontWeight = FontWeight.Bold,
                };
                _hotbarIndicators[i] = tb;
                numberRow.Children.Add(tb);
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
                int row = i / columns;
                int col = i % columns;
                var cell = CreateSlotCell(slots[i], row, col);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                grid.Children.Add(cell);
            }

            return grid;
        }

        private static IBrush GetSlotBg(bool isEmpty, int row, int col)
        {
            bool isA = (row + col) % 2 == 0;
            return isEmpty
                ? (isA ? BrSlotEmptyA : BrSlotEmptyB)
                : (isA ? BrSlotFillA : BrSlotFillB);
        }

        private Border CreateSlotCell(SlotViewModel slot, int row = 0, int col = 0)
        {
            var nameTb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
            };

            var countTb = new TextBlock
            {
                Foreground = BrCount,
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
            };

            ApplySlotVisual(slot, nameTb, countTb);

            int r = row, c = col;
            var border = new Border
            {
                Width = _slotW,
                Height = _slotH,
                Background = GetSlotBg(slot.IsEmpty, r, c),
                Child = new Panel
                {
                    Children = { nameTb, countTb },
                },
                Tag = (slot, r, c),
            };

            border.PointerPressed += OnSlotPointerPressed;
            border.PointerEntered += OnSlotPointerEnter;
            border.PointerExited += OnSlotPointerExit;
            border.PointerMoved += OnSlotPointerMoved;

            slot.PropertyChanged += (_, _) =>
            {
                ApplySlotVisual(slot, nameTb, countTb);
                border.Background = GetSlotBg(slot.IsEmpty, r, c);
            };

            return border;
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
            if (sender is not Border border || border.Tag is not (SlotViewModel slot, int, int))
                return;

            SetHover(border, slot);

            var point = e.GetCurrentPoint(border);
            bool isShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

            WindowActionType action;
            if (point.Properties.IsRightButtonPressed)
                action = isShift ? WindowActionType.ShiftRightClick : WindowActionType.RightClick;
            else
                action = isShift ? WindowActionType.ShiftClick : WindowActionType.LeftClick;

            _vm.PerformAction(slot.SlotId, action);
            UpdateInfoPanel();
            UpdateHeldItemFloater(e);
            e.Handled = true;
        }

        private void OnSlotPointerEnter(object? sender, PointerEventArgs e)
        {
            if (sender is Border b && b.Tag is (SlotViewModel slot, int, int))
            {
                SetHover(b, slot);
                UpdateHeldItemFloater(e);
            }
        }

        private void OnSlotPointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is Border b && b.Tag is (SlotViewModel slot, int, int))
            {
                SetHover(b, slot);
                UpdateHeldItemFloater(e);
            }
        }

        private void OnSlotPointerExit(object? sender, PointerEventArgs e)
        {
            if (sender is Border b && b.Tag is (SlotViewModel slot, int row, int col))
                b.Background = GetSlotBg(slot.IsEmpty, row, col);
        }

        private void SetHover(Border border, SlotViewModel slot)
        {
            if (_lastHoveredSlotBorder != null && _lastHoveredSlotBorder != border)
            {
                if (_lastHoveredSlotBorder.Tag is (SlotViewModel oldSlot, int or, int oc))
                    _lastHoveredSlotBorder.Background = GetSlotBg(oldSlot.IsEmpty, or, oc);
            }

            _lastHoveredSlotBorder = border;
            border.Background = BrSlotHover;
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
                double remainingW = _termW - left - 2;
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

        private void CloseInventory()
        {
            if (ConsoleIO.Backend is TuiConsoleBackend tuiBackend)
                tuiBackend.GetView()?.HideOverlay();
            else
                (Application.Current?.ApplicationLifetime as IControlledApplicationLifetime)?.Shutdown();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                case Key.E:
                    CloseInventory();
                    e.Handled = true;
                    break;

                case Key.C:
                    if ((e.KeyModifiers & KeyModifiers.Shift) != 0 &&
                        _vm.HoveredSlot != null && !_vm.HoveredSlot.IsEmpty)
                    {
                        _vm.PerformAction(_vm.HoveredSlot.SlotId, WindowActionType.ShiftClick);
                        UpdateInfoPanel();
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
                    }
                    e.Handled = true;
                    break;

                case Key.R:
                    _vm.RefreshFromContainer();
                    _currentHotbarSlot = _vm.Handler.GetCurrentSlot();
                    UpdateHotbarIndicators();
                    UpdateInfoPanel();
                    e.Handled = true;
                    break;
            }
        }

        private void UpdateHotbarIndicators()
        {
            for (int i = 0; i < 9; i++)
            {
                bool active = i == _currentHotbarSlot;
                _hotbarIndicators[i].Text = active ? $"{i + 1} \u25bc" : $" {i + 1} ";
                _hotbarIndicators[i].Foreground = active ? Brushes.LightGreen : Brushes.DarkCyan;
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Focusable = true;
            Focus();
            AddHandler(KeyDownEvent, OnTunnelKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            SizeChanged += OnViewSizeChanged;
        }

        private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseInventory();
                e.Handled = true;
            }
        }

        private void OnViewSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            int newW, newH;
            try
            {
                newW = System.Console.WindowWidth;
                newH = System.Console.WindowHeight;
            }
            catch { return; }

            if (newW == _lastTermW && newH == _lastTermH) return;

            _vm.RefreshFromContainer();
            _currentHotbarSlot = _vm.Handler.GetCurrentSlot();
            RebuildUi();
            Focus();
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            Focusable = true;
        }
    }
}
