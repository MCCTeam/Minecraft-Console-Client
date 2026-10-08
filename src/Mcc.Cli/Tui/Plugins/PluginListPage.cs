using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using DMCBK.Core.Plugins;
using DMCBK.Core.Presentation;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// The manager's root page: every discovered plugin as a selectable row plus filter chips and the footer tools.
/// Mirrors <c>plugins list</c> (states, entry kinds, offline and not-loaded notes) with the outdated markers of <c>plugins outdated</c> folded in when a market is attached.
/// </summary>
internal sealed class PluginListPage : StackPanel, IRefreshablePage
{
    private readonly PluginManagerContext _ctx;
    private readonly StackPanel _rows = new();
    private readonly StackPanel _chips = new() { Orientation = Avalonia.Layout.Orientation.Horizontal };
    private readonly TextBox _search = PluginUi.SearchBox();
    private readonly Grid _filterBar = new();
    private readonly TextBlock _summary = new() { Foreground = PluginUi.Soft };
    private readonly List<Button> _rowButtons = new();
    private readonly List<PluginInfo> _shown = new();
    private readonly List<PluginInfo> _plugins = new();
    private Dictionary<string, string> _outdatedById = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _localIds = new(StringComparer.OrdinalIgnoreCase);
    private string _filter = Strings.PmFilterAll;
    private int _selected;
    private bool _compactFilters;

    public PluginListPage(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        _ctx = ctx;
        // Section titles already reserve a row above themselves.
        // Extra panel spacing compounded with that margin and each tool button's bottom margin, leaving the compact TUI page needlessly sparse.
        Spacing = 0;

        Children.Add(PluginUi.PageHeader(Strings.PmLibrary, Strings.PmManagerSubtitle));
        Children.Add(_summary);
        _search.MinWidth = 24;
        _search.MaxWidth = 110;
        _filterBar.Children.Add(_chips);
        _filterBar.Children.Add(_search);
        ApplyFilterLayout(_filterBar, _chips, _search, compact: false);
        Children.Add(PluginUi.Card(_filterBar));
        Children.Add(PluginUi.Card(_rows));

        _search.TextChanged += (_, _) => RebuildRows(focusRows: false);

        Children.Add(PluginUi.SectionTitle(Strings.PmDiscover));
        var discover = new WrapPanel();
        AddTool(discover, Strings.PmInstall, () => PluginInstallForm.Open(_ctx), PluginButtonKind.Primary);
        AddTool(discover, Strings.PmSearch, () => PluginInstallForm.OpenSearch(_ctx));
        AddTool(discover, Strings.PmUpdates, () => _ctx.Show(new PluginUpdatesPage(_ctx)));
        AddTool(discover, Strings.PmMarketplaces, () => _ctx.Show(new PluginMarketplacesPage(_ctx)));
        Children.Add(discover);

        Children.Add(PluginUi.SectionTitle(Strings.PmMaintenance));
        var maintenance = new WrapPanel();
        AddTool(maintenance, Strings.PmUpdateAll, () => _ = UpdateAllAsync());
        AddTool(maintenance, Strings.PmReloadAll, () => _ = RunHostAsync(
            static (host, ct) => host.ReloadAllAsync(ct)));
        AddTool(maintenance, Strings.PmDoctor,
            () => _ctx.Show(new PluginToolsPage(_ctx, PluginToolsPage.Tool.Doctor)));
        AddTool(maintenance, Strings.PmNew, () => PluginToolsForms.OpenNew(_ctx));
        AddTool(maintenance, Strings.PmValidate,
            () => _ctx.Show(new PluginToolsPage(_ctx, PluginToolsPage.Tool.Validate)));
        AddTool(maintenance, Strings.PmLoad, () => PluginToolsForms.OpenLoad(_ctx));
        Children.Add(maintenance);

        _ctx.Owner.SetHints(Strings.PmListHints);
        KeyDown += OnKeyDown;
        Focusable = true;
        _ = RefreshAsync();
    }

