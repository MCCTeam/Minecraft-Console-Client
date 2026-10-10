using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// The marketplaces page: every added marketplace with source, plugin count, auto-update policy, refresh state and errors; per-row refresh/remove, a policy cycler, an add form, and refresh-all.
/// Removing forgets the catalogue; installed plugins stay and go unmanaged (said inline, as the text verb does).
/// </summary>
internal sealed class PluginMarketplacesPage : Grid, IRefreshablePage, IOverlayPage
{
    private const string TableColumns = "16,*,9,10,24";

    private readonly PluginManagerContext _ctx;
    private readonly TextBlock _summary;
    private readonly ListBox _list;
    private readonly Button _pluginsButton;
    private readonly Button _policyButton;
    private readonly Button _refreshButton;
    private readonly Button _removeButton;
    private readonly Control _selectionActions;
    private readonly List<MarketplaceInfo> _shown = new();
    private int _selected = -1;

    // The three policies a marketplace may carry (DMCBK.Core.Plugins.AutoUpdatePolicy).
    private static readonly string[] Policies =
        [AutoUpdatePolicy.Off, AutoUpdatePolicy.Check, AutoUpdatePolicy.Apply];

    public PluginMarketplacesPage(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        _ctx = ctx;
        RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto");
        Focusable = true;

        Control header = PluginUi.PageHeader(Strings.PmMarketsTitle, Strings.PmMarketsSubtitle);
        Children.Add(header);
        _summary = new TextBlock
        {
            Foreground = PluginUi.Gold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 1),
        };
        Grid.SetRow(_summary, 1);
        Children.Add(_summary);

        _list = new ListBox
        {
            MinHeight = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = PluginUi.Panel,
            Foreground = PluginUi.White,
            SelectionMode = SelectionMode.Single,
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_list, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        _list.SelectionChanged += (_, _) => Select(_list.SelectedIndex);
        _list.KeyDown += OnKeyDown;

        var table = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        table.Children.Add(BuildTableHeader());
        Grid.SetRow(_list, 1);
        table.Children.Add(_list);
        Border tableCard = PluginUi.Card(table);
        tableCard.VerticalAlignment = VerticalAlignment.Stretch;
        Grid.SetRow(tableCard, 2);
        Children.Add(tableCard);

        _pluginsButton = PluginUi.Button(
            Strings.PmBrowsePlugins,
            BrowseSelected,
            PluginButtonKind.Primary);
        _refreshButton = PluginUi.Button(Strings.PmRefresh, () => RefreshSelected());
        _policyButton = PluginUi.Button(Strings.PmPolicyLabel, () => TogglePolicySelected());
        _removeButton = PluginUi.Button(Strings.PmRemove, RemoveSelected, PluginButtonKind.Danger);
        Button add = PluginUi.Button(
            Strings.PmAddMarketplace,
            () => PluginMarketplaceAddForm.Open(_ctx),
            PluginButtonKind.Primary);
        Button refreshAll = PluginUi.Button(Strings.PmRefreshAll, () => _ = RefreshAsync(null));
        _selectionActions = PluginUi.Actions(
            _pluginsButton,
            _refreshButton,
            _policyButton,
            _removeButton);
        _selectionActions.IsVisible = false;
        var footer = new StackPanel { Spacing = 0 };
        footer.Children.Add(_selectionActions);
        footer.Children.Add(PluginUi.Actions(add, refreshAll));
        Grid.SetRow(footer, 3);
        Children.Add(footer);

        _ctx.Owner.SetHints(Strings.PmMarketsHints);
        KeyDown += OnKeyDown;
        _ = RefreshAsync();
    }

    public bool UseWorkspaceScroll => false;

