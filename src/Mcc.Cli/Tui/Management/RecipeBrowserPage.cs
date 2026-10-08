using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Client;
using Umpk.Game.Inventory;

namespace Mcc.Cli.Tui.Management;

internal sealed class RecipeBrowserPage : DockPanel, IManagementWorkspacePage
{
    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly TextBox _search;
    private readonly ListBox _list;
    private readonly StackPanel _detail;
    private readonly ScrollViewer _detailScroll;
    private readonly ContentControl _body;
    private readonly Button _craftOne;
    private readonly Button _craftAll;
    private readonly Control _craftActions;
    private RecipeFilter _filter;
    private RecipeBookSnapshot? _book;
    private RecipePlacementSupport? _support;
    private bool _menuOpen;
    private bool _compact;
    private bool _showingDetail;

    public RecipeBrowserPage(ManagementWorkspace workspace, Client client)
    {
        _workspace = workspace;
        _client = client;

        _search = ManagementUi.SearchBox();
        _search.TextChanged += (_, _) => Filter();
        Control filters = ManagementUi.Actions(
            ManagementUi.Button(Strings.RecipeUiAll, () => SetFilter(RecipeFilter.All)),
            ManagementUi.Button(Strings.RecipeUiNamed, () => SetFilter(RecipeFilter.Named)),
            ManagementUi.Button(Strings.RecipeUiNumeric, () => SetFilter(RecipeFilter.Numeric)));
        Grid toolbar = ManagementUi.FilterSearchToolbar(
            filters,
            ManagementUi.Button(Strings.MgmtRefresh, Refresh),
            _search);
        SetDock(toolbar, Dock.Top);
        Children.Add(toolbar);

        _list = new ListBox
        {
            MinWidth = 32,
            MinHeight = 18,
            Background = ManagementUi.Canvas,
            Foreground = Brushes.White,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
        };
        _list.SelectionChanged += (_, _) => RenderSelected(open: false);
        ScrollViewer.SetVerticalScrollBarVisibility(_list, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        _list.DoubleTapped += (_, _) => RenderSelected(open: true);
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                RenderSelected(open: true);
                e.Handled = true;
            }
        };

        _detail = new StackPanel { Spacing = 0, MinWidth = 40, Margin = new Thickness(1, 0, 0, 0) };
        _detailScroll = new ScrollViewer
        {
            Content = _detail,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        _craftOne = ManagementUi.Button(Strings.RecipeUiCraftOne, () => _ = CraftAsync(false), ManagementButtonKind.Primary);
        _craftAll = ManagementUi.Button(Strings.RecipeUiCraftAll, () => _ = CraftAsync(true));
        _craftActions = ManagementUi.Actions(_craftOne, _craftAll);
        _body = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        Children.Add(_body);
        Refresh();
    }

    public bool UseWorkspaceScroll => false;

    public bool HasLocalBackNavigation => _compact && _showingDetail;

    public void FocusSearch() => _search.Focus();

    public void Refresh() => _ = RefreshAsync();

    public bool HandleBack()
    {
        if (!_compact || !_showingDetail)
            return false;

        _showingDetail = false;
        BuildLayout();
        _list.Focus();
        return true;
    }

    public void SetCompact(bool compact)
    {
        _compact = compact;
        if (!compact)
            _showingDetail = false;
        BuildLayout();
    }

