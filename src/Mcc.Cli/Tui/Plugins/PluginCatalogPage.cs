using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// Every plugin available from the on-disk marketplace catalogues, optionally narrowed to one marketplace.
/// Each row exposes the action that matches its current installation state.
/// </summary>
internal sealed class PluginCatalogPage : StackPanel, IRefreshablePage
{
    private readonly PluginManagerContext _ctx;
    private readonly string? _marketplace;
    private readonly TextBlock _summary = new() { Foreground = PluginUi.Gold };
    private readonly StackPanel _rows = new() { Spacing = 1 };
    private readonly TextBox _search = PluginUi.SearchBox();
    private readonly Button _showInstalledButton;
    private readonly Grid _filterBar = new();
    private readonly Control _installedFilter;
    private IReadOnlyList<MarketplaceSearchResult> _available = [];
    private bool _showInstalled = true;
    private bool _compactFilters;
    private Control? _firstAction;

    public PluginCatalogPage(PluginManagerContext ctx, string? marketplace = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        _ctx = ctx;
        _marketplace = string.IsNullOrWhiteSpace(marketplace) ? null : marketplace;
        Spacing = 1;
        Focusable = true;

        string title = _marketplace is null
            ? Strings.PmAvailablePluginsTitle
            : Strings.PmMarketplacePluginsTitle(_marketplace);
        string subtitle = _marketplace is null
            ? Strings.PmAvailablePluginsSubtitle
            : Strings.PmMarketplacePluginsSubtitle(_marketplace);
        Children.Add(PluginUi.PageHeader(title, subtitle));
        Children.Add(_summary);

        _showInstalledButton = PluginUi.Button(string.Empty, ToggleInstalled, PluginButtonKind.Quiet);
        UpdateInstalledToggle();
        _installedFilter = PluginUi.Actions(_showInstalledButton);
        _search.MinWidth = 24;
        _search.MaxWidth = 110;
        _filterBar.Children.Add(_installedFilter);
        _filterBar.Children.Add(_search);
        PluginListPage.ApplyFilterLayout(_filterBar, _installedFilter, _search, compact: false);
        Children.Add(_filterBar);
        Children.Add(_rows);

        _search.TextChanged += (_, _) => ApplyFilter(focusAction: false);

        _ctx.Owner.SetHints(Strings.PmAvailablePluginsHints);
        _ = RefreshAsync();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        bool compact = e.NewSize.Width < 120;
        if (compact == _compactFilters)
            return;

        _compactFilters = compact;
        PluginListPage.ApplyFilterLayout(
            _filterBar,
            _installedFilter,
            _search,
            compact);
    }

    public async Task RefreshAsync()
    {
        _rows.Children.Clear();
        _firstAction = null;
        if (_ctx.Market is not { } market)
        {
            _available = [];
            _summary.Text = Strings.PmNoMarket;
            _summary.Foreground = PluginUi.Red;
            return;
        }

        try
        {
            _available = await market.SearchAsync(string.Empty, _marketplace).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _available = [];
            _summary.Text = ex.Message;
            _summary.Foreground = PluginUi.Red;
            return;
        }

        ApplyFilter(focusAction: true);
    }

    private void ApplyFilter(bool focusAction)
    {
        _rows.Children.Clear();
        _firstAction = null;
        string query = (_search.Text ?? string.Empty).Trim();
        List<MarketplaceSearchResult> shown = _available
            .Where(plugin => _showInstalled || string.IsNullOrWhiteSpace(plugin.Installed))
            .Where(plugin => MatchesSearch(plugin, query))
            .ToList();

        _summary.Text = Strings.PmAvailablePluginsHeader(shown.Count);
        _summary.Foreground = PluginUi.Gold;
        if (shown.Count == 0)
        {
            _rows.Children.Add(new TextBlock
            {
                Text = query.Length > 0
                    ? Strings.PmSearchNone(query)
                    : _available.Count > 0
                    ? Strings.PmNoFilteredPlugins
                    : _marketplace is null
                    ? Strings.PmNoAvailablePlugins
                    : Strings.PmNoMarketplacePlugins(_marketplace),
                Foreground = PluginUi.Soft,
                TextWrapping = TextWrapping.Wrap,
            });
            return;
        }

        foreach (MarketplaceSearchResult plugin in shown)
            _rows.Children.Add(BuildRow(plugin));

        if (focusAction)
            _firstAction?.Focus();
    }

    internal static string ActionLabel(bool installed)
        => installed ? Strings.PmUninstall : Strings.PmInstall;

    internal static bool MatchesSearch(MarketplaceSearchResult plugin, string query)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        return string.IsNullOrWhiteSpace(query)
            || plugin.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            || plugin.Marketplace.Contains(query, StringComparison.OrdinalIgnoreCase)
            || plugin.Version.Contains(query, StringComparison.OrdinalIgnoreCase)
            || plugin.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
            || plugin.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))
            || plugin.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (plugin.Installed?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ToggleInstalled()
    {
        _showInstalled = !_showInstalled;
        UpdateInstalledToggle();
        ApplyFilter(focusAction: false);
    }

    private void UpdateInstalledToggle()
    {
        _showInstalledButton.Content = $"{Strings.PmShowInstalled}: "
            + (_showInstalled ? Strings.MgmtOn : Strings.MgmtOff);
    }

    private Control BuildRow(MarketplaceSearchResult plugin)
    {
        bool installed = !string.IsNullOrWhiteSpace(plugin.Installed);
        var details = new StackPanel { Spacing = 0 };
        details.Children.Add(new TextBlock
        {
            Text = $"{plugin.Id} · {plugin.Version} · {plugin.Marketplace}",
            Foreground = PluginUi.White,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
        });
        if (!string.IsNullOrWhiteSpace(plugin.Description))
        {
            details.Children.Add(new TextBlock
            {
                Text = plugin.Description,
                Foreground = PluginUi.Muted,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        if (installed)
        {
            details.Children.Add(new TextBlock
            {
                Text = Strings.PmSearchInstalled(plugin.Installed!),
                Foreground = PluginUi.Soft,
            });
        }

        if (plugin.Incompatibility is { Length: > 0 } incompatibility)
        {
            details.Children.Add(new TextBlock
            {
                Text = incompatibility,
                Foreground = PluginUi.Red,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        Button action = PluginUi.Button(
            ActionLabel(installed),
            installed
                ? () => PluginUninstallDialog.Open(_ctx, plugin.Id, () => _ = RefreshAsync())
                : () => _ = InstallAsync(plugin),
            installed ? PluginButtonKind.Danger : PluginButtonKind.Primary);
        action.IsEnabled = installed || string.IsNullOrWhiteSpace(plugin.Incompatibility);
        action.HorizontalAlignment = HorizontalAlignment.Right;
        action.VerticalAlignment = VerticalAlignment.Center;
        action.Margin = new Thickness(2, 0, 0, 0);
        if (action.IsEnabled)
            _firstAction ??= action;

        var line = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        line.Children.Add(details);
        Grid.SetColumn(action, 1);
        line.Children.Add(action);
        return PluginUi.Card(line);
    }

    private async Task InstallAsync(MarketplaceSearchResult plugin)
    {
        if (_ctx.Market is null)
            return;

        _ctx.Owner.SetStatus(Strings.PmWorking);
        try
        {
            string reference = $"{plugin.Id}@{plugin.Marketplace}";
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.InstallAsync(reference, null, false, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        await RefreshAsync().ConfigureAwait(true);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_firstAction is not null)
            _firstAction.Focus();
        else
            Focus();
    }
}
