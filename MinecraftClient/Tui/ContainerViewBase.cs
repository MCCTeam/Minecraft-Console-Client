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
    public abstract class ContainerViewBase : UserControl
    {
        protected static readonly IBrush BrSlotEmptyA = new SolidColorBrush(Color.FromRgb(40, 40, 40));
        protected static readonly IBrush BrSlotEmptyB = new SolidColorBrush(Color.FromRgb(55, 55, 55));
        protected static readonly IBrush BrSlotFillA = new SolidColorBrush(Color.FromRgb(60, 60, 75));
        protected static readonly IBrush BrSlotFillB = new SolidColorBrush(Color.FromRgb(75, 75, 90));
        protected static readonly IBrush BrSlotHover = new SolidColorBrush(Color.FromRgb(100, 100, 140));
        protected static readonly IBrush BrName = Brushes.White;
        protected static readonly IBrush BrCount = Brushes.Yellow;
        protected static readonly IBrush BrDim = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        protected static readonly IBrush BrEquipLbl = Brushes.DarkCyan;
        protected static readonly IBrush BrInfoHighlight = new SolidColorBrush(Color.FromRgb(40, 40, 60));
        protected static readonly IBrush BrHeldItemBg = new SolidColorBrush(Color.FromRgb(60, 50, 80));
        protected static readonly IBrush BrHeldItemBorder = Brushes.Yellow;

        protected int _slotW;
        protected int _slotH;
        protected int _nameMaxLen;
        protected int _nameLines;
        protected int _termW;

        protected readonly ContainerViewModel _vm;
        protected TextBlock _titleText = null!;
        protected Border _infoDetailBorder = null!;
        protected TextBlock _infoDetailText = null!;
        protected TextBlock _cursorItemText = null!;
        protected TextBlock _helpText = null!;

        protected TextBlock[] _hotbarIndicators = new TextBlock[9];
        protected int _currentHotbarSlot = -1;

        protected Border? _lastHoveredSlotBorder;

        protected Canvas _overlayCanvas = null!;
        protected Border _heldItemFloater = null!;
        protected TextBlock _heldItemFloaterName = null!;
        protected TextBlock _heldItemFloaterCount = null!;

        protected ScrollViewer _chatScrollViewer = null!;
        protected ObservableCollection<string>? _chatLines;
        protected int _lastTermW;
        protected int _lastTermH;
        protected bool _chatScrollToBottom = true;

        protected ContainerViewBase(ContainerViewModel vm)
        {
            _vm = vm;
            _currentHotbarSlot = vm.Handler.GetCurrentSlot();

            _chatLines = TuiConsoleBackend.Instance?.GetView()?.GetRecentLogLines(50)
                ?? new ObservableCollection<string>();
        }

        protected void Initialize()
        {
            RebuildUi();
        }

        protected abstract int GetTotalSlotRows();

        protected abstract Control BuildContainerSpecificArea();

        protected virtual void OnContainerDataChanged() { }

        protected virtual void RebuildUi()
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

            int totalRows = GetTotalSlotRows();
            int overhead = 4;
            int chatMinH = 1;
            _slotH = Math.Clamp((termH - overhead - chatMinH) / totalRows, 2, 5);
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
                Text = Translations.tui_inventory_controls_help,
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

        protected virtual Control BuildRootLayout()
        {
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

        protected virtual Control BuildMainArea()
        {
            var infoPanel = BuildInfoPanel();
            DockPanel.SetDock(infoPanel, Dock.Right);

            return new DockPanel
            {
                Children = { infoPanel, BuildInventoryPanel() }
            };
        }

        protected virtual Control BuildInventoryPanel()
        {
            var root = new StackPanel
            {
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            root.Children.Add(BuildContainerSpecificArea());
            root.Children.Add(BuildSeparator());
            root.Children.Add(BuildSlotGrid(_vm.MainInventorySlots, 9));
            root.Children.Add(BuildHotbarSection());

            return new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Child = root,
            };
        }

        protected Control BuildSeparator()
        {
            return new Border
            {
                Height = 1,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 0),
            };
        }

        protected Control BuildInfoPanel()
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
                        new TextBlock { Text = Translations.tui_inventory_item_info, FontWeight = FontWeight.Bold, Foreground = Brushes.Cyan },
                        _infoDetailBorder,
                        new TextBlock { Text = Translations.tui_inventory_held_item, FontWeight = FontWeight.Bold, Foreground = Brushes.Yellow, Margin = new Thickness(0, 1, 0, 0) },
                        _cursorItemText,
                        new TextBlock { Text = Translations.tui_inventory_controls, FontWeight = FontWeight.Bold, Foreground = Brushes.Green, Margin = new Thickness(0, 1, 0, 0) },
                        _helpText,
                    }
                }
            };
        }

        protected Control BuildHotbarSection()
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

        protected Control BuildSlotGrid(ObservableCollection<SlotViewModel> slots, int columns)
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

        protected static IBrush GetSlotBg(bool isEmpty, int row, int col)
        {
            bool isA = (row + col) % 2 == 0;
            return isEmpty
                ? (isA ? BrSlotEmptyA : BrSlotEmptyB)
                : (isA ? BrSlotFillA : BrSlotFillB);
        }

        protected Border CreateSlotCell(SlotViewModel slot, int row = 0, int col = 0)
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

        protected static void ApplySlotVisual(SlotViewModel slot, TextBlock nameTb, TextBlock countTb)
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

        protected TextBlock MakeLabel(string text)
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

        #region Pointer / Keyboard interaction

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
            OnContainerDataChanged();
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

        protected void SetHover(Border border, SlotViewModel slot)
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

        protected void UpdateHeldItemFloater(PointerEventArgs e)
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

        protected void UpdateInfoPanel()
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
                _cursorItemText.Text = Translations.tui_inventory_cursor_empty;
                _cursorItemText.Foreground = BrDim;
                _heldItemFloater.IsVisible = false;
            }
        }

        protected void UpdateTitle()
        {
            _titleText.Text = _vm.Title;
        }

        protected void CloseInventory()
        {
            if (_vm.WindowId != 0)
                _vm.Handler.CloseInventory(_vm.WindowId);

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
                    OnContainerDataChanged();
                    e.Handled = true;
                    break;
            }
        }

        protected void UpdateHotbarIndicators()
        {
            for (int i = 0; i < 9; i++)
            {
                bool active = i == _currentHotbarSlot;
                _hotbarIndicators[i].Text = active ? $"{i + 1} \u25bc" : $" {i + 1} ";
                _hotbarIndicators[i].Foreground = active ? Brushes.LightGreen : Brushes.DarkCyan;
            }
        }

        #endregion

        #region Lifecycle

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

        #endregion

        public static bool HasTuiSupport(ContainerType type)
        {
            return type switch
            {
                ContainerType.PlayerInventory => true,
                ContainerType.Generic_9x1 => true,
                ContainerType.Generic_9x2 => true,
                ContainerType.Generic_9x3 => true,
                ContainerType.Generic_9x4 => true,
                ContainerType.Generic_9x5 => true,
                ContainerType.Generic_9x6 => true,
                ContainerType.Generic_3x3 => true,
                ContainerType.Crafter => true,
                ContainerType.ShulkerBox => true,
                ContainerType.Crafting => true,
                ContainerType.Furnace => true,
                ContainerType.BlastFurnace => true,
                ContainerType.Smoker => true,
                ContainerType.Enchantment => true,
                ContainerType.BrewingStand => true,
                ContainerType.Hopper => true,
                ContainerType.Grindstone => true,
                _ => false,
            };
        }

        public static ContainerViewBase CreateView(ContainerType type, McClient handler, int windowId)
        {
            return type switch
            {
                ContainerType.PlayerInventory => new PlayerInventoryView(handler, windowId),
                ContainerType.Generic_9x3 or ContainerType.ShulkerBox => new GridContainerView(handler, windowId, type, 3, 9),
                ContainerType.Generic_9x6 => new GridContainerView(handler, windowId, type, 6, 9),
                ContainerType.Generic_3x3 or ContainerType.Crafter
                    => new GridContainerView(handler, windowId, type, 3, 3),
                ContainerType.Generic_9x1 => new GridContainerView(handler, windowId, type, 1, 9),
                ContainerType.Generic_9x2 => new GridContainerView(handler, windowId, type, 2, 9),
                ContainerType.Generic_9x4 => new GridContainerView(handler, windowId, type, 4, 9),
                ContainerType.Generic_9x5 => new GridContainerView(handler, windowId, type, 5, 9),
                ContainerType.Crafting => new CraftingView(handler, windowId),
                ContainerType.Furnace or ContainerType.BlastFurnace or ContainerType.Smoker
                    => new FurnaceView(handler, windowId, type),
                ContainerType.Enchantment => new EnchantingTableView(handler, windowId),
                ContainerType.BrewingStand => new BrewingStandView(handler, windowId),
                ContainerType.Hopper => new HopperView(handler, windowId),
                ContainerType.Grindstone => new GrindstoneView(handler, windowId),
                _ => throw new ArgumentException($"No TUI view for {type}"),
            };
        }
    }
}