    /// <summary>Focuses the selected row once shown (a focused row is what makes arrows+Enter live).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_selected < _rowButtons.Count)
            _rowButtons[_selected].Focus();
        else
            Focus();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        bool compact = e.NewSize.Width < 120;
        if (compact == _compactFilters)
            return;

        _compactFilters = compact;
        ApplyFilterLayout(_filterBar, _chips, _search, compact);
    }

    internal static void ApplyFilterLayout(Grid host, Control chips, Control search, bool compact)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(chips);
        ArgumentNullException.ThrowIfNull(search);

        if (compact)
        {
            host.ColumnDefinitions = new ColumnDefinitions("*");
            host.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetColumn(chips, 0);
            Grid.SetRow(chips, 0);
            Grid.SetColumn(search, 0);
            Grid.SetRow(search, 1);
            search.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            return;
        }

        host.ColumnDefinitions = new ColumnDefinitions("Auto,*,8*");
        host.ColumnDefinitions[2].MinWidth = 24;
        host.ColumnDefinitions[2].MaxWidth = 110;
        host.RowDefinitions = new RowDefinitions("Auto");
        Grid.SetColumn(chips, 0);
        Grid.SetRow(chips, 0);
        Grid.SetColumn(search, 2);
        Grid.SetRow(search, 0);
        search.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
    }

    /// <summary>Re-reads the host (and the market for markers) and rebuilds the rows.</summary>
    public async Task RefreshAsync()
    {
        _chips.Children.Clear();

        if (_ctx.Host is not { } host)
        {
            _ctx.Owner.SetStatus(Strings.PmNoHost);
            _plugins.Clear();
            RebuildRows(focusRows: false);
            return;
        }

        List<PluginInfo> plugins = [.. host.List()];
        plugins.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
        _plugins.Clear();
        _plugins.AddRange(plugins);
        _summary.Text = Strings.PmLibrarySummary(
            plugins.Count,
            plugins.Count(static p => p.Enabled),
            plugins.Count(static p => p.Loaded));

        _outdatedById = await OutdatedMarkersAsync().ConfigureAwait(true);
        _localIds = await LocalIdsAsync().ConfigureAwait(true);
        RebuildChips(plugins, _outdatedById, _localIds);
        RebuildRows(focusRows: true);
    }

    private void RebuildRows(bool focusRows)
    {
        string? selectedId = _selected < _shown.Count ? _shown[_selected].Id : null;
        _rows.Children.Clear();
        _rowButtons.Clear();
        _shown.Clear();

        string query = (_search.Text ?? string.Empty).Trim();
        IEnumerable<PluginInfo> shown = ApplyFilter(_plugins, _outdatedById, _localIds)
            .Where(plugin => MatchesSearch(plugin, query));

        foreach (PluginInfo plugin in shown)
        {
            string outdated = _outdatedById.TryGetValue(plugin.Id, out string? available)
                ? available
                : string.Empty;
            string id = plugin.Id;
            Button row = PluginUi.RowButton(
                BuildRowText(_ctx.Glyphs, plugin, outdated),
                () => _ctx.Show(new PluginDetailPage(_ctx, id)));
            _shown.Add(plugin);
            _rowButtons.Add(row);
            _rows.Children.Add(row);
        }

        if (_shown.Count == 0)
        {
            _rows.Children.Add(new TextBlock
            {
                Text = query.Length > 0
                    ? Strings.PmSearchNone(query)
                    : string.Equals(_filter, Strings.PmFilterAll, StringComparison.Ordinal)
                    ? Strings.PmNoneDiscovered
                    : Strings.PmListEmpty(_filter),
                Foreground = PluginUi.Soft,
            });
        }

        int preservedIndex = selectedId is null
            ? -1
            : _shown.FindIndex(plugin => string.Equals(plugin.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        _selected = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(_selected, 0, Math.Max(0, _rowButtons.Count - 1));
        if (focusRows && _rowButtons.Count > 0)
            _rowButtons[_selected].Focus();
    }

    internal static bool MatchesSearch(PluginInfo plugin, string query)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        return string.IsNullOrWhiteSpace(query)
            || plugin.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            || plugin.Version.Contains(query, StringComparison.OrdinalIgnoreCase)
            || plugin.Entry.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || (plugin.Status?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (plugin.LastError?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary>
    /// One list row, pure so it is unit-testable.
    /// Glyphs come from the host-resolved set (checkbox ballot boxes included), never hardcoded.
    /// </summary>
    internal static string BuildRowText(GlyphSet glyphs, PluginInfo plugin, string outdatedTo)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        ArgumentNullException.ThrowIfNull(plugin);
        string state = plugin.Enabled ? glyphs.PluginEnabled : glyphs.PluginDisabled;
        string entry = plugin.Entry == PluginEntryKind.Source ? Strings.PmEntrySource : Strings.PmEntryDll;
        var text = new System.Text.StringBuilder($"{state} {plugin.Id} {plugin.Version} ({entry})");
        if (plugin.Offline)
            text.Append(' ').Append(Strings.PmOfflineTag);

        if (outdatedTo.Length > 0)
            text.Append(' ').Append(Strings.PmOutdatedTag(outdatedTo));

        if (plugin.Enabled && !plugin.Loaded)
        {
            text.Append(' ').Append(Strings.PmNotLoadedTag(
                string.IsNullOrWhiteSpace(plugin.Status) ? Strings.PmInfoNotLoaded : plugin.Status));
        }

        return text.ToString();
    }

    private void AddTool(
        WrapPanel tools,
        string label,
        Action open,
        PluginButtonKind kind = PluginButtonKind.Secondary)
    {
        Button button = PluginUi.Button(label, open, kind);
        button.Margin = new Thickness(0, 0, 1, 0);
        tools.Children.Add(button);
    }

    private void RebuildChips(
        List<PluginInfo> plugins, Dictionary<string, string> outdatedById, HashSet<string> localIds)
    {
        foreach (string filter in new[]
                 {
                     Strings.PmFilterAll, Strings.PmFilterOutdated, Strings.PmFilterLocal,
                     Strings.PmFilterEnabled, Strings.PmFilterDisabled,
                 })
        {
            int count = filter switch
            {
                var f when f == Strings.PmFilterOutdated => outdatedById.Count,
                var f when f == Strings.PmFilterLocal => localIds.Count,
                var f when f == Strings.PmFilterEnabled => plugins.Count(static p => p.Enabled),
                var f when f == Strings.PmFilterDisabled => plugins.Count(static p => !p.Enabled),
                _ => plugins.Count,
            };

            bool active = string.Equals(filter, _filter, StringComparison.Ordinal);
            Button chip = PluginUi.Button(Strings.PmFilterCount(filter, count), () =>
            {
                _filter = filter;
                _selected = 0;
                _ = RefreshAsync();
            }, active ? PluginButtonKind.Primary : PluginButtonKind.Quiet);
            chip.Margin = new Thickness(0, 0, 1, 0);
            _chips.Children.Add(chip);
        }
    }

    private IEnumerable<PluginInfo> ApplyFilter(
        List<PluginInfo> plugins, Dictionary<string, string> outdatedById, HashSet<string> localIds)
    {
        if (string.Equals(_filter, Strings.PmFilterEnabled, StringComparison.Ordinal))
            return plugins.Where(static p => p.Enabled);

        if (string.Equals(_filter, Strings.PmFilterDisabled, StringComparison.Ordinal))
            return plugins.Where(static p => !p.Enabled);

        if (string.Equals(_filter, Strings.PmFilterOutdated, StringComparison.Ordinal))
            return plugins.Where(p => outdatedById.ContainsKey(p.Id));

        if (string.Equals(_filter, Strings.PmFilterLocal, StringComparison.Ordinal))
            return plugins.Where(p => localIds.Contains(p.Id));

        return plugins;
    }

    /// <summary>
    /// The ids the market knows as local (hand-installed, unmanaged).
    /// Empty without a market, in which case the filter simply matches nothing; the status line already says the market is missing the first time a market verb is needed.
    /// </summary>
    private async Task<HashSet<string>> LocalIdsAsync()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_ctx.Market is not { } market
            || !string.Equals(_filter, Strings.PmFilterLocal, StringComparison.Ordinal))
            return ids;

        if (_ctx.Host is not { } host)
            return ids;

        foreach (PluginInfo plugin in host.List())
        {
            try
            {
                if (await market.InfoAsync(plugin.Id).ConfigureAwait(true) is { IsLocal: true })
                    ids.Add(plugin.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ctx.Owner.SetStatus(ex.Message);
            }
        }

        return ids;
    }

    private async Task<Dictionary<string, string>> OutdatedMarkersAsync()
    {
        var markers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_ctx.Market is not { } market)
            return markers;

        try
        {
            foreach (PluginUpdateInfo update in await market.OutdatedAsync().ConfigureAwait(true))
            {
                if (update.AvailableVersion is { Length: > 0 } available && update.Error is not { Length: > 0 })
                    markers[update.Id] = available;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        return markers;
    }

    private async Task UpdateAllAsync()
    {
        if (_ctx.Market is null)
        {
            _ctx.Owner.SetStatus(Strings.PmNoMarket);
            return;
        }

        _ctx.Owner.SetStatus(Strings.PmWorking);
        try
        {
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.UpdateAsync(null, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ = RefreshAsync();
    }

    private async Task RunHostAsync(Func<IPluginHost, CancellationToken, Task<PluginActionResult>> op)
    {
        if (_ctx.Host is null)
        {
            _ctx.Owner.SetStatus(Strings.PmNoHost);
            return;
        }

        try
        {
            PluginActionResult result = await op(_ctx.Host, CancellationToken.None).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ = RefreshAsync();
    }

    private void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        // Search owns every letter while it is focused.
        // Page shortcuts only apply when focus is on the list or another non-editable control, matching the detail page's editor guard.
        if (!ShouldHandlePageShortcut(e.Source) || _rowButtons.Count == 0)
            return;

        switch (e.Key)
        {
            case Avalonia.Input.Key.Down:
                _selected = Math.Min(_selected + 1, _rowButtons.Count - 1);
                _rowButtons[_selected].Focus();
                e.Handled = true;
                break;
            case Avalonia.Input.Key.Up:
                _selected = Math.Max(_selected - 1, 0);
                _rowButtons[_selected].Focus();
                e.Handled = true;
                break;
            case Avalonia.Input.Key.E:
                _ = ToggleAsync(_selected, enable: true);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.D:
                _ = ToggleAsync(_selected, enable: false);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.R:
                _ = ActOnSelectedAsync(_selected, static (host, id, ct) => host.ReloadAsync(id, ct));
                e.Handled = true;
                break;
            case Avalonia.Input.Key.U:
                if (_selected < _shown.Count)
                    _ctx.Show(new PluginDetailPage(_ctx, _shown[_selected].Id, openUninstall: true));

                e.Handled = true;
                break;
            case Avalonia.Input.Key.Enter:
                if (_selected < _shown.Count)
                    _ctx.Show(new PluginDetailPage(_ctx, _shown[_selected].Id));

                e.Handled = true;
                break;
        }
    }

    internal static bool ShouldHandlePageShortcut(object? source) => source is not TextBox;

    private async Task ToggleAsync(int index, bool enable)
    {
        if (index < _shown.Count)
        {
            await ActOnSelectedAsync(index, enable
                ? static (host, id, ct) => host.EnableAsync(id, ct)
                : static (host, id, ct) => host.DisableAsync(id, ct)).ConfigureAwait(true);
        }
    }

    private async Task ActOnSelectedAsync(
        int index, Func<IPluginHost, string, CancellationToken, Task<PluginActionResult>> op)
    {
        if (_ctx.Host is null || index >= _shown.Count)
            return;

        try
        {
            PluginActionResult result = await op(_ctx.Host, _shown[index].Id, CancellationToken.None)
                .ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ = RefreshAsync();
    }
}
