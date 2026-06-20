using System.Collections.ObjectModel;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    public class PlayerInventoryViewModel : ContainerViewModel
    {
        public ObservableCollection<SlotViewModel> EquipmentSlots { get; } = new();
        public ObservableCollection<SlotViewModel> CraftingInputSlots { get; } = new();
        public SlotViewModel CraftingOutputSlot { get; }
        public SlotViewModel OffhandSlot { get; }

        public PlayerInventoryViewModel(McClient handler, int windowId)
            : base(handler, windowId, ContainerType.PlayerInventory)
        {
            CraftingOutputSlot = SlotMap[0];
            OffhandSlot = SlotMap[45];
        }

        protected override void InitializeSlots()
        {
            SlotMap.Clear();

            var craftOut = new SlotViewModel(0);
            SlotMap[0] = craftOut;

            for (int i = 1; i <= 4; i++)
            {
                var slot = new SlotViewModel(i);
                CraftingInputSlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 5; i <= 8; i++)
            {
                var slot = new SlotViewModel(i);
                EquipmentSlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 9; i <= 35; i++)
            {
                var slot = new SlotViewModel(i);
                MainInventorySlots.Add(slot);
                SlotMap[i] = slot;
            }

            for (int i = 36; i <= 44; i++)
            {
                int hotbarIdx = i - 36;
                var slot = new SlotViewModel(i, isHotbar: true, hotbarIndex: hotbarIdx);
                HotbarSlots.Add(slot);
                SlotMap[i] = slot;
            }

            var offhand = new SlotViewModel(45);
            SlotMap[45] = offhand;
        }
    }
}
