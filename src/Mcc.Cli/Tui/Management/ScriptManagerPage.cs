using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using DMCBK.Core;
using DMCBK.Core.Beacon;

namespace Mcc.Cli.Tui.Management;

internal sealed class ScriptManagerPage : Grid, IManagementWorkspacePage
{
    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly TextBox _search;
    private readonly ListBox _list;
    private readonly Button _runStop;
    private readonly Button _reload;
    private readonly Button _edit;
    private readonly Button _lint;
    private readonly Button _format;
    private readonly Button _settings;
    private readonly Button _delete;
    private readonly Button _watch;
    private readonly Button _mute;
    private readonly Grid _toolbar;
    private readonly Control _toolbarButtons;
    private IReadOnlyList<ScriptInfo> _scripts = [];
    private bool _busy;

    public ScriptManagerPage(ManagementWorkspace workspace, Client client)
    {
        _workspace = workspace;
        _client = client;
        RowDefinitions = new RowDefinitions("Auto,*,Auto");

        _search = ManagementUi.SearchBox();
        _search.MinWidth = 24;
        _search.MaxWidth = 110;
        _search.TextChanged += (_, _) => Filter();
        _watch = ManagementUi.Button(string.Empty, () => ToggleWatch());
        _mute = ManagementUi.Button(string.Empty, () => ToggleMute());
        _toolbarButtons = ManagementUi.Actions(
            ManagementUi.Button(Strings.ScriptsUiNew, ShowNew, ManagementButtonKind.Primary),
            _watch,
            _mute,
            ManagementUi.Button(Strings.MgmtRefresh, Refresh));
        _toolbar = BuildToolbar(_toolbarButtons, _search, compact: false);
        Children.Add(_toolbar);

        _list = new ListBox
        {
            MinHeight = 6,
            Background = ManagementUi.Canvas,
            Foreground = ManagementUi.Soft,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
        };
        Grid.SetRow(_list, 1);
        _list.SelectionChanged += (_, _) => UpdateActions();
        ScrollViewer.SetVerticalScrollBarVisibility(_list, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        _list.DoubleTapped += (_, _) => OpenEditor();
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                OpenEditor();
                e.Handled = true;
            }
        };
        Children.Add(_list);

