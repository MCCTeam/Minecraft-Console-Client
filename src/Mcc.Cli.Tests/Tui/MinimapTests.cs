using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Presentation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mcc.Cli.Configuration;
using Mcc.Cli.Tui;
using Mcc.Cli.Tui.Minimap;
using Umpk.Client.Snapshots;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// Unit tests for the pure, host-side minimap logic: the namespaced-id to PascalCase key conversion, the embedded block-color table, the embedded entity classifier, and the core surface-region record.
/// The Consolonia views themselves are integration-smoke (live spread), but this classification/color mapping is deterministic and testable without a session.
/// </summary>
public sealed class MinimapTests
{
    [Fact]
    public void DefaultWindowIsLargeEnoughForMapAndFooter()
    {
        var config = new ConsoleHostConfig();

        Assert.Equal(40, config.MinimapWidth);
        Assert.Equal(20, config.MinimapHeight);
        Assert.True(config.MinimapShowPlayerNames);
        Assert.True(config.MinimapShowHostileNames);
        Assert.True(config.MinimapShowNeutralNames);
        Assert.True(config.MinimapShowPassiveNames);
    }

    [Fact]
    public void DefaultController_PreservesVisibleEntityLabels()
    {
        var controller = new MinimapController(new TuiBackend(), new ConsoleHostConfig());

        Assert.True(controller.GetNameCategory(MobCategory.Player));
        Assert.True(controller.GetNameCategory(MobCategory.Hostile));
        Assert.True(controller.GetNameCategory(MobCategory.Neutral));
        Assert.True(controller.GetNameCategory(MobCategory.Passive));
    }

    [Fact]
    public void TooltipLayerStartsAboveOrdinaryContent()
    {
        var content = new Border();
        var root = new Panel { Children = { content } };

        _ = new TuiTooltipService(root);

        Control tooltipLayer = root.Children[^1];
        Assert.IsType<Canvas>(tooltipLayer);
        Assert.Equal(int.MaxValue, tooltipLayer.ZIndex);
    }

    [Theory]
    [InlineData((int)MinimapPosition.TopLeft, 1, 1)]
    [InlineData((int)MinimapPosition.TopRight, 65, 1)]
    [InlineData((int)MinimapPosition.BottomLeft, 1, 29)]
    [InlineData((int)MinimapPosition.BottomRight, 65, 29)]
    [InlineData((int)MinimapPosition.Center, 33, 15)]
    public void WindowAnchor_UsesRequestedPreset(int anchor, int expectedX, int expectedY)
    {
        PixelPoint actual = MinimapWindow.ResolveAnchor(
            (MinimapPosition)anchor, new Size(100, 50), new Size(34, 20));

        Assert.Equal(new PixelPoint(expectedX, expectedY), actual);
    }

    [Fact]
    public void WindowAnchor_ClampsWhenWindowIsLargerThanSurface()
        => Assert.Equal(
            new PixelPoint(0, 0),
            MinimapWindow.ResolveAnchor(MinimapPosition.BottomRight, new Size(20, 10), new Size(34, 20)));

    [Theory]
    [InlineData(120, 42, 32, 16, 120, 40)]
    [InlineData(500, 200, 32, 16, 240, 96)]
    [InlineData(4, 3, 32, 16, 8, 8)]
    [InlineData(0, 0, 32, 16, 32, 16)]
    public void ResizableMap_UsesAvailableContentAndKeepsFooterPinned(
        double availableWidth,
        double availableHeight,
        int fallbackWidth,
        int fallbackHeight,
        int expectedWidth,
        int expectedHeight)
    {
        (int width, int height) = MinimapControl.ResolveMapSize(
            new Size(availableWidth, availableHeight), fallbackWidth, fallbackHeight);

        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }

    [Theory]
    [InlineData("minecraft:acacia_door", "AcaciaDoor")]
    [InlineData("minecraft:packed_ice", "PackedIce")]
    [InlineData("minecraft:zombie", "Zombie")]
    [InlineData("stone", "Stone")]
    [InlineData("minecraft:tnt", "Tnt")]
    [InlineData("", "")]
    public void ToPascal_StripsNamespaceAndPascalCases(string id, string expected)
        => Assert.Equal(expected, RegistryKey.ToPascal(id));

    [Fact]
    public void ColorMap_LoadsEmbeddedTable()
        => Assert.True(MinimapColorMap.ColorCount > 100, $"expected a populated color table, got {MinimapColorMap.ColorCount}");

