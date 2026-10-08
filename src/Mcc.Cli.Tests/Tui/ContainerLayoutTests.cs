using Mcc.Cli.Tui.Container;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// Unit tests for the TUI container-type classifier.
/// The layout is chosen from the SEMANTIC menu type UMPK resolves (<c>InventoryState.OpenContainerMenuType</c>), with the raw pre-1.14 window-type string as the fallback and the old slot-count/property heuristic only when both are absent.
/// These cover all three tiers, the precedence between them, and the cases the heuristic provably got wrong.
/// </summary>
public sealed class ContainerLayoutTests
{
    private static readonly IReadOnlyDictionary<int, int> NoProps = new Dictionary<int, int>();
    private static readonly IReadOnlyDictionary<int, int> SomeProps = new Dictionary<int, int> { [0] = 100, [1] = 200 };

    [Fact]
    public void Enchanting_TwoContainerSlots()
        => Assert.Equal("enchanting", ContainerLayout.Classify(38, SomeProps).KindName);

    [Fact]
    public void Furnace_ThreeSlotsWithProgressProperties()
        => Assert.Equal("furnace", ContainerLayout.Classify(39, SomeProps).KindName);

    [Fact]
    public void Grindstone_ThreeSlotsNoProperties()
        => Assert.Equal("grindstone", ContainerLayout.Classify(39, NoProps).KindName);

    [Fact]
    public void Brewing_FiveSlotsWithProperties()
        => Assert.Equal("brewing stand", ContainerLayout.Classify(41, SomeProps).KindName);

    [Fact]
    public void Hopper_FiveSlotsNoProperties()
        => Assert.Equal("hopper", ContainerLayout.Classify(41, NoProps).KindName);

    [Fact]
    public void Crafting_TenContainerSlots()
        => Assert.Equal("crafting", ContainerLayout.Classify(46, NoProps).KindName);

    [Theory]
    [InlineData(63)] // 9x3 chest
    [InlineData(90)] // double chest
    public void GenericChest_MultipleOfNine(int totalSlots)
        => Assert.Equal("container", ContainerLayout.Classify(totalSlots, NoProps).KindName);

    [Fact]
    public void Classify_CarriesTotalSlotCount()
        => Assert.Equal(63, ContainerLayout.Classify(63, NoProps).SlotCount);

    #region The semantic menu type (InventoryState.OpenContainerMenuType)

    [Theory]
    [InlineData("minecraft:furnace", "furnace")]
    [InlineData("minecraft:smoker", "furnace")]
    [InlineData("minecraft:blast_furnace", "furnace")]
    [InlineData("minecraft:brewing_stand", "brewing stand")]
    [InlineData("minecraft:hopper", "hopper")]
    [InlineData("minecraft:crafting", "crafting")]
    [InlineData("minecraft:enchantment", "enchanting")]
    [InlineData("minecraft:grindstone", "grindstone")]
    public void SemanticMenuType_NamesTheKind(string menuType, string expected)
        => Assert.Equal(expected, ContainerLayout.Classify(63, NoProps, menuType).KindName);

    [Theory]
    [InlineData("minecraft:crafting_table", "crafting")]
    [InlineData("minecraft:enchanting_table", "enchanting")]
    [InlineData("minecraft:furnace", "furnace")]
    [InlineData("minecraft:brewing_stand", "brewing stand")]
    [InlineData("minecraft:hopper", "hopper")]
    public void LegacyWindowType_NamesTheKind_WhenNoSemanticType(string windowType, string expected)
        => Assert.Equal(expected, ContainerLayout.Classify(63, NoProps, menuType: null, legacyWindowType: windowType).KindName);

    [Fact]
    public void SemanticMenuType_BeatsTheLegacyStringAndTheHeuristic()
    {
        // 39 slots with properties is exactly what the heuristic calls a furnace.
        // The semantic type says grindstone, and the semantic type is authoritative.
        Assert.Equal(
            "grindstone",
            ContainerLayout.Classify(39, SomeProps, "minecraft:grindstone", "minecraft:furnace").KindName);
    }

    [Fact]
    public void LegacyWindowType_BeatsTheHeuristic()
        => Assert.Equal(
            "hopper",
            ContainerLayout.Classify(41, SomeProps, menuType: null, legacyWindowType: "minecraft:hopper").KindName);

    [Fact]
    public void Heuristic_RunsOnlyWhenBothTypesAreAbsent()
    {
        // The unnameable-window case: UMPK resolved neither, so the old guess is all that is left.
        Assert.Equal("furnace", ContainerLayout.Classify(39, SomeProps, menuType: null, legacyWindowType: null).KindName);
        Assert.Equal("furnace", ContainerLayout.Classify(39, SomeProps, string.Empty, string.Empty).KindName);
    }

    [Fact]
    public void SmokerAndBlastFurnace_WereIndistinguishableFromAFurnaceByCountAlone()
    {
        // All three are 3 slots with progress properties, so the heuristic could only ever say "furnace".
        // The semantic type is what makes them separately identifiable to a caller.
        Assert.Equal("furnace", ContainerLayout.Classify(39, SomeProps).KindName);
        Assert.Equal("furnace", ContainerLayout.Classify(39, SomeProps, "minecraft:smoker").KindName);
        Assert.Equal("furnace", ContainerLayout.Classify(39, SomeProps, "minecraft:blast_furnace").KindName);
    }

    [Fact]
    public void UnknownMenuTypeKey_FallsThroughToTheHeuristic()
    {
        // A modded or future menu key must not break the view: it is simply not one of the typed layouts.
        Assert.Equal("container", ContainerLayout.Classify(63, NoProps, "examplemod:reactor").KindName);
        Assert.Equal("furnace", ContainerLayout.Classify(39, SomeProps, "examplemod:reactor").KindName);
    }

    [Fact]
    public void MenuTypeMatching_IgnoresNamespaceAndCase()
    {
        Assert.Equal("hopper", ContainerLayout.Classify(63, NoProps, "MINECRAFT:HOPPER").KindName);
        Assert.Equal("hopper", ContainerLayout.Classify(63, NoProps, "hopper").KindName);
    }

    [Fact]
    public void GenericChestMenuTypes_StayGeneric()
    {
        Assert.Equal("container", ContainerLayout.Classify(63, NoProps, "minecraft:generic_9x3").KindName);
        Assert.Equal("container", ContainerLayout.Classify(90, NoProps, "minecraft:generic_9x6").KindName);
        Assert.Equal("container", ContainerLayout.Classify(63, NoProps, menuType: null, legacyWindowType: "minecraft:chest").KindName);
    }
    #endregion
}