        _runStop = ManagementUi.Button(Strings.ScriptsUiRun, () => _ = RunOrStopAsync(), ManagementButtonKind.Primary);
        _reload = ManagementUi.Button(Strings.ScriptsUiReload, () => _ = ReloadAsync());
        _edit = ManagementUi.Button(Strings.ScriptsUiEdit, OpenEditor);
        _lint = ManagementUi.Button(Strings.ScriptsUiLint, () => _ = LintAsync());
        _format = ManagementUi.Button(Strings.ScriptsUiFormat, OpenFormatter);
        _settings = ManagementUi.Button(Strings.ScriptsUiSettings, OpenSettings);
        _delete = ManagementUi.Button(
            Strings.ScriptsUiDelete,
            ConfirmDelete,
            ManagementButtonKind.Danger);
        Control actions = ManagementUi.Actions(
            _runStop,
            _reload,
            _edit,
            _lint,
            _format,
            _settings,
            ManagementUi.Button(Strings.ScriptsUiRepl, OpenRepl),
            _delete);
        Grid.SetRow(actions, 2);
        Children.Add(actions);
        Refresh();
    }

    public bool UseWorkspaceScroll => false;

    internal static Grid BuildToolbar(Control buttons, TextBox search, bool compact = false)
    {
        var toolbar = new Grid();
        toolbar.Children.Add(buttons);
        toolbar.Children.Add(search);
        ApplyToolbarLayout(toolbar, buttons, search, compact);
        return toolbar;
    }

    internal static void ApplyToolbarLayout(Grid toolbar, Control buttons, TextBox search, bool compact)
    {
        if (compact)
        {
            toolbar.ColumnDefinitions = new ColumnDefinitions("*");
            toolbar.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetColumn(buttons, 0);
            Grid.SetRow(buttons, 0);
            Grid.SetColumn(search, 0);
            Grid.SetRow(search, 1);
            search.HorizontalAlignment = HorizontalAlignment.Stretch;
            return;
        }

        toolbar.ColumnDefinitions = new ColumnDefinitions("Auto,*,8*");
        toolbar.ColumnDefinitions[2].MinWidth = 24;
        toolbar.ColumnDefinitions[2].MaxWidth = 110;
        toolbar.RowDefinitions = new RowDefinitions("Auto");
        Grid.SetColumn(buttons, 0);
        Grid.SetRow(buttons, 0);
        Grid.SetColumn(search, 2);
        Grid.SetRow(search, 0);
        search.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    public void SetCompact(bool compact)
        => ApplyToolbarLayout(_toolbar, _toolbarButtons, _search, compact);

    public void FocusSearch() => _search.Focus();

    public void Refresh()
    {
        string? selected = Selected?.Id;
        _scripts = _client.Scripts.Discover();
        Filter(selected);
        UpdateToggles();
    }

    private ScriptInfo? Selected => (_list.SelectedItem as ScriptRow)?.Script;

    private void Filter(string? preserve = null)
    {
        string query = (_search.Text ?? string.Empty).Trim();
        List<ScriptRow> rows = _scripts
            .Where(script => query.Length == 0 || script.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(script => script.Running)
            .ThenBy(script => script.Id, StringComparer.Ordinal)
            .Select(script => new ScriptRow(script))
            .ToList();
        _list.ItemsSource = rows;
        _list.SelectedItem = rows.FirstOrDefault(row => string.Equals(row.Script.Id, preserve, StringComparison.Ordinal))
            ?? rows.FirstOrDefault();
        int running = _scripts.Count(script => script.Running);
        _workspace.SetStatus(_scripts.Count == 0
            ? Strings.ScriptsUiNoScripts
            : Strings.ScriptsUiCount(running, _scripts.Count - running));
        UpdateActions();
    }

    private void UpdateActions()
    {
        ScriptInfo? script = Selected;
        bool enabled = script is not null && !_busy;
        _runStop.IsEnabled = enabled;
        _reload.IsEnabled = enabled && script!.Running;
        _edit.IsEnabled = enabled;
        _lint.IsEnabled = enabled;
        _format.IsEnabled = enabled;
        _settings.IsEnabled = enabled;
        _delete.IsEnabled = enabled;
        _runStop.Content = script is { Running: true } ? Strings.ScriptsUiStop : Strings.ScriptsUiRun;
    }

    private void UpdateToggles()
    {
        _watch.Content = Strings.ScriptsUiWatch + ": " + (_client.Scripts.WatchEnabled ? Strings.MgmtOn : Strings.MgmtOff);
        _mute.Content = Strings.ScriptsUiMute + ": " + (_client.Scripts.IsMuted ? Strings.MgmtOn : Strings.MgmtOff);
    }

    private void ToggleWatch()
    {
        _client.Scripts.SetWatch(!_client.Scripts.WatchEnabled);
        UpdateToggles();
    }

    private void ToggleMute()
    {
        _client.Scripts.SetMuted(!_client.Scripts.IsMuted);
        UpdateToggles();
    }

    private async Task RunOrStopAsync()
    {
        if (Selected is not { } script || _busy)
            return;

        SetBusy(true);
        try
        {
            if (script.Running)
                _client.Scripts.Stop(script.Id);
            else
            {
                BeaconRunResult result = await _client.Scripts.RunFileAsync(script.Id, _workspace.Closed);
                if (!result.Success)
                    _workspace.SetStatus(FirstError(result));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
        finally
        {
            SetBusy(false);
            Refresh();
        }
    }

    private async Task ReloadAsync()
    {
        if (Selected is not { } script || _busy)
            return;

        SetBusy(true);
        try
        {
            BeaconRunResult result = await _client.Scripts.ReloadAsync(script.Id, _workspace.Closed);
            _workspace.SetStatus(result.Success ? Strings.ScriptsUiReload : FirstError(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
        finally
        {
            SetBusy(false);
            Refresh();
        }
    }

    private async Task LintAsync()
    {
        if (Selected is not { } script)
            return;

        try
        {
            ScriptDocument document = await _client.Scripts.LoadDocumentAsync(script.Id, _workspace.Closed);
            BeaconLintReport report = _client.Scripts.Lint(Path.GetFileName(document.Path), document.Source);
            _workspace.SetStatus(report.Diagnostics.Count == 0
                ? Strings.ScriptsUiNoDiagnostics
                : string.Join(" · ", report.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private void OpenEditor()
    {
        if (Selected is { } script)
            _workspace.Push(new ScriptEditorPage(_workspace, _client, script.Id));
    }

    private void OpenFormatter()
    {
        if (Selected is { } script)
        {
            var editor = new ScriptEditorPage(_workspace, _client, script.Id);
            _workspace.Push(editor);
            editor.PreviewFormat();
        }
    }

    private void OpenSettings()
    {
        if (Selected is { } script)
            _workspace.Push(new ScriptSettingsPage(_workspace, _client, script.Id));
    }

    private void OpenRepl() => _workspace.Push(new ScriptReplPage(_workspace, _client));

    private void ConfirmDelete()
    {
        if (Selected is not { } script || _busy)
            return;

        string id = script.Id;
        var content = new StackPanel { Spacing = 1, MaxWidth = 56 };
        content.Children.Add(ManagementUi.Section(Strings.ScriptsUiDeleteConfirm(id)));
        content.Children.Add(ManagementUi.Body(
            Strings.ScriptsUiDeleteWarning(id, script.Running),
            ManagementUi.Soft));

        Button cancel = ManagementUi.Button(
            Strings.ScriptsUiCancel,
            _workspace.HideModal,
            ManagementButtonKind.Quiet);
        content.Children.Add(ManagementUi.Actions(
            ManagementUi.Button(
                Strings.ScriptsUiDelete,
                () => DeleteScript(id),
                ManagementButtonKind.Danger),
            cancel));

        _workspace.ShowModal(ManagementUi.Card(content, danger: true, compact: true));
        cancel.Focus();
    }

    private void DeleteScript(string id)
    {
        if (_busy)
            return;

        _workspace.HideModal();
        SetBusy(true);
        string status;
        try
        {
            ScriptDeleteResult result = _client.Scripts.Delete(id);
            status = result.Outcome == ScriptDeleteOutcome.Deleted
                ? Strings.ScriptsUiDeleted(id)
                : Strings.ScriptsUiDeleteMissing(id);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            status = Strings.ScriptsUiFailed(ex.Message);
        }

        Refresh();
        SetBusy(false);
        _workspace.SetStatus(status);
    }

    private void ShowNew()
    {
        var id = new TextBox { MinWidth = 32 };
        var template = new ComboBox { ItemsSource = BeaconTemplates.Names, SelectedIndex = 0, MinWidth = 20 };
        var fields = new StackPanel { Spacing = 1 };
        fields.Children.Add(ManagementUi.Body(Strings.ScriptsUiId, ManagementUi.Soft));
        fields.Children.Add(id);
        fields.Children.Add(ManagementUi.Body(Strings.ScriptsUiTemplate, ManagementUi.Soft));
        fields.Children.Add(template);
        fields.Children.Add(ManagementUi.Actions(
            ManagementUi.Button(Strings.ScriptsUiCreate, () =>
            {
                ScriptCreateResult result = _client.Scripts.Create(
                    template.SelectedItem?.ToString() ?? "empty", id.Text ?? string.Empty);
                if (!result.Created)
                {
                    _workspace.SetStatus(Strings.ScriptsUiFailed(result.Error));
                    return;
                }

                _workspace.HideModal();
                Refresh();
                _workspace.Push(new ScriptEditorPage(_workspace, _client, Path.GetFileNameWithoutExtension(result.Path!)));
            }, ManagementButtonKind.Primary),
            ManagementUi.Button(Strings.ScriptsUiCancel, _workspace.HideModal, ManagementButtonKind.Quiet)));
        _workspace.ShowModal(ManagementUi.Card(fields));
        id.Focus();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UpdateActions();
    }

    private static string FirstError(BeaconRunResult result)
        => result.Error?.Message
            ?? result.Diagnostics.FirstOrDefault(d => d.Severity == BeaconSeverity.Error)?.Message
            ?? string.Empty;

    private sealed record ScriptRow(ScriptInfo Script)
    {
        public override string ToString()
            => (Script.Running ? Strings.ScriptsUiRunning : Strings.ScriptsUiStopped) + "  " + Script.Id;
    }
}
