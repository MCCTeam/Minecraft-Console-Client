using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        private SlotViewModel? _hoveredSlot;
        private string _title = "";
        private string _statusText = "";
        private string _cursorItemInfo = "";
        private bool _hasCursorItem;

        public McClient Handler { get; }
        public int WindowId { get; }

        public ObservableCollection<SlotViewModel> EquipmentSlots { get; } = new();
        public ObservableCollection<SlotViewModel> CraftingInputSlots { get; } = new();
        public SlotViewModel CraftingOutputSlot { get; }
        public ObservableCollection<SlotViewModel> MainInventorySlots { get; } = new();
        public ObservableCollection<SlotViewModel> HotbarSlots { get; } = new();
        public SlotViewModel OffhandSlot { get; }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string CursorItemInfo
        {
            get => _cursorItemInfo;
            set { _cursorItemInfo = value; OnPropertyChanged(); }
        }

        public bool HasCursorItem
        {
            get => _hasCursorItem;
            set { _hasCursorItem = value; OnPropertyChanged(); }
        }

        public SlotViewModel? HoveredSlot
        {
            get => _hoveredSlot;
            set
            {
                if (_hoveredSlot != null)
                    _hoveredSlot.IsHovered = false;
                _hoveredSlot = value;
                if (_hoveredSlot != null)
                    _hoveredSlot.IsHovered = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HoveredSlotDetailText));
            }
        }

        /// <summary>
        /// Multi-line detail text for the hovered slot.
        /// </summary>
        public string HoveredSlotDetailText
        {
            get
            {
                if (_hoveredSlot == null)
                    return "Hover over a slot to\nsee item details.";

                if (_hoveredSlot.IsEmpty)
                    return $"Slot #{_hoveredSlot.SlotId}\n(Empty)";

                var sb = new StringBuilder();
                sb.AppendLine(_hoveredSlot.ItemTypeName);
                sb.AppendLine($"Slot #{_hoveredSlot.SlotId}  Count: {_hoveredSlot.ItemCount}");

                string fullInfo = _hoveredSlot.FullInfo;
                if (!string.IsNullOrEmpty(fullInfo))
                {
                    string[] parts = fullInfo.Split(" | ");
                    for (int i = 1; i < parts.Length; i++)
                        sb.AppendLine(parts[i].Trim());
                }

                return sb.ToString().TrimEnd();
            }
        }

        private Dictionary<int, SlotViewModel> _slotMap = new();
        private int _nameMaxLen = 9;
        private int _nameMaxLines = 1;

        public InventoryViewModel(McClient handler, int windowId)
        {
            Handler = handler;
            WindowId = windowId;

            CraftingOutputSlot = new SlotViewModel(0);
            OffhandSlot = new SlotViewModel(45);

            InitializeSlots();
            RefreshFromContainer();
        }

        public void SetSlotDisplayParams(int maxWidth, int maxLines)
        {
            _nameMaxLen = maxWidth;
            _nameMaxLines = maxLines;
            foreach (var kvp in _slotMap)
            {
                kvp.Value.NameMaxWidth = maxWidth;
                kvp.Value.NameMaxLines = maxLines;
            }
            RefreshFromContainer();
        }

        private void InitializeSlots()
        {
            _slotMap.Clear();

            _slotMap[0] = CraftingOutputSlot;

            for (int i = 1; i <= 4; i++)
            {
                var slot = new SlotViewModel(i);
                CraftingInputSlots.Add(slot);
                _slotMap[i] = slot;
            }

            for (int i = 5; i <= 8; i++)
            {
                var slot = new SlotViewModel(i);
                EquipmentSlots.Add(slot);
                _slotMap[i] = slot;
            }

            for (int i = 9; i <= 35; i++)
            {
                var slot = new SlotViewModel(i);
                MainInventorySlots.Add(slot);
                _slotMap[i] = slot;
            }

            for (int i = 36; i <= 44; i++)
            {
                int hotbarIdx = i - 36;
                var slot = new SlotViewModel(i, isHotbar: true, hotbarIndex: hotbarIdx);
                HotbarSlots.Add(slot);
                _slotMap[i] = slot;
            }

            _slotMap[45] = OffhandSlot;
        }

        public void RefreshFromContainer()
        {
            Inventory.Container? container = Handler.GetInventory(WindowId);
            if (container == null)
            {
                StatusText = "Container not found";
                return;
            }

            Title = $"Inventory #{WindowId} - {container.Title}";

            foreach (var kvp in _slotMap)
            {
                Item? item = container.Items.TryGetValue(kvp.Key, out var it) ? it : null;
                kvp.Value.Update(item);
            }

            UpdateCursorItem(container);
            int itemCount = 0;
            foreach (var kvp in container.Items)
            {
                if (kvp.Key >= 0 && !kvp.Value.IsEmpty)
                    itemCount++;
            }
            StatusText = $"{itemCount} items";

            OnPropertyChanged(nameof(HoveredSlotDetailText));
        }

        private void UpdateCursorItem(Inventory.Container container)
        {
            if (container.Items.TryGetValue(-1, out var cursorItem) && !cursorItem.IsEmpty)
            {
                CursorItemInfo = $"x{cursorItem.Count} {cursorItem.GetTypeString()}";
                HasCursorItem = true;
            }
            else
            {
                CursorItemInfo = "";
                HasCursorItem = false;
            }
        }

        public bool PerformAction(int slotId, WindowActionType action)
        {
            bool result = Handler.DoWindowAction(WindowId, slotId, action);
            RefreshFromContainer();
            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
