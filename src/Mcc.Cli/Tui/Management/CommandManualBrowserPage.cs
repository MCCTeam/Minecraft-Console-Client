using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Presentation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Commands;
using DMCBK.Core.Manual;

namespace Mcc.Cli.Tui.Management;

internal sealed class CommandManualBrowserPage : DockPanel, IManagementWorkspacePage
{
    private readonly ManagementWorkspace _workspace;
    private readonly MainTuiView _view;
    private readonly Client _client;
    private readonly MarkdownTuiRenderer _renderer;
    private readonly TextBox _search;
    private readonly Button _commandsTab;
    private readonly Button _manualTab;
    private readonly ListBox _list;
    private readonly StackPanel _detail;
    private readonly ScrollViewer _detailScroll;
    private readonly ContentControl _body;
    private IReadOnlyList<CommandDescriptor> _commands = [];
    private IReadOnlyList<ManualEntry> _manual = [];
    private CommandBrowserTab _tab;
    private bool _compact;
    private bool _showingDetail;

    public CommandManualBrowserPage(
        ManagementWorkspace workspace,
        MainTuiView view,
        Client client,
        MarkdownTuiRenderer renderer,
        CommandBrowserTab initialTab)
    {
        _workspace = workspace;
        _view = view;
        _client = client;
        _renderer = renderer;
        _tab = initialTab;

        _commandsTab = ManagementUi.Button(Strings.BrowserCommandsTab, () => SetTab(CommandBrowserTab.Commands));
        _manualTab = ManagementUi.Button(Strings.BrowserManualTab, () => SetTab(CommandBrowserTab.Manual));
        _search = ManagementUi.SearchBox();
        _search.TextChanged += (_, _) => ApplyFilter();
        Control tabs = ManagementUi.Actions(_commandsTab, _manualTab);
        Grid toolbar = ManagementUi.FilterSearchToolbar(
            tabs,
            ManagementUi.Button(Strings.MgmtRefresh, Refresh),
            _search);
        SetDock(toolbar, Dock.Top);
        Children.Add(toolbar);

        _list = new ListBox
        {
            Background = ManagementUi.Canvas,
            Foreground = Brushes.White,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
            ItemTemplate = new FuncDataTemplate<BrowserItem>((item, _) =>
                ManagementUi.VirtualizedItemText(item, static value => value.Label)),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_list, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        _list.SelectionChanged += (_, _) => SelectCurrent(open: false);
        _list.DoubleTapped += (_, _) => SelectCurrent(open: true);
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrent(open: true);
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

    public void Refresh()
    {
        string? selected = (_list.SelectedItem as BrowserItem)?.Key;
        _commands = _client.Commands.DescribeCommands()
            .Where(command => command.ShowInIndex)
            .OrderBy(command => command.Category)
            .ThenBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _manual = _client.Manuals.Topics()
            .Select(topic => new ManualEntry(topic, _client.Manuals.Read(topic.Id, _client.UiCulture) ?? string.Empty))
            .OrderBy(entry => entry.Topic.Group)
            .ThenBy(entry => entry.Topic.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        ApplyFilter(selected);
    }

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
        if (_compact == compact)
            return;

        _compact = compact;
        if (!compact)
            _showingDetail = false;
        BuildLayout();
    }

    private void SetTab(CommandBrowserTab tab)
    {
        _tab = tab;
        _showingDetail = false;
        ApplyFilter();
    }

    private void ApplyFilter(string? preserveKey = null)
    {
        string query = (_search.Text ?? string.Empty).Trim();
        List<BrowserItem> items = _tab == CommandBrowserTab.Commands
            ? _commands.Where(command => Matches(command, query))
                .Select(command => new BrowserItem(
                    command.Name,
                    $"{command.Category.ToString().ToUpperInvariant(),-11} {command.Name}",
                    command))
                .ToList()
            : _manual.Where(entry => Matches(entry, query))
                .Select(entry => new BrowserItem(
                    entry.Topic.Id,
                    $"{entry.Topic.Group.ToString().ToUpperInvariant(),-11} {entry.Topic.Id}",
                    entry))
                .ToList();

        _list.ItemsSource = items;
        _commandsTab.IsEnabled = _tab != CommandBrowserTab.Commands;
        _manualTab.IsEnabled = _tab != CommandBrowserTab.Manual;
        BrowserItem? restored = items.FirstOrDefault(item => string.Equals(item.Key, preserveKey, StringComparison.OrdinalIgnoreCase));
        _list.SelectedItem = restored ?? items.FirstOrDefault();
        _workspace.SetStatus(items.Count == 0
            ? Strings.MgmtNoMatches
            : Strings.MgmtShowing(items.Count, _tab == CommandBrowserTab.Commands ? _commands.Count : _manual.Count));
        SelectCurrent(open: false);
        BuildLayout();
    }

    private void SelectCurrent(bool open)
    {
        _detail.Children.Clear();
        _detailScroll.Offset = default;
        if (_list.SelectedItem is not BrowserItem item)
        {
            _detail.Children.Add(ManagementUi.Body(Strings.MgmtNoMatches, ManagementUi.Muted));
            return;
        }

        if (item.Value is CommandDescriptor command)
            RenderCommand(command);
        else if (item.Value is ManualEntry manual)
            RenderManual(manual);

        if (open && _compact)
        {
            _showingDetail = true;
            BuildLayout();
        }
    }

    private void RenderCommand(CommandDescriptor command)
    {
        _detail.Children.Add(ManagementUi.Section(command.Name));
        _detail.Children.Add(ManagementUi.Body(command.Description, ManagementUi.Soft));
        _detail.Children.Add(ManagementUi.Actions(ManagementUi.Button(
            Strings.BrowserInsertCommand,
            () => _view.InsertCommandText(_client.Commands.ActivePrefix + command.Name + " "),
            ManagementButtonKind.Primary)));

        AddSection(Strings.BrowserUsage);
        if (command.UsageLines.Length == 0)
            AddLine(_client.Commands.ActivePrefix + command.Usage);
        else
        {
            foreach (UsageLine usage in command.UsageLines)
            {
                string syntax = _client.Commands.ActivePrefix + command.Name
                    + (usage.Syntax.Length == 0 ? string.Empty : " " + usage.Syntax);
                AddLine(usage.Description is null ? syntax : syntax + "  " + usage.Description);
            }
        }

        if (command.Flags.Length > 0)
        {
            AddSection(Strings.BrowserFlags);
            foreach (UsageFlag flag in command.Flags)
                AddLine(flag.Name + "  " + flag.Description);
        }

        if (command.Examples.Length > 0)
        {
            AddSection(Strings.BrowserExamples);
            foreach (string example in command.Examples)
            {
                string text = _client.Commands.ActivePrefix + example;
                _detail.Children.Add(WrappingButton(
                    text,
                    () => _view.InsertCommandText(text),
                    ManagementButtonKind.Quiet));
            }
        }

        AddSection(Strings.BrowserRequirements);
        AddLine(command.Requirements == CommandFeature.None ? Strings.BrowserNone : command.Requirements.ToString());
        if (command.RelatedCommands.Length > 0)
        {
            AddSection(Strings.BrowserRelated);
            AddLine(string.Join(", ", command.RelatedCommands));
        }

        if (command.ManualTopic is { Length: > 0 } topic)
        {
            AddSection(Strings.BrowserManualTopic);
            _detail.Children.Add(ManagementUi.Button(topic, () => OpenManual(topic), ManagementButtonKind.Secondary));
        }
    }

    private void RenderManual(ManualEntry entry)
    {
        _detail.Children.Add(ManagementUi.Section(entry.Topic.Id));
        _detail.Children.Add(ManagementUi.Body(entry.Topic.Summary, ManagementUi.Soft));
        foreach (Control control in _renderer.Render(entry.Markdown, wrapPreformatted: true))
            _detail.Children.Add(control);
    }

    private void OpenManual(string topicId)
    {
        _tab = CommandBrowserTab.Manual;
        _search.Text = topicId;
        ApplyFilter(topicId);
        if (_compact)
        {
            _showingDetail = true;
            BuildLayout();
        }
    }

    private void AddSection(string text) => _detail.Children.Add(ManagementUi.Section(text));

    private void AddLine(string text) => _detail.Children.Add(ManagementUi.Body(text));

    private static Button WrappingButton(string text, Action action, ManagementButtonKind kind)
    {
        Button button = ManagementUi.Button(text, action, kind);
        button.Content = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
        };
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        return button;
    }

    private void BuildLayout()
    {
        ManagementUi.SetResponsiveBody(
            _body, _compact, _showingDetail, _list, _detailScroll, "34,*");
        _workspace.RefreshNavigation();
    }

    internal static bool Matches(CommandDescriptor command, string query)
    {
        if (query.Length == 0)
            return true;

        return new[]
            {
                command.Name,
                command.Description,
                command.Usage,
                string.Join(' ', command.Aliases),
                string.Join(' ', command.UsageLines.Select(line => line.Syntax + " " + line.Description)),
                string.Join(' ', command.Flags.Select(flag => flag.Name + " " + flag.Description)),
                string.Join(' ', command.Examples),
            }
            .Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static bool Matches(ManualEntry entry, string query)
        => query.Length == 0
            || entry.Topic.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.Topic.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.Markdown.Contains(query, StringComparison.OrdinalIgnoreCase);

    private sealed record BrowserItem(string Key, string Label, object Value)
    {
        public override string ToString() => Label;
    }

    private sealed record ManualEntry(ManualTopic Topic, string Markdown);
}