    [Fact]
    public void ColorMap_ResolvesKnownBlockColor()
    {
        // MinimapBlockColors.json: AcaciaDoor -> [216, 127, 51].
        Color color = MinimapColorMap.GetBaseColor("minecraft:acacia_door");
        Assert.Equal(Color.FromRgb(216, 127, 51), color);
    }

    [Fact]
    public void ColorMap_ClassifiesAirWaterIce()
    {
        Assert.True(MinimapColorMap.IsTransparent("minecraft:air"));
        Assert.True(MinimapColorMap.IsWater("minecraft:water"));
        Assert.True(MinimapColorMap.IsIce("minecraft:packed_ice"));
        Assert.False(MinimapColorMap.IsWater("minecraft:stone"));
    }

    [Fact]
    public void ColorMap_UnknownBlockFallsBack()
        => Assert.Equal(MinimapColorMap.UnknownColor, MinimapColorMap.GetBaseColor("minecraft:not_a_real_block"));

    [Fact]
    public void ColorMap_WaterBlockUsesWaterColor()
        => Assert.Equal(MinimapColorMap.WaterColor, MinimapColorMap.GetBaseColor("minecraft:water"));

    [Fact]
    public void ColorMap_HeightShade_UphillBrighterThanDownhill()
    {
        Color baseColor = Color.FromRgb(200, 200, 200);
        Color uphill = MinimapColorMap.ApplyHeightShade(baseColor, 1);
        Color flat = MinimapColorMap.ApplyHeightShade(baseColor, 0);
        Color downhill = MinimapColorMap.ApplyHeightShade(baseColor, -1);
        Assert.True(uphill.R > flat.R);
        Assert.True(flat.R > downhill.R);
    }

    [Fact]
    public void Classifier_LoadsEmbeddedTable()
        => Assert.True(MinimapEntityClassifier.Count > 50, $"expected a populated classifier, got {MinimapEntityClassifier.Count}");

    [Theory]
    [InlineData("minecraft:blaze", "Hostile")]
    [InlineData("minecraft:allay", "Passive")]
    [InlineData("minecraft:bee", "Neutral")]
    [InlineData("minecraft:arrow", "NonLiving")]
    public void Classifier_ClassifiesKnownTypes(string typeId, string expected)
        => Assert.Equal(expected, MinimapEntityClassifier.Classify(typeId, isPlayer: false).ToString());

    [Fact]
    public void Classifier_NamedPlayerIsPlayer()
        => Assert.Equal(MobCategory.Player, MinimapEntityClassifier.Classify("minecraft:zombie", isPlayer: true));

    [Fact]
    public void Classifier_UnknownIsNonLiving()
        => Assert.Equal(MobCategory.NonLiving, MinimapEntityClassifier.Classify("minecraft:not_a_mob", isPlayer: false));

    [Fact]
    public void Classifier_PriorityOrder_HostileBeatsPassive()
    {
        Assert.True(MinimapEntityClassifier.GetPriority(MobCategory.Hostile) > MinimapEntityClassifier.GetPriority(MobCategory.Player));
        Assert.True(MinimapEntityClassifier.GetPriority(MobCategory.Player) > MinimapEntityClassifier.GetPriority(MobCategory.Neutral));
        Assert.True(MinimapEntityClassifier.GetPriority(MobCategory.Neutral) > MinimapEntityClassifier.GetPriority(MobCategory.Passive));
        Assert.True(MinimapEntityClassifier.GetPriority(MobCategory.Passive) > MinimapEntityClassifier.GetPriority(MobCategory.NonLiving));
    }

    [Fact]
    public void SurfaceColumn_UnloadedDefault()
    {
        SurfaceColumn unloaded = SurfaceColumn.Unloaded;
        Assert.False(unloaded.Loaded);
        Assert.False(unloaded.Found);
        Assert.True(unloaded.State.IsDefault);
    }

    // water/ice blend paths, ported from legacy's MinimapColorMap.BlendWaterColor/BlendIceColor.

    [Fact]
    public void ColorMap_BlendWaterColor_DeeperLeansFurtherTowardWaterColor()
    {
        // WaterColor (50, 80, 180) is darker/bluer than this light-gray floor on every channel, so more depth (more WaterColor alpha) pulls every channel DOWN toward WaterColor's own value.
        Color floor = Color.FromRgb(200, 200, 200);
        Color shallow = MinimapColorMap.BlendWaterColor(floor, waterDepth: 0);
        Color deep = MinimapColorMap.BlendWaterColor(floor, waterDepth: 20);

        Assert.True(deep.R < shallow.R);
        Assert.True(deep.G < shallow.G);
        Assert.True(deep.B < shallow.B);
    }

