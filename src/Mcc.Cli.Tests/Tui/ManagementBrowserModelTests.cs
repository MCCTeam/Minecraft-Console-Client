using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Mcc.Cli.Tui.Management;
using DMCBK.Core.Commands;
using Umpk.Client;
using Umpk.Game.Inventory;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

public sealed class ManagementBrowserModelTests
{
    [Theory]
    [InlineData(120, 40, false, 101, 35)]
    [InlineData(120, 40, true, 57, 35)]
    [InlineData(5, 3, false, 1, 1)]
    public void ChunkBrowserUsesAnOddResponsiveGrid(
        double width,
        double height,
        bool emoji,
        int expectedColumns,
        int expectedRows)
        => Assert.Equal(
            (expectedColumns, expectedRows),
            ChunkBrowserPage.ResolveGridSize(new Avalonia.Size(width, height), emoji));

    [Theory]
    [InlineData(1, true)]
    [InlineData(40, true)]
    [InlineData(99, true)]
    [InlineData(100, false)]
    [InlineData(140, false)]
    public void WorkspaceSwitchesToListDetailNavigationBelowOneHundredColumns(
        double width, bool expectedCompact)
        => Assert.Equal(expectedCompact, ManagementWorkspace.CompactForWidth(width));

    [Fact]
    public void ResponsiveBodyCanRebuildAndSwitchModesWithoutDoubleParentingControls()
    {
        var host = new ContentControl();
        var list = new ListBox();
        var detail = new StackPanel();

        ManagementUi.SetResponsiveBody(host, false, false, list, detail, "40,*");
        ManagementUi.SetResponsiveBody(host, false, false, list, detail, "40,*");
        ManagementUi.SetResponsiveBody(host, true, false, list, detail, "40,*");
        Assert.Same(list, host.Content);

        ManagementUi.SetResponsiveBody(host, true, true, list, detail, "40,*");
        Assert.Same(detail, host.Content);

        ManagementUi.SetResponsiveBody(host, false, false, list, detail, "40,*");
        Assert.IsType<Grid>(host.Content);
    }

