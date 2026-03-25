using System.ComponentModel;
using System.Runtime.CompilerServices;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class SlotViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isHovered;

        public int SlotId { get; }
        public string ItemDisplayText { get; private set; }
        public string CountDisplay { get; private set; }
        public string FullInfo { get; private set; }
        public string ItemTypeName { get; private set; }
        public bool IsEmpty { get; private set; }
        public bool IsHotbar { get; }
        public int HotbarIndex { get; }
        public ItemType ItemType { get; private set; }
        public int ItemCount { get; private set; }
        public Item? RawItem { get; private set; }
        public int NameMaxWidth { get; set; } = 9;
        public int NameMaxLines { get; set; } = 1;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public bool IsHovered
        {
            get => _isHovered;
            set { _isHovered = value; OnPropertyChanged(); }
        }

        public SlotViewModel(int slotId, bool isHotbar = false, int hotbarIndex = -1)
        {
            SlotId = slotId;
            IsHotbar = isHotbar;
            HotbarIndex = hotbarIndex;
            ItemDisplayText = "";
            CountDisplay = "";
            FullInfo = "";
            ItemTypeName = "";
            IsEmpty = true;
            ItemType = ItemType.Air;
            ItemCount = 0;
        }

        public void Update(Item? item)
        {
            RawItem = item;
            if (item == null || item.IsEmpty)
            {
                ItemDisplayText = "";
                CountDisplay = "";
                FullInfo = "";
                ItemTypeName = "";
                IsEmpty = true;
                ItemType = ItemType.Air;
                ItemCount = 0;
            }
            else
            {
                ItemType = item.Type;
                ItemCount = item.Count;
                string typeName = item.GetTypeString();
                ItemTypeName = typeName;
                ItemDisplayText = FormatMultiLine(typeName, NameMaxWidth, NameMaxLines);
                CountDisplay = item.Count > 1 ? $"x{item.Count}" : "";
                FullInfo = item.ToFullString();
                IsEmpty = false;
            }

            OnPropertyChanged(nameof(ItemDisplayText));
            OnPropertyChanged(nameof(CountDisplay));
            OnPropertyChanged(nameof(FullInfo));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(ItemType));
            OnPropertyChanged(nameof(ItemTypeName));
            OnPropertyChanged(nameof(ItemCount));
        }

        /// <summary>
        /// Format item name into multi-line display text that fits within
        /// maxWidth columns and maxLines lines. Breaks at word boundaries.
        /// </summary>
        private static string FormatMultiLine(string name, int maxWidth, int maxLines)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            int colonIdx = name.LastIndexOf(':');
            if (colonIdx >= 0 && colonIdx < name.Length - 1)
                name = name[(colonIdx + 1)..];

            name = name.Replace("_", " ").Trim();
            name = InsertCamelCaseSpaces(name);

            if (maxLines <= 1 || name.Length <= maxWidth)
                return name.Length <= maxWidth ? name : name[..maxWidth];

            var lines = new System.Collections.Generic.List<string>();
            string remaining = name;

            for (int line = 0; line < maxLines && remaining.Length > 0; line++)
            {
                if (remaining.Length <= maxWidth)
                {
                    lines.Add(remaining);
                    break;
                }

                int breakAt = -1;
                for (int i = maxWidth; i >= 1; i--)
                {
                    if (remaining[i] == ' ')
                    {
                        breakAt = i;
                        break;
                    }
                }

                if (breakAt < 0)
                    breakAt = maxWidth;

                lines.Add(remaining[..breakAt].TrimEnd());
                remaining = remaining[breakAt..].TrimStart();
            }

            return string.Join("\n", lines);
        }

        private static string InsertCamelCaseSpaces(string s)
        {
            if (s.Length < 2) return s;
            var sb = new System.Text.StringBuilder(s.Length + 4);
            sb.Append(s[0]);
            for (int i = 1; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]) && char.IsLower(s[i - 1]))
                    sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