    private async Task RefreshAsync()
    {
        string? selected = (_list.SelectedItem as RecipeRow)?.Key;
        try
        {
            _book = await _client.Game.Inventory.GetRecipeBookAsync(_workspace.Closed);
            _support = await _client.Game.Inventory.GetRecipePlacementAsync(_workspace.Closed);
            _menuOpen = await _client.Game.Inventory.GetOpenContainerAsync(_workspace.Closed) is not null;
            Filter(selected);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private void SetFilter(RecipeFilter filter)
    {
        _filter = filter;
        _showingDetail = false;
        Filter();
    }

    private void Filter(string? preserve = null)
    {
        if (_book is null)
            return;

        string query = (_search.Text ?? string.Empty).Trim();
        var rows = new List<RecipeRow>();
        if (_filter is RecipeFilter.All or RecipeFilter.Named)
        {
            rows.AddRange(_book.Recipes.Select(id => new RecipeRow(
                "n:" + id,
                InventoryRendering.TypeName(_client.Translations, id),
                id,
                Numeric: false)));
        }
        if (_filter is RecipeFilter.All or RecipeFilter.Numeric)
        {
            rows.AddRange(_book.RecipeIds.Select(id => new RecipeRow(
                "i:" + id,
                "#" + id,
                id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Numeric: true)));
        }
        rows = rows
            .Where(row => query.Length == 0
                || row.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Recipe.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        _list.ItemsSource = rows;
        _list.SelectedItem = rows.FirstOrDefault(row => row.Key == preserve) ?? rows.FirstOrDefault();
        string status = rows.Count == 0 ? Strings.RecipeUiNoRecipes : Strings.MgmtShowing(rows.Count, _book.Recipes.Count + _book.RecipeIds.Count);
        if (_book.OpaqueAdditions > 0)
            status += " " + Strings.RecipeUiIncomplete;
        _workspace.SetStatus(status);
        RenderSelected(open: false);
        BuildLayout();
    }

    private void RenderSelected(bool open)
    {
        _detail.Children.Clear();
        if (_list.SelectedItem is not RecipeRow row)
        {
            _detail.Children.Add(ManagementUi.Body(Strings.RecipeUiNoRecipes, ManagementUi.Muted));
            return;
        }

        _detail.Children.Add(ManagementUi.Section(row.Label));
        _detail.Children.Add(ManagementUi.Body(row.Numeric ? Strings.RecipeUiNumericKind : Strings.RecipeUiNamedKind, ManagementUi.Soft));
        _detail.Children.Add(ManagementUi.Body(row.Recipe));
        string? disabled = DisabledReason(row);
        _craftOne.IsEnabled = disabled is null;
        _craftAll.IsEnabled = disabled is null;
        _detail.Children.Add(_craftActions);
        if (disabled is not null)
            _detail.Children.Add(ManagementUi.Body(disabled, ManagementUi.Gold));
        if (open && _compact)
        {
            _showingDetail = true;
            BuildLayout();
        }
    }

    private string? DisabledReason(RecipeRow row)
    {
        return ActionAvailability(_support, row.Numeric, _menuOpen) switch
        {
            RecipeActionAvailability.Unsupported => Strings.RecipeUiUnsupported,
            RecipeActionAvailability.WrongForm => Strings.RecipeUiWrongForm,
            RecipeActionAvailability.MissingMenu => Strings.RecipeUiNoMenu,
            _ => null,
        };
    }

    internal static RecipeActionAvailability ActionAvailability(
        RecipePlacementSupport? support, bool numeric, bool menuOpen)
    {
        if (support is null || support.Form == RecipePlacementForm.None)
            return RecipeActionAvailability.Unsupported;
        if ((numeric && support.Form != RecipePlacementForm.NetworkId)
            || (!numeric && support.Form != RecipePlacementForm.ResourceName))
            return RecipeActionAvailability.WrongForm;
        return menuOpen ? RecipeActionAvailability.Enabled : RecipeActionAvailability.MissingMenu;
    }

    private async Task CraftAsync(bool makeAll)
    {
        if (_list.SelectedItem is not RecipeRow row || DisabledReason(row) is not null)
            return;
        try
        {
            RecipePlacementOutcome outcome = await _client.Game.Inventory.PlaceRecipeAsync(
                row.Recipe, makeAll, _workspace.Closed);
            _workspace.SetStatus(outcome.Kind switch
            {
                RecipePlacementOutcomeKind.Success => Strings.RecipeUiPlaced,
                RecipePlacementOutcomeKind.MissingMenu => Strings.RecipeUiNoMenu,
                RecipePlacementOutcomeKind.UnsupportedRecipeForm => Strings.RecipeUiWrongForm,
                _ => Strings.RecipeUiRejected,
            });
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private void BuildLayout()
    {
        ManagementUi.SetResponsiveBody(
            _body, _compact, _showingDetail, _list, _detailScroll, "34,*");
        _workspace.RefreshNavigation();
    }

    private enum RecipeFilter { All, Named, Numeric }

    internal enum RecipeActionAvailability { Enabled, Unsupported, WrongForm, MissingMenu }

    private sealed record RecipeRow(string Key, string Label, string Recipe, bool Numeric)
    {
        public override string ToString() => Label;
    }
}
