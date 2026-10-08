using Avalonia.Controls;
using Avalonia.Layout;
using Mcc.Cli.Tui.Plugins;
using DMCBK.Core;
using DMCBK.Core.Commands;
using DMCBK.Core.Configuration;
using DMCBK.Core.Plugins;
using Umpk.Auth;
using Umpk.Client;
using Xunit;

namespace Mcc.Cli.Tests.Commands;

/// <summary>
/// The <c>plugins ui</c> entry point and the settings editor's TOML layer.
/// Dialog rendering needs a live UI and is verified by hand; everything below it is pure and pinned here: the verb falls back to text without a dialog host, rows parse out of real-world settings files, edits render back to valid TOML, and merging touches only the edited lines.
/// </summary>
public sealed class PluginsUiCommandTests
{
    [Fact]
    public void InstalledPluginShortcutsDoNotCaptureSearchTyping()
    {
        Assert.False(PluginListPage.ShouldHandlePageShortcut(new Avalonia.Controls.TextBox()));
        Assert.True(PluginListPage.ShouldHandlePageShortcut(new Avalonia.Controls.Button()));
    }

    [Theory]
    [InlineData(false, 2, 0)]
    [InlineData(true, 0, 1)]
    public void InstalledPluginFiltersPlaceSearchResponsively(
        bool compact,
        int expectedColumn,
        int expectedRow)
    {
        var host = new Avalonia.Controls.Grid();
        var chips = new Avalonia.Controls.StackPanel();
        var search = new Avalonia.Controls.TextBox();
        host.Children.Add(chips);
        host.Children.Add(search);

        PluginListPage.ApplyFilterLayout(host, chips, search, compact);

        Assert.Equal(expectedColumn, Avalonia.Controls.Grid.GetColumn(search));
        Assert.Equal(expectedRow, Avalonia.Controls.Grid.GetRow(search));
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Stretch, search.HorizontalAlignment);
        if (!compact)
            Assert.Equal(110, host.ColumnDefinitions[2].MaxWidth);
    }

    private const string Sample = """
        # Alerts settings (regenerated with commented defaults on first run if deleted).
        Enabled = true # Set to false to disable Alerts without unloading it.
        UseRegex = false # Default for rules.
        # Case-insensitive suppression substrings. Any match here cancels the alert.
        Excludes = [ "joined the game", "left the game" ]
        RouteMode = "fixed" # fixed sends every alert to all its routes.
        Walk_Range = 5 # Maximum offset, in blocks. Must be positive.
        Cast_Delay = 0.4 # Seconds to wait before every cast.
        # A dotted key the editor cannot model.
        deep.key = 1
        [Delay]
        min = 60.0
        max = 60.0
        Broken = [ "a",
        """;

    [Fact]
    public async Task PluginsUi_WithoutDialogHost_FallsBackToText()
    {
        var builder = new ClientBuilder().UseCommands().UseBeacon()
            .UseUsername("Tester")
            .UseServer("localhost")
            .UseHostInterface(new NoDialogHost());
        await using Client client = builder.Build();

        CmdResult result = await client.Commands.DispatchAsync("plugins ui");

        Assert.Equal(CmdStatus.Fail, result.Status);
        Assert.Contains("dialog", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRows_ReadsScalarsArraysAndSections()
    {
        IReadOnlyList<SettingRow> rows = PluginSettingsModel.ParseRows(Sample);

        SettingRow enabled = Assert.Single(rows, r => r.Key == "Enabled");
        Assert.Equal(SettingValueKind.Bool, enabled.Kind);
        Assert.Equal("true", enabled.Value);
        Assert.Contains("without unloading", enabled.Comment, StringComparison.Ordinal);
        Assert.DoesNotContain("#", enabled.Comment, StringComparison.Ordinal);

        SettingRow excludes = Assert.Single(rows, r => r.Key == "Excludes");
        Assert.Equal(SettingValueKind.StringList, excludes.Kind);
        Assert.Equal("joined the game, left the game", excludes.Value);
        Assert.Contains("cancels the alert", excludes.Comment, StringComparison.Ordinal);

        SettingRow route = Assert.Single(rows, r => r.Key == "RouteMode");
        Assert.Equal(SettingValueKind.Text, route.Kind);

        Assert.Equal(SettingValueKind.Integer, Assert.Single(rows, r => r.Key == "Walk_Range").Kind);
        Assert.Equal(SettingValueKind.Float, Assert.Single(rows, r => r.Key == "Cast_Delay").Kind);

        SettingRow delay = Assert.Single(rows, r => r.Kind == SettingValueKind.Section);
        Assert.Equal("Delay", delay.Section);
        Assert.Equal(SettingValueKind.Float, Assert.Single(rows, r => r.Key == "min").Kind);

        // Unmodeled shapes are skipped (dotted keys) or read-only (multiline arrays), never dropped: ApplyEdits with no edits is the identity.
        Assert.DoesNotContain("key", rows.Select(r => r.Key));
        Assert.Equal(
            SettingValueKind.Unsupported,
            Assert.Single(rows, r => r.Key == "Broken").Kind);
        Assert.Equal(Sample, PluginSettingsModel.ApplyEdits(Sample, []));
    }

    [Theory]
    [InlineData(false, "Pin")]
    [InlineData(true, "Unpin")]
    public void PluginDetail_PinActionLabelReflectsMarketplaceState(bool pinned, string expected)
        => Assert.Equal(expected, PluginDetailPage.PinActionLabel(pinned));

    [Theory]
    [InlineData(false, "Install")]
    [InlineData(true, "Uninstall")]
    public void PluginCatalog_ActionLabelReflectsInstallationState(bool installed, string expected)
        => Assert.Equal(expected, PluginCatalogPage.ActionLabel(installed));

    [Theory]
    [InlineData("auto", true)]
    [InlineData("1.0", true)]
    [InlineData("load error", true)]
    [InlineData("missing", false)]
    public void InstalledPluginSearchMatchesUsefulMetadata(string query, bool expected)
    {
        var plugin = new PluginInfo(
            "auto-attack",
            "1.0.0",
            PluginEntryKind.Source,
            Enabled: true,
            Loaded: false,
            Status: "Load error");

        Assert.Equal(expected, PluginListPage.MatchesSearch(plugin, query));
    }

    [Theory]
    [InlineData("alerts", true)]
    [InlineData("notifications", true)]
    [InlineData("local", true)]
    [InlineData("2.0", true)]
    [InlineData("missing", false)]
    public void MarketplacePluginSearchMatchesCatalogueMetadata(string query, bool expected)
    {
        var plugin = new MarketplaceSearchResult(
            "alerts",
            "local",
            "2.0.0",
            "Watches chat",
            ["notifications"],
            "plugins/alerts",
            null,
            "1.0.0");

        Assert.Equal(expected, PluginCatalogPage.MatchesSearch(plugin, query));
    }

    [Fact]
    public void PluginSearchFieldIsThinLeftAlignedAndWidthCapped()
    {
        TextBox search = PluginUi.SearchBox();

        Assert.Equal(3, search.Height);
        Assert.Equal(72, search.MaxWidth);
        Assert.Equal(HorizontalAlignment.Stretch, search.HorizontalAlignment);
    }

    [Theory]
    [InlineData(-1, 2, false)]
    [InlineData(0, 0, false)]
    [InlineData(0, 2, true)]
    [InlineData(1, 2, true)]
    [InlineData(2, 2, false)]
    public void MarketplaceActionsRequireAnExplicitValidSelection(
        int selected, int count, bool expected)
        => Assert.Equal(expected, PluginMarketplacesPage.IsValidSelection(selected, count));

    [Fact]
    public void PluginWorkspaceCanGiveMarketplaceTableItsOwnFixedViewport()
    {
        var pageHost = new ContentControl();
        var scroll = new ScrollViewer { Content = pageHost };
        var viewport = new ContentControl { Content = scroll };

        PluginManagerOverlay.SetPageScrollMode(viewport, scroll, pageHost, false);

        Assert.Same(pageHost, viewport.Content);
        Assert.Null(scroll.Content);

        PluginManagerOverlay.SetPageScrollMode(viewport, scroll, pageHost, false);

        Assert.Same(pageHost, viewport.Content);
        Assert.Null(scroll.Content);

        PluginManagerOverlay.SetPageScrollMode(viewport, scroll, pageHost, true);

        Assert.Same(scroll, viewport.Content);
        Assert.Same(pageHost, scroll.Content);

        PluginManagerOverlay.SetPageScrollMode(viewport, scroll, pageHost, true);

        Assert.Same(scroll, viewport.Content);
        Assert.Same(pageHost, scroll.Content);
    }

    [Theory]
    [InlineData(SettingValueKind.Bool, "true", "true")]
    [InlineData(SettingValueKind.Integer, " 42 ", "42")]
    [InlineData(SettingValueKind.Float, "0.4", "0.4")]
    [InlineData(SettingValueKind.Text, "hi", "\"hi\"")]
    [InlineData(SettingValueKind.StringList, "a, b", "[ \"a\", \"b\" ]")]
    [InlineData(SettingValueKind.StringList, "", "[  ]")]
    public void TryRender_AcceptsValidText(SettingValueKind kind, string display, string expected)
    {
        var row = new SettingRow(0, string.Empty, "K", string.Empty, kind, string.Empty);

        Assert.True(PluginSettingsModel.TryRender(row, display, out string rendered, out string? error));
        Assert.Null(error);
        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData(SettingValueKind.Bool, "maybe")]
    [InlineData(SettingValueKind.Integer, "4.5")]
    [InlineData(SettingValueKind.Float, "lots")]
    [InlineData(SettingValueKind.Unsupported, "anything")]
    public void TryRender_RejectsInvalidText(SettingValueKind kind, string display)
    {
        var row = new SettingRow(0, string.Empty, "K", string.Empty, kind, string.Empty);

        Assert.False(PluginSettingsModel.TryRender(row, display, out _, out string? error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void ApplyEdits_ReplacesValuesAndKeepsEverythingElse()
    {
        IReadOnlyList<SettingRow> rows = PluginSettingsModel.ParseRows(Sample);
        var edits = new List<SettingEdit>();
        foreach (SettingRow row in rows)
        {
            string display = row.Key switch
            {
                "Enabled" => "false",
                "Walk_Range" => "8",
                "Excludes" => "hi",
                _ => row.Value,
            };
            if (!string.Equals(display, row.Value, StringComparison.Ordinal)
                && PluginSettingsModel.TryRender(row, display, out string rendered, out _))
                edits.Add(new SettingEdit(row.LineIndex, rendered));
        }

        string merged = PluginSettingsModel.ApplyEdits(Sample, edits);
        string[] lines = merged.Split('\n');

        // The edited values changed in place, comments and spacing intact.
        Assert.Contains("Enabled = false # Set to false", merged, StringComparison.Ordinal);
        Assert.Contains("Walk_Range = 8 # Maximum offset", merged, StringComparison.Ordinal);
        Assert.Contains("Excludes = [ \"hi\" ]", merged, StringComparison.Ordinal);

        // Everything else byte-identical, including the shapes the editor cannot model.
        string[] original = Sample.Split('\n');
        Assert.Equal(original.Length, lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            bool edited = rows.Any(r => r.LineIndex == i
                && (r.Key is "Enabled" or "Walk_Range" or "Excludes"));
            Assert.Equal(edited, lines[i] != original[i]);
        }

        Assert.Contains("deep.key = 1", merged, StringComparison.Ordinal);
    }

    private sealed class NoDialogHost : IHostInterface
    {
        public IUserPrompt? Prompt => null;

        public IAuthInteraction? AuthInteraction => null;

        public ICommandOutput? CommandOutput => null;

        public IHostUi? Ui => null;
    }
}