    [Fact]
    public void ColorMap_BlendWaterColor_ZeroDepthMatchesMinimumAlpha()
    {
        // waterDepth 0 -> alpha = 0.35 exactly (min(0.85, 0.35 + 0*0.08)).
        Color floor = Color.FromRgb(200, 200, 200);
        Color blended = MinimapColorMap.BlendWaterColor(floor, waterDepth: 0);
        var expected = Color.FromRgb(
            (byte)((50 * 0.35) + (200 * 0.65)),
            (byte)((80 * 0.35) + (200 * 0.65)),
            (byte)((180 * 0.35) + (200 * 0.65)));
        Assert.Equal(expected, blended);
    }

    [Fact]
    public void ColorMap_BlendIceColor_LeansTowardIceColor()
    {
        Color floor = Color.FromRgb(0, 0, 0);
        Color blended = MinimapColorMap.BlendIceColor(floor);
        Assert.True(blended.R > 0);
        Assert.True(blended.G > 0);
        Assert.True(blended.B > 0);
    }

    [Fact]
    public void ColorMap_ApplyCaveDarkening_DarkensTowardBlack()
    {
        Color color = Color.FromRgb(200, 200, 200);
        Color darkened = MinimapColorMap.ApplyCaveDarkening(color);
        Assert.True(darkened.R < color.R);
        Assert.True(darkened.G < color.G);
        Assert.True(darkened.B < color.B);
    }

    // depth-based entity fade/culling, ported from legacy's MinimapEntityClassifier.ApplyDepthFade/ShouldDisplay.

    [Fact]
    public void Classifier_ApplyDepthFade_UnfadedWithinFiveBlocksBelow()
    {
        Color baseColor = MinimapEntityClassifier.HostileColor;
        Assert.Equal(baseColor, MinimapEntityClassifier.ApplyDepthFade(baseColor, playerY: 64, entityY: 60));
    }

    [Fact]
    public void Classifier_ApplyDepthFade_FullyFadedAtFifteenBlocksOrMoreBelow()
        => Assert.Equal(
            MinimapEntityClassifier.FadedColor,
            MinimapEntityClassifier.ApplyDepthFade(MinimapEntityClassifier.HostileColor, playerY: 64, entityY: 49));

    [Fact]
    public void Classifier_ApplyDepthFade_NeverFadesAboveThePlayer()
    {
        Color baseColor = MinimapEntityClassifier.PassiveColor;
        Assert.Equal(baseColor, MinimapEntityClassifier.ApplyDepthFade(baseColor, playerY: 64, entityY: 90));
    }

    [Theory]
    [InlineData(64, 70, true)] // above the player: always shown
    [InlineData(64, 60, true)] // 4 blocks below: shown
    [InlineData(64, 49, true)] // exactly 15 blocks below: still shown
    [InlineData(64, 48, false)] // more than 15 blocks below: culled
    public void Classifier_ShouldDisplay_CullsFarBelowNonPlayers(double playerY, double entityY, bool expected)
        => Assert.Equal(expected, MinimapEntityClassifier.ShouldDisplay(MobCategory.Hostile, playerY, entityY));

    [Fact]
    public void Classifier_ShouldDisplay_PlayersAlwaysShown()
        => Assert.True(MinimapEntityClassifier.ShouldDisplay(MobCategory.Player, playerY: 64, entityY: 0));

    [Fact]
    public void Classifier_GetCategoryLabel_ReturnsNonEmptyLabelForEachDisplayCategory()
    {
        Assert.False(string.IsNullOrWhiteSpace(MinimapEntityClassifier.GetCategoryLabel(MobCategory.Hostile)));
        Assert.False(string.IsNullOrWhiteSpace(MinimapEntityClassifier.GetCategoryLabel(MobCategory.Passive)));
        Assert.False(string.IsNullOrWhiteSpace(MinimapEntityClassifier.GetCategoryLabel(MobCategory.Neutral)));
        Assert.False(string.IsNullOrWhiteSpace(MinimapEntityClassifier.GetCategoryLabel(MobCategory.Player)));
    }
}