    [Fact]
    public void WorkspaceCanMovePageHostBetweenOuterScrollAndFixedViewport()
    {
        var pageHost = new ContentControl();
        var scroll = new ScrollViewer { Content = pageHost };
        var viewport = new ContentControl { Content = scroll };

        ManagementWorkspace.SetPageScrollMode(viewport, scroll, pageHost, true);
        ManagementWorkspace.SetPageScrollMode(viewport, scroll, pageHost, false);
        ManagementWorkspace.SetPageScrollMode(viewport, scroll, pageHost, false);
        Assert.Same(pageHost, viewport.Content);
        Assert.Null(scroll.Content);

        ManagementWorkspace.SetPageScrollMode(viewport, scroll, pageHost, true);
        ManagementWorkspace.SetPageScrollMode(viewport, scroll, pageHost, true);
        Assert.Same(scroll, viewport.Content);
        Assert.Same(pageHost, scroll.Content);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, true)]
    [InlineData(2, true, true)]
    public void WorkspaceShowsBackOnlyForNestedOrCompactDetailNavigation(
        int pageDepth, bool localBack, bool expected)
        => Assert.Equal(expected, ManagementWorkspace.ShouldShowBack(pageDepth, localBack));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplKeepsInputAndResultsInBoundedResponsivePanes(bool compact)
    {
        var input = new Border();
        var results = new Border();

        Grid layout = ScriptReplPage.BuildSplitLayout(input, results, compact);

        Assert.Same(input, layout.Children[0]);
        Assert.Same(results, layout.Children[1]);
        if (compact)
        {
            Assert.Equal(2, layout.RowDefinitions.Count);
            Assert.Equal(1, Grid.GetRow(results));
        }
        else
        {
            Assert.Equal(2, layout.ColumnDefinitions.Count);
            Assert.Equal(1, Grid.GetColumn(results));
        }
    }

    [Theory]
    [InlineData(false, 2, 0)]
    [InlineData(true, 0, 1)]
    public void ScriptWorkbenchPlacesSearchResponsively(
        bool compact,
        int expectedColumn,
        int expectedRow)
    {
        var buttons = new Border();
        var search = new TextBox();

        Grid toolbar = ScriptManagerPage.BuildToolbar(buttons, search, compact);

        Assert.Equal(0, Grid.GetRow(buttons));
        Assert.Equal(expectedColumn, Grid.GetColumn(search));
        Assert.Equal(expectedRow, Grid.GetRow(search));
        Assert.Equal(HorizontalAlignment.Stretch, search.HorizontalAlignment);
        if (!compact)
            Assert.Equal(110, toolbar.ColumnDefinitions[2].MaxWidth);
    }

    [Fact]
    public void ReplPlacesLargerEvaluateAtLeftAndThinSeedAtRight()
    {
        var evaluate = new Button();
        var seed = new TextBox { Width = 28, Height = 3 };

        Grid actions = ScriptReplPage.BuildInputActions(evaluate, seed);

        Assert.Same(evaluate, actions.Children[0]);
        Assert.Same(seed, actions.Children[1]);
        Assert.Equal(0, Grid.GetColumn(evaluate));
        Assert.Equal(2, Grid.GetColumn(seed));
        Assert.Equal(new Thickness(2, 0), evaluate.Padding);
        Assert.Equal(3, evaluate.MinHeight);
        Assert.Equal(28, seed.Width);
        Assert.Equal(3, seed.Height);
    }

    [Theory]
    [InlineData(Key.Enter, KeyModifiers.Control, true)]
    [InlineData(Key.Enter, KeyModifiers.None, false)]
    [InlineData(Key.A, KeyModifiers.Control, false)]
    public void ReplRecognizesOnlyItsControlShortcuts(
        Key key,
        KeyModifiers modifiers,
        bool expected)
        => Assert.Equal(expected, ScriptReplPage.IsReplShortcut(key, modifiers));

    [Fact]
    public void FilterSearchToolbarPlacesScopeLeftOfThinExpandingSearch()
    {
        var filters = new Border();
        var refresh = new Button();
        TextBox search = ManagementUi.SearchBox();
        var scope = new ComboBox();

        Grid toolbar = ManagementUi.FilterSearchToolbar(filters, refresh, search, scope);
        Grid searchRow = Assert.IsType<Grid>(toolbar.Children[2]);

        Assert.Equal(0, Grid.GetRow(filters));
        Assert.Equal(0, Grid.GetRow(refresh));
        Assert.Equal(1, Grid.GetRow(searchRow));
        Assert.Same(scope, searchRow.Children[0]);
        Assert.Same(search, searchRow.Children[1]);
        Assert.Equal(0, Grid.GetColumn(scope));
        Assert.Equal(1, Grid.GetColumn(search));
        Assert.Equal(3, search.Height);
        Assert.Equal(HorizontalAlignment.Stretch, search.HorizontalAlignment);
    }

    [Fact]
    public void FilterSearchToolbarLetsSearchUseTheWholeSecondRowWithoutALeadingField()
    {
        var filters = new Border();
        var refresh = new Button();
        TextBox search = ManagementUi.SearchBox();

        Grid toolbar = ManagementUi.FilterSearchToolbar(filters, refresh, search);

        Assert.Same(search, toolbar.Children[2]);
        Assert.Equal(1, Grid.GetRow(search));
        Assert.Equal(2, Grid.GetColumnSpan(search));
    }

    [Fact]
    public void CompactModalCardCentersAndWrapsItsContent()
    {
        Border card = ManagementUi.Card(new TextBlock(), compact: true);

        Assert.Equal(HorizontalAlignment.Center, card.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, card.VerticalAlignment);
        Assert.Equal(64, card.MaxWidth);
    }

    [Fact]
    public void VirtualizedListRowsTolerateTheTransientNullItemUsedDuringRecycling()
    {
        TextBlock recycled = ManagementUi.VirtualizedItemText<object>(null, static _ => "unreachable");
        TextBlock populated = ManagementUi.VirtualizedItemText("commands", static value => value);

        Assert.Equal(string.Empty, recycled.Text);
        Assert.Equal("commands", populated.Text);
    }

    [Fact]
    public void ReusableActionRowRetainsButtonOwnershipAcrossDetailRefreshes()
    {
        var detail = new StackPanel();
        var primary = new Button();
        var secondary = new Button();
        Control actions = ManagementUi.Actions(primary, secondary);

        detail.Children.Add(actions);
        detail.Children.Clear();
        detail.Children.Add(actions);

        Assert.Same(actions, detail.Children[0]);
        Assert.Same(actions, primary.Parent);
        Assert.Same(actions, secondary.Parent);
    }

    [Theory]
    [InlineData("inspect")]
    [InlineData("probe")]
    [InlineData("entity details")]
    [InlineData("--raw")]
    [InlineData("inspect 42")]
    public void CommandSearchCoversNameAliasDescriptionSyntaxFlagsAndExamples(string query)
        => Assert.True(CommandManualBrowserPage.Matches(Descriptor(), query));

    [Fact]
    public void CommandSearchRejectsUnrelatedText()
        => Assert.False(CommandManualBrowserPage.Matches(Descriptor(), "marketplace"));

    [Theory]
    [InlineData("--- test.bcn", "RemovedHeader")]
    [InlineData("+++ test.bcn (formatted)", "AddedHeader")]
    [InlineData("-  set value to 1", "Removed")]
    [InlineData("+    set value to 1", "Added")]
    [InlineData("@@ test.bcn:1 @@", "Hunk")]
    [InlineData(" unchanged", "Context")]
    public void ScriptDiffPreviewClassifiesInlineColours(string line, string expected)
        => Assert.Equal(expected, ScriptEditorPage.ClassifyDiffLine(line).ToString());

    [Fact]
    public void FormatPreviewKeepsActionsBelowAFlexibleDiffRegion()
    {
        var heading = new Border();
        var preview = new Border();
        var actions = new Border();

        Grid layout = ScriptEditorPage.BuildFormatPreviewLayout(heading, preview, actions);

        Assert.Equal(0, Grid.GetRow(heading));
        Assert.Equal(1, Grid.GetRow(preview));
        Assert.Equal(2, Grid.GetRow(actions));
        Assert.True(layout.RowDefinitions[1].Height.IsStar);
        Assert.Equal(VerticalAlignment.Stretch, layout.VerticalAlignment);
    }

    [Theory]
    [InlineData(RecipePlacementForm.None, false, true, "Unsupported")]
    [InlineData(RecipePlacementForm.ResourceName, true, true, "WrongForm")]
    [InlineData(RecipePlacementForm.NetworkId, false, true, "WrongForm")]
    [InlineData(RecipePlacementForm.ResourceName, false, false, "MissingMenu")]
    [InlineData(RecipePlacementForm.NetworkId, true, false, "MissingMenu")]
    [InlineData(RecipePlacementForm.ResourceName, false, true, "Enabled")]
    [InlineData(RecipePlacementForm.NetworkId, true, true, "Enabled")]
    public void RecipeActionAvailabilityExplainsProtocolFormAndMenuGates(
        RecipePlacementForm form,
        bool numeric,
        bool menuOpen,
        string expected)
    {
        var support = new RecipePlacementSupport(form, "test", 0);
        Assert.Equal(expected, RecipeBrowserPage.ActionAvailability(support, numeric, menuOpen).ToString());
    }

    private static CommandDescriptor Descriptor()
        => new(
            "inspect",
            "Show entity details.",
            "inspect <id> [--raw]",
            CommandCategory.Entities,
            ImmutableArray.Create("probe"),
            ImmutableArray.Create(new UsageLine("<id>", "Select an entity.")),
            ImmutableArray.Create(new UsageFlag("--raw", "Show raw values.")),
            ImmutableArray.Create("inspect 42"),
            CommandFeature.Entity,
            ImmutableArray.Create("entity"),
            "entities",
            true);
}
