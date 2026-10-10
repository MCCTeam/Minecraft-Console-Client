using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;

namespace Mcc.Cli.Tui.Management;

internal sealed class AdvancementBrowserPage : DockPanel, IManagementWorkspacePage
{
    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly TextBox _search;
    private readonly ComboBox _scope;
    private readonly ListBox _list;
    private readonly StackPanel _detail;
    private readonly ScrollViewer _detailScroll;
    private readonly ContentControl _body;
    private AdvancementsSnapshot? _snapshot;
    private CompletionFilter _filter;
    private bool _compact;
    private bool _showingDetail;

    public AdvancementBrowserPage(ManagementWorkspace workspace, Client client)
    {
        _workspace = workspace;
        _client = client;
        _search = ManagementUi.SearchBox();
        _search.TextChanged += (_, _) => Filter();
        _scope = new ComboBox
        {
            MinWidth = 20,
            Height = 3,
            MinHeight = 3,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        _scope.SelectionChanged += (_, _) => Filter();
        Control filters = ManagementUi.Actions(
            ManagementUi.Button(Strings.AdvancementUiAll, () => SetFilter(CompletionFilter.All)),
            ManagementUi.Button(Strings.AdvancementUiUnlocked, () => SetFilter(CompletionFilter.Unlocked)),
            ManagementUi.Button(Strings.AdvancementUiLocked, () => SetFilter(CompletionFilter.Locked)));
        Grid toolbar = ManagementUi.FilterSearchToolbar(
            filters,
            ManagementUi.Button(Strings.MgmtRefresh, Refresh),
            _search,
            _scope);
        SetDock(toolbar, Dock.Top);
        Children.Add(toolbar);

        _list = new ListBox
        {
            Background = ManagementUi.Canvas,
            Foreground = Brushes.White,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
            ItemTemplate = new FuncDataTemplate<AdvancementRow>((row, _) =>
                ManagementUi.VirtualizedItemText(
                    row,
                    static value => value.DisplayText,
                    TextWrapping.NoWrap)),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_list, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        _list.SelectionChanged += (_, _) => RenderSelected(open: false);
        _list.DoubleTapped += (_, _) => RenderSelected(open: true);
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                RenderSelected(open: true);
                e.Handled = true;
            }
        };
        _detail = new StackPanel
        {
            Spacing = 0,
            Margin = new Thickness(1, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _detailScroll = new ScrollViewer
        {
            Content = _detail,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
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
        string? selected = (_list.SelectedItem as AdvancementRow)?.Advancement.Id;
        try
        {
            _snapshot = await _client.Game.Player.GetAdvancementsAsync(_workspace.Closed);
            string? selectedScope = (_scope.SelectedItem as ScopeItem)?.Id;
            List<ScopeItem> scopes = [new(null, Strings.AdvancementUiEveryScope)];
            scopes.AddRange(_snapshot.Entries
                .Select(entry => AdvancementPresentation.Scope(entry.Id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Select(value => new ScopeItem(value, value)));
            _scope.ItemsSource = scopes;
            _scope.SelectedItem = scopes.FirstOrDefault(item => item.Id == selectedScope) ?? scopes[0];
            Filter(selected);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private void SetFilter(CompletionFilter filter)
    {
        _filter = filter;
        _showingDetail = false;
        Filter();
    }

    private void Filter(string? preserve = null)
    {
        if (_snapshot is null)
            return;
        if (!_snapshot.StructuredDataAvailable && _snapshot.Entries.Count == 0)
        {
            _list.ItemsSource = Array.Empty<AdvancementRow>();
            _detail.Children.Clear();
            _detail.Children.Add(ManagementUi.Body(Strings.AdvancementUiUnavailable, ManagementUi.Gold));
            _workspace.SetStatus(Strings.AdvancementUiUnavailable);
            BuildLayout();
            return;
        }

        string query = (_search.Text ?? string.Empty).Trim();
        string? scope = (_scope.SelectedItem as ScopeItem)?.Id;
        Dictionary<string, AdvancementInfo> byId = _snapshot.Entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
        List<AdvancementRow> rows = _snapshot.Entries
            .Select(entry => new AdvancementRow(
                entry,
                AdvancementPresentation.Title(_client.Translations, entry),
                AdvancementPresentation.Depth(entry, byId),
                AdvancementPresentation.IsComplete(entry),
                AdvancementPresentation.IsComplete(entry)
                    ? _client.Commands.Glyphs.AdvancementDone
                    : _client.Commands.Glyphs.AdvancementTodo,
                AdvancementPresentation.HierarchyKey(entry, byId)))
            .Where(row => (_filter == CompletionFilter.All
                    || (_filter == CompletionFilter.Unlocked) == row.Complete)
                && (scope is null || string.Equals(AdvancementPresentation.Scope(row.Advancement.Id), scope, StringComparison.OrdinalIgnoreCase))
                && (query.Length == 0
                    || row.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || row.Advancement.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.Advancement.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)))
            .OrderBy(row => AdvancementPresentation.Scope(row.Advancement.Id), StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.HierarchyKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _list.ItemsSource = rows;
        _list.SelectedItem = rows.FirstOrDefault(row => row.Advancement.Id == preserve) ?? rows.FirstOrDefault();
        int total = _snapshot.Entries.Count;
        int completed = _snapshot.Entries.Count(AdvancementPresentation.IsComplete);
        _workspace.SetStatus(rows.Count == 0
            ? Strings.AdvancementUiNoMatches
            : Strings.AdvancementUiProgress(completed, total) + " · " + Strings.MgmtShowing(rows.Count, total));
        RenderSelected(open: false);
        BuildLayout();
    }

    private void RenderSelected(bool open)
    {
        _detail.Children.Clear();
        _detailScroll.Offset = default;
        if (_list.SelectedItem is not AdvancementRow row)
        {
            _detail.Children.Add(ManagementUi.Body(Strings.AdvancementUiNoMatches, ManagementUi.Muted));
            return;
        }
        AdvancementInfo item = row.Advancement;
        _detail.Children.Add(ManagementUi.Section(row.Title));
        _detail.Children.Add(ManagementUi.Body(
            row.Complete ? Strings.AdvancementUiComplete : Strings.AdvancementUiIncomplete,
            row.Complete ? ManagementUi.Green : ManagementUi.Gold));
        AddLine(Strings.AdvancementUiDescription, item.Description ?? Strings.BrowserNone);
        AddLine(Strings.AdvancementUiFullId, item.Id);
        AddLine(Strings.AdvancementUiFrame, AdvancementPresentation.FrameName(item.Frame));
        AddLine(Strings.AdvancementUiCriteria, Strings.AdvancementUiCriteriaProgress(item.CriteriaCompleted, item.CriteriaCount));
        if (open && _compact)
        {
            _showingDetail = true;
            BuildLayout();
        }
    }

    private void AddLine(string label, string value)
        => _detail.Children.Add(ManagementUi.Body(label + ": " + (value.Length == 0 ? Strings.BrowserNone : value)));

    private void BuildLayout()
    {
        ManagementUi.SetResponsiveBody(
            _body, _compact, _showingDetail, _list, _detailScroll, "40,*");
        _workspace.RefreshNavigation();
    }

    private enum CompletionFilter { All, Unlocked, Locked }

    private sealed record ScopeItem(string? Id, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed record AdvancementRow(
        AdvancementInfo Advancement,
        string Title,
        int Depth,
        bool Complete,
        string Marker,
        string HierarchyKey)
    {
        public string DisplayText => new string(' ', Depth * 2) + Marker + " " + Title;

        public override string ToString()
            => DisplayText;
    }
}
