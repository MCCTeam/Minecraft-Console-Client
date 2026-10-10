using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Container;

/// <summary>
/// Chooses and builds a typed slot layout for an open container window.
/// <para>
/// The kind is taken from the SEMANTIC menu type UMPK resolves (<c>InventoryState.OpenContainerMenuType</c>, surfaced as <c>OpenContainerSnapshot.MenuType</c>): a stable <c>minecraft:menu</c> key such as <c>minecraft:furnace</c> or <c>minecraft:generic_9x3</c> that means the same thing on every supported version.
/// When the menu type could not be named, the raw pre-1.14 window type string (<c>OpenContainerSnapshot.LegacyWindowType</c>) is tried next, and only when BOTH are null does the old slot-count/property heuristic run as a last resort: enchanting (2 slots), furnace family (3 slots + progress properties), grindstone/anvil (3 slots, no properties), brewing (5 slots + properties), hopper (5 slots, no properties), crafting (10 slots), else a generic N x 9 grid.
/// The heuristic could not tell a furnace from a smoker, nor a shulker box from a chest; the semantic type can.
/// </para>
/// <para>
/// Every layout renders the container's own slots specifically and then the mirrored player inventory (27 main + 9 hotbar) as a grid.
/// Slot indices are the window indices the click actions use.
/// </para>
/// </summary>
internal sealed class ContainerLayout
{
    private const int PlayerInventorySlots = 36; // 27 main + 9 hotbar mirrored into every container window

    private readonly ContainerKind _kind;
    private readonly int _containerSlots;

    private ContainerLayout(ContainerKind kind, int totalSlots)
    {
        _kind = kind;
        SlotCount = totalSlots;
        _containerSlots = Math.Max(0, totalSlots - PlayerInventorySlots);
    }

    /// <summary>The total window slot count this layout was built for.</summary>
    public int SlotCount { get; }

    /// <summary>A localized-safe name for the container kind (fallback title).</summary>
    public string KindName => _kind switch
    {
        ContainerKind.Furnace => "furnace",
        ContainerKind.Brewing => "brewing stand",
        ContainerKind.Hopper => "hopper",
        ContainerKind.Crafting => "crafting",
        ContainerKind.Enchanting => "enchanting",
        ContainerKind.Grindstone => "grindstone",
        _ => "container",
    };