    /// <summary>Focuses the selected row once shown (pre-attach focus is a no-op).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _list.Focus();
    }

    public async Task RefreshAsync()
    {
        _shown.Clear();

        if (_ctx.Market is null)
        {
            _summary.Text = Strings.PmNoMarket;
            _summary.Foreground = PluginUi.Red;
            _list.ItemsSource = Array.Empty<TextBlock>();
            SetActionsEnabled(false);
            return;
        }

        IReadOnlyList<MarketplaceInfo> markets;
        try
        {
            markets = await _ctx.Market.MarketplacesAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
            return;
        }

        _shown.AddRange(markets);
        _summary.Text = markets.Count == 0 ? Strings.PmNoMarkets : Strings.PmMarketsHeader(markets.Count);
        _summary.Foreground = PluginUi.Gold;
        _list.ItemsSource = markets.Select(BuildTableRow).ToList();
        _selected = -1;
        _list.SelectedIndex = -1;
        Select(-1);
    }

    private static Control BuildTableHeader()
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(TableColumns),
            Background = PluginUi.Raised,
        };
        AddCell(row, Strings.PmFieldMarketName, 0, PluginUi.Gold);
        AddCell(row, Strings.PmFieldMarketSource, 1, PluginUi.Gold);
        AddCell(row, Strings.PmPluginsColumn, 2, PluginUi.Gold);
        AddCell(row, Strings.PmPolicyLabel, 3, PluginUi.Gold);
        AddCell(row, Strings.PmRefreshedColumn, 4, PluginUi.Gold);
        return row;
    }

    private static Control BuildTableRow(MarketplaceInfo market)
    {
        var item = new StackPanel { Spacing = 0 };
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions(TableColumns) };
        AddCell(row, market.Name, 0, PluginUi.White);
        AddCell(row, market.Source, 1, PluginUi.Soft);
        AddCell(row, market.PluginCount.ToString(System.Globalization.CultureInfo.InvariantCulture), 2, PluginUi.White);
        AddCell(row, market.AutoUpdate, 3, PluginUi.White);
        AddCell(
            row,
            market.Refreshed is { } when
                ? when.ToString("u", System.Globalization.CultureInfo.InvariantCulture)
                : Strings.PmNeverRefreshed,
            4,
            market.Error is { Length: > 0 } ? PluginUi.Red : PluginUi.Soft);
        item.Children.Add(row);
        if (market.Error is { Length: > 0 } error)
        {
            item.Children.Add(new TextBlock
            {
                Text = error,
                Foreground = PluginUi.Red,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(1, 0, 0, 0),
            });
        }
        return item;
    }

    private static void AddCell(Grid row, string text, int column, IBrush foreground)
    {
        var cell = new TextBlock
        {
            Text = text,
            Foreground = foreground,
            TextWrapping = TextWrapping.NoWrap,
            Margin = new Thickness(1, 0),
        };
        Grid.SetColumn(cell, column);
        row.Children.Add(cell);
    }

    private void Select(int selected)
    {
        _selected = selected;
        bool hasSelection = IsValidSelection(selected, _shown.Count);
        SetActionsEnabled(hasSelection);
        _selectionActions.IsVisible = hasSelection;
        _policyButton.Content = hasSelection
            ? $"{Strings.PmPolicyLabel}: {_shown[selected].AutoUpdate}"
            : Strings.PmPolicyLabel;
    }

    private void SetActionsEnabled(bool enabled)
    {
        _pluginsButton.IsEnabled = enabled;
        _refreshButton.IsEnabled = enabled;
        _policyButton.IsEnabled = enabled;
        _removeButton.IsEnabled = enabled;
    }

    private void BrowseSelected()
    {
        if (IsValidSelection(_selected, _shown.Count))
            _ctx.Show(new PluginCatalogPage(_ctx, _shown[_selected].Name));
    }

    private void RefreshSelected()
    {
        if (IsValidSelection(_selected, _shown.Count))
            _ = RefreshAsync(_shown[_selected].Name);
    }

    private void TogglePolicySelected()
    {
        if (IsValidSelection(_selected, _shown.Count))
        {
            MarketplaceInfo market = _shown[_selected];
            _ = CyclePolicyAsync(market.Name, market.AutoUpdate);
        }
    }

    private void RemoveSelected()
    {
        if (IsValidSelection(_selected, _shown.Count))
            ShowRemoveModal(_shown[_selected].Name);
    }

    private async Task RefreshAsync(string? name)
    {
        if (_ctx.Market is null)
            return;

        _ctx.Owner.SetStatus(Strings.PmWorking);
        try
        {
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.MarketplaceRefreshAsync(name, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginMarketplacesPage(_ctx));
    }

    private async Task CyclePolicyAsync(string name, string current)
    {
        if (_ctx.Market is null)
            return;

        string next = Policies[(Array.IndexOf(Policies, current.ToLowerInvariant()) + 1) % Policies.Length];
        try
        {
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.MarketplaceAutoUpdateAsync(name, next, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginMarketplacesPage(_ctx));
    }

    private void ShowRemoveModal(string name)
    {
        Border frame = PluginModal.Frame(
            _ctx,
            Strings.PmRemoveMarketTitleFor(name),
            out StackPanel content,
            showDismiss: false);
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmRemoveMarketBody,
            Foreground = PluginUi.White,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(PluginModal.ButtonRow(
            PluginModal.ActionButton(_ctx, Strings.PmRemove, () => _ = RemoveAsync(name), danger: true, primary: true),
            PluginModal.ActionButton(_ctx, Strings.TuiPromptCancel, () => { })));
        _ctx.Owner.ShowModal(frame);
    }

    private async Task RemoveAsync(string name)
    {
        if (_ctx.Market is null)
            return;

        try
        {
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.MarketplaceRemoveAsync(name, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginMarketplacesPage(_ctx));
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_shown.Count == 0)
            return;

        switch (e.Key)
        {
            case Key.Enter when IsValidSelection(_selected, _shown.Count):
                BrowseSelected();
                e.Handled = true;
                break;
            case Key.R when IsValidSelection(_selected, _shown.Count):
                RefreshSelected();
                e.Handled = true;
                break;
            case Key.P when IsValidSelection(_selected, _shown.Count):
                TogglePolicySelected();
                e.Handled = true;
                break;
            case Key.Delete when IsValidSelection(_selected, _shown.Count):
                RemoveSelected();
                e.Handled = true;
                break;
        }
    }

    internal static bool IsValidSelection(int selected, int count)
        => selected >= 0 && selected < count;
}

/// <summary>The add-marketplace form as a centered modal (source + optional name).</summary>
internal static class PluginMarketplaceAddForm
{
    internal static void Open(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var source = new TextBox();
        var name = new TextBox();

        Border frame = PluginModal.Frame(
            ctx,
            Strings.PmAddMarketTitle,
            out StackPanel content,
            dismissLabel: Strings.PmClose);
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldMarketSource, source));
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldMarketName, name));
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmAddMarketHint,
            Foreground = PluginUi.Muted,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(PluginModal.ButtonRow(
            PluginModal.ActionButton(ctx, Strings.PmAddMarketplace,
                () => _ = AddAsync(ctx, source.Text ?? string.Empty, name.Text ?? string.Empty),
                primary: true,
                closeModal: false)));
        PluginModal.EscToHide(ctx, source, name);
        ctx.Owner.ShowModal(frame);
        source.Focus();
    }

    private static async Task AddAsync(PluginManagerContext ctx, string source, string name)
    {
        if (ctx.Market is null)
        {
            ctx.Owner.SetStatus(Strings.PmNoMarket);
            return;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            ctx.Owner.SetStatus(Strings.PmAddMarketNeedsSource);
            return;
        }

        // "as <name>" is a command-grammar concern; the API takes both parts directly.
        string? named = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        ctx.Owner.HideModal();
        ctx.Owner.SetStatus(Strings.PmWorking);
        try
        {
            PluginActionResult result = await ctx.RunMarketAsync(
                (market, ct) => market.MarketplaceAddAsync(source.Trim(), named, ct)).ConfigureAwait(true);
            ctx.Owner.SetStatus(result.Message);
            ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ctx.Owner.SetStatus(ex.Message);
            return;
        }

        ctx.Owner.ReplaceTop(new PluginMarketplacesPage(ctx));
    }
}
