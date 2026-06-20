using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MinecraftClient.Inventory;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

namespace MinecraftClient.Tui
{
    public class ContainerViewModel : INotifyPropertyChanged
    {
        private SlotViewModel? _hoveredSlot;
        private string _title = "";
        private string _statusText = "";
        private string _cursorItemInfo = "";
        private bool _hasCursorItem;

        public McClient Handler { get; }
        public int WindowId { get; }
        public ContainerType ContainerType { get; }

        public ObservableCollection<SlotViewModel> ContainerSlots { get; } = new();
        public ObservableCollection<SlotViewModel> MainInventorySlots { get; } = new();
        public ObservableCollection<SlotViewModel> HotbarSlots { get; } = new();

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

        public string HoveredSlotDetailText
        {
            get
            {
                if (_hoveredSlot == null)
                    return Translations.tui_inventory_hover_hint;

                if (_hoveredSlot.IsEmpty)
                    return $"Slot #{_hoveredSlot.SlotId}\n{Translations.tui_inventory_slot_empty}";

                var sb = new StringBuilder();
                sb.AppendLine(_hoveredSlot.ItemTypeName);
                sb.AppendLine(string.Format(Translations.tui_inventory_slot_detail, _hoveredSlot.SlotId, _hoveredSlot.ItemCount));

                var item = _hoveredSlot.RawItem;
                if (item != null)
                    AppendItemExtras(sb, item);

                return sb.ToString().TrimEnd();
            }
        }

        protected Dictionary<int, SlotViewModel> SlotMap { get; } = new();

        public ContainerViewModel(McClient handler, int windowId, ContainerType containerType)
        {
            Handler = handler;
            WindowId = windowId;
            ContainerType = containerType;

            InitializeSlots();
            RefreshFromContainer();
        }

        public void SetSlotDisplayParams(int maxWidth, int maxLines)
        {
            foreach (var kvp in SlotMap)
            {
                kvp.Value.NameMaxWidth = maxWidth;
                kvp.Value.NameMaxLines = maxLines;
            }
            RefreshFromContainer();
        }

        protected virtual void InitializeSlots()
        {
            SlotMap.Clear();

            int slotCount = ContainerType.SlotCount();
            if (slotCount == 0) return;

            int playerInvStart = slotCount - 36;

            for (int i = 0; i < playerInvStart; i++)
            {
                var slot = new SlotViewModel(i);
                ContainerSlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = playerInvStart; i < playerInvStart + 27; i++)
            {
                var slot = new SlotViewModel(i);
                MainInventorySlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = playerInvStart + 27; i < slotCount; i++)
            {
                int hotbarIdx = i - (playerInvStart + 27);
                var slot = new SlotViewModel(i, isHotbar: true, hotbarIndex: hotbarIdx);
                HotbarSlots.Add(slot);
                SlotMap[i] = slot;
            }
        }

        public virtual void RefreshFromContainer()
        {
            Inventory.Container? container = Handler.GetInventory(WindowId);
            if (container == null)
            {
                StatusText = Translations.tui_inventory_container_not_found;
                return;
            }

            Title = string.Format(Translations.tui_inventory_title, WindowId, container.Title);

            foreach (var kvp in SlotMap)
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
            StatusText = string.Format(Translations.tui_inventory_item_count, itemCount);

            OnPropertyChanged(nameof(HoveredSlotDetailText));
        }

        protected void UpdateCursorItem(Inventory.Container _)
        {
            var playerInv = Handler.GetInventory(0);
            if (playerInv != null && playerInv.Items.TryGetValue(-1, out var cursorItem) && !cursorItem.IsEmpty)
            {
                CursorItemInfo = FormatItemDetail(cursorItem);
                HasCursorItem = true;
            }
            else
            {
                CursorItemInfo = "";
                HasCursorItem = false;
            }
        }

        protected static string FormatItemDetail(Item item)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"x{item.Count} {item.GetTypeString()}");
            AppendItemExtras(sb, item);
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        private static void AppendItemExtras(StringBuilder sb, Item item)
        {
            int damage = item.Damage;
            if (damage != 0)
            {
                int maxDamage = item.Components?.OfType<MaxDamageComponent>().FirstOrDefault()?.MaxDamage ?? 0;
                if (maxDamage > 0)
                    sb.AppendLine($"{Translations.tui_inventory_durability}: {maxDamage - damage}/{maxDamage}");
                else
                    sb.AppendLine($"{Translations.cmd_inventory_damage}: {damage}");
            }

            try
            {
                var enchList = item.EnchantmentList;
                if (enchList is not null)
                {
                    bool isFirstEnchantment = true;
                    foreach (var ench in enchList)
                    {
                        string name = EnchantmentMapping.GetEnchantmentName(ench.Type);
                        string level = EnchantmentMapping.ConvertLevelToRomanNumbers(ench.Level);
                        if (isFirstEnchantment)
                        {
                            isFirstEnchantment = false;
                            sb.Append($"{name} {level}");
                        }
                        else
                        {
                            sb.Append($" | {name} {level}");
                        }
                    }
                }
                else if (item.NBT is not null &&
                         (item.NBT.TryGetValue("Enchantments", out object? enchantments) ||
                          item.NBT.TryGetValue("StoredEnchantments", out enchantments)))
                {
                    bool isFirstEnchantment = true;
                    foreach (Dictionary<string, object> enchantment in (object[])enchantments)
                    {
                        short level = (short)enchantment["lvl"];
                        string id = ((string)enchantment["id"]).Replace(':', '.');
                        string name = Protocol.Message.ChatParser.TranslateString("enchantment." + id) ?? id;
                        string levelStr = Protocol.Message.ChatParser.TranslateString("enchantment.level." + level) ?? level.ToString();
                        if (isFirstEnchantment)
                        {
                            isFirstEnchantment = false;
                            sb.Append($"{name} {levelStr}");
                        }
                        else
                        {
                            sb.Append($" | {name} {levelStr}");
                        }
                    }
                }
            }
            catch { }
        }

        public bool PerformAction(int slotId, WindowActionType action)
        {
            bool result = Handler.DoWindowAction(WindowId, slotId, action);
            RefreshFromContainer();
            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