    /// <summary>
    /// Classifies the open window.
    /// <paramref name="menuType"/> is the semantic <c>minecraft:menu</c> key and wins outright; <paramref name="legacyWindowType"/> is the raw pre-1.14 wire string and is tried next; the slot-count/property heuristic runs only when both are absent.
    /// </summary>
    public static ContainerLayout Classify(
        int totalSlots,
        IReadOnlyDictionary<int, int> properties,
        string? menuType = null,
        string? legacyWindowType = null)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ContainerKind? named = FromMenuType(menuType) ?? FromMenuType(legacyWindowType);
        return new ContainerLayout(named ?? FromHeuristic(totalSlots, properties), totalSlots);
    }

    /// <summary>
    /// Maps a menu-type key to a layout kind.
    /// Accepts both the modern registry key and the pre-1.14 wire string, which spell the same kinds the same way apart from the namespace and the horse window ("EntityHorse"), so the namespace is stripped before matching.
    /// </summary>
    private static ContainerKind? FromMenuType(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        int colon = key.LastIndexOf(':');
        string path = (colon >= 0 ? key[(colon + 1)..] : key).ToLowerInvariant();
        return path switch
        {
            // The furnace family shares the in/fuel/out layout: vanilla's FurnaceMenu, SmokerMenu and BlastFurnaceMenu are all three slots with the same roles.
            // The heuristic could not tell them apart from a grindstone without the property probe; the key names them outright.
            "furnace" or "smoker" or "blast_furnace" => ContainerKind.Furnace,
            "brewing_stand" => ContainerKind.Brewing,
            "hopper" => ContainerKind.Hopper,
            // Both spellings of the same window: 1.14+ registers "crafting"/"enchantment", while the pre-1.14 wire strings are "crafting_table"/"enchanting_table".
            "crafting" or "crafting_table" => ContainerKind.Crafting,
            "enchantment" or "enchanting_table" => ContainerKind.Enchanting,
            "grindstone" => ContainerKind.Grindstone,
            _ => null,
        };
    }

    /// <summary>The pre-semantic fallback: guess the kind from the slot count and whether properties exist.</summary>
    private static ContainerKind FromHeuristic(int totalSlots, IReadOnlyDictionary<int, int> properties)
    {
        int container = totalSlots - PlayerInventorySlots;
        bool hasProps = properties.Count > 0;
        return container switch
        {
            2 => ContainerKind.Enchanting,
            3 => hasProps ? ContainerKind.Furnace : ContainerKind.Grindstone,
            5 => hasProps ? ContainerKind.Brewing : ContainerKind.Hopper,
            10 => ContainerKind.Crafting,
            _ => ContainerKind.Generic,
        };
    }

    /// <summary>Builds the layout control; <paramref name="makeCell"/> yields a cell for a window slot index.</summary>
    public Control Build(Func<int, int, SlotCell> makeCell)
    {
        var root = new StackPanel();
        root.Children.Add(BuildContainerArea(makeCell));
        root.Children.Add(Label("inventory"));
        root.Children.Add(BuildPlayerInventory(makeCell));
        return root;
    }

    private Control BuildContainerArea(Func<int, int, SlotCell> makeCell)
    {
        return _kind switch
        {
            ContainerKind.Furnace => Row(makeCell, "in", 0, "fuel", 1, "out", 2),
            ContainerKind.Grindstone => Row(makeCell, "in", 0, "in", 1, "out", 2),
            ContainerKind.Enchanting => Row(makeCell, "item", 0, "lapis", 1),

            // Slot 4 is the brewing fuel slot, which only exists from 1.9 on; Row drops any pair whose slot is past the window, so the 1.8 four-slot brewing stand renders its four real slots.
            ContainerKind.Brewing => Row(makeCell, "fuel", 4, "ingr", 3, "b1", 0, "b2", 1, "b3", 2),
            ContainerKind.Hopper => Grid(makeCell, 0, Math.Min(5, _containerSlots), 5),
            ContainerKind.Crafting => BuildCrafting(makeCell),
            _ => Grid(makeCell, 0, _containerSlots, 9),
        };
    }

    private Control BuildCrafting(Func<int, int, SlotCell> makeCell)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(Grid(makeCell, 1, 9, 3)); // 3x3 grid, slots 1..9
        row.Children.Add(new TextBlock { Text = " => ", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(makeCell(0, 0)); // result slot 0
        return row;
    }

    private Control BuildPlayerInventory(Func<int, int, SlotCell> makeCell)
    {
        int mainStart = _containerSlots;
        var stack = new StackPanel();
        stack.Children.Add(Grid(makeCell, mainStart, 27, 9));      // 3 rows main
        stack.Children.Add(Grid(makeCell, mainStart + 27, 9, 9));  // hotbar
        return stack;
    }

    private static Control Grid(Func<int, int, SlotCell> makeCell, int start, int count, int columns)
    {
        var grid = new StackPanel();
        StackPanel? current = null;
        for (int i = 0; i < count; i++)
        {
            if (i % columns == 0)
            {
                current = new StackPanel { Orientation = Orientation.Horizontal };
                grid.Children.Add(current);
            }

            int slotIndex = start + i;
            current!.Children.Add(makeCell(slotIndex, (i / columns + i) % 2));
        }

        return grid;
    }

    private Control Row(Func<int, int, SlotCell> makeCell, params object[] labelSlotPairs)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i + 1 < labelSlotPairs.Length; i += 2)
        {
            var label = (string)labelSlotPairs[i];
            var slot = (int)labelSlotPairs[i + 1];

            // A named kind can legitimately have fewer slots than its richest era (the pre-1.9 brewing stand has no fuel slot), so never emit a cell for a slot this window does not have.
            if (slot >= _containerSlots)
                continue;

            var cell = new StackPanel();
            cell.Children.Add(new TextBlock { Text = label, Foreground = Brushes.Gray });
            cell.Children.Add(makeCell(slot, 0));
            row.Children.Add(cell);
        }

        return row;
    }

    private static TextBlock Label(string text) => new() { Text = text, Foreground = Brushes.DarkGray };

    private enum ContainerKind
    {
        Generic,
        Furnace,
        Brewing,
        Hopper,
        Crafting,
        Enchanting,
        Grindstone,
    }
}
