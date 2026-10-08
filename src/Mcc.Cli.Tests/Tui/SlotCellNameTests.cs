using DMCBK.Core;
using Mcc.Cli.Tui.Presentation;
using Mcc.Cli.Tui;
using Mcc.Cli.Tui.Container;
using DMCBK.Core.Localization;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// Guards the name a TUI container cell shows.
/// The cell used to print the registry path ("red_stained_", "netherite_sw"), which is an id, not a name; it now resolves the vanilla lang entry through the same <c>InventoryRendering.TypeName</c> the text listing uses, prefers a server-given custom name, and cuts the RESOLVED name to the cell's width.
/// </summary>
public sealed class SlotCellNameTests
{
    private static readonly ITranslationSource Translations = HostTranslations.CreateDefault();

    private static ItemStackInfo Stack(string itemId, int count = 1, string? customName = null)
        => new(itemId, count, false, customName, 0, 0, [], []);

    [Theory]
    [InlineData("minecraft:red_stained_glass", "Red Stained Glass")]
    [InlineData("minecraft:player_head", "Player Head")]
    [InlineData("minecraft:lava_bucket", "Lava Bucket")]
    [InlineData("minecraft:netherite_sword", "Netherite Sword")]
    [InlineData("minecraft:cobblestone", "Cobblestone")]
    public void DisplayName_ResolvesVanillaItemName(string itemId, string expected)
        => Assert.Equal(expected, SlotCell.DisplayName(Stack(itemId), Translations));

    /// <summary>
    /// An id no vanilla table knows falls back to the PascalCase form, exactly as the text listing does (legacy returned the enum name here, which was already PascalCase).
    /// Still a name, never a raw path.
    /// </summary>
    [Fact]
    public void DisplayName_FallsBackToPascalCase_ForAnUnknownId()
        => Assert.Equal("WidgetThing", SlotCell.DisplayName(Stack("someplugin:widget_thing"), Translations));

    [Fact]
    public void DisplayName_PrefersTheServerGivenCustomName()
        => Assert.Equal(
            "Excalibur",
            SlotCell.DisplayName(Stack("minecraft:netherite_sword", customName: "Excalibur"), Translations));

    [Fact]
    public void DisplayName_DropsLegacyColourCodesFromACustomName()
        => Assert.Equal(
            "Excalibur",
            SlotCell.DisplayName(Stack("minecraft:netherite_sword", customName: "§6§lExcalibur"), Translations));

    [Fact]
    public void DisplayName_FallsBackToTheTypeName_WhenTheCustomNameIsOnlyCodes()
        => Assert.Equal(
            "Netherite Sword",
            SlotCell.DisplayName(Stack("minecraft:netherite_sword", customName: "§6"), Translations));

    [Fact]
    public void Fit_KeepsANameThatAlreadyFits()
        => Assert.Equal("Cobblestone", SlotCell.Fit("Cobblestone", 12));

    [Fact]
    public void Fit_CutsTheResolvedNameToTheCellWidth()
        => Assert.Equal("Netherite Sw", SlotCell.Fit("Netherite Sword", 12));

    [Fact]
    public void Fit_ShowsNothingWhenTheCountLeavesNoRoom()
        => Assert.Equal(string.Empty, SlotCell.Fit("Cobblestone", 0));

    /// <summary>
    /// The overlay reaches the session's translation chain through the backend's component renderer, the one object that was built from it.
    /// This is that bridge: a name resolves through it exactly as it does through the chain itself, and an unknown key still reports a miss so the PascalCase fallback (not a raw lang key) reaches the cell.
    /// </summary>
    [Fact]
    public void RendererTranslations_ResolvesThroughTheRenderersOwnChain()
    {
        var bridged = new ContainerOverlay.RendererTranslations(
            new ComponentInlineRenderer(HostTranslations.CreateDefault()));

        Assert.Equal("Lava Bucket", SlotCell.DisplayName(Stack("minecraft:lava_bucket"), bridged));
        Assert.False(bridged.TryResolve("item.minecraft.not_a_real_item", out _));
    }
}
