using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Beacon;

namespace Mcc.Cli.Tui.Management;

internal sealed class ScriptReplPage : ContentControl, IManagementWorkspacePage
{
    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly TextBox _input;
    private readonly TextBox _seed;
    private readonly TextBox _output;
    private readonly TextBox _locals;
    private readonly Control _inputPane;
    private readonly Control _resultPane;
    private readonly List<string> _history = [];
    private int _historyIndex;
    private bool _compact;

    public ScriptReplPage(ManagementWorkspace workspace, Client client)
    {
        _workspace = workspace;
        _client = client;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;

        _input = ReplTextBox(readOnly: false);
        // Tunnel prevents the multiline TextBox from applying its own Ctrl+Enter edit to the current selection before the REPL can evaluate the buffer.
        _input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        _seed = new TextBox
        {
            Watermark = Strings.ScriptsUiSeed,
            Width = 28,
            Height = 3,
            MinHeight = 3,
            Padding = new Thickness(1, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        _output = ReplTextBox(readOnly: true);
        _locals = ReplTextBox(readOnly: true);
        _locals.Text = _client.Scripts.ReplLocals();

        _inputPane = BuildInputPane();
        _resultPane = BuildResultPane();
        BuildLayout();
    }

    public bool UseWorkspaceScroll => false;

    public string? Hints => Strings.ScriptsUiReplHints;

    public void SetCompact(bool compact)
    {
        if (_compact == compact)
            return;

        _compact = compact;
        BuildLayout();
        _input.Focus();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _input.Focus();
    }

    internal static Grid BuildSplitLayout(Control input, Control results, bool compact)
    {
        var layout = compact
            ? new Grid { RowDefinitions = new RowDefinitions("*,*") }
            : new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };

        layout.Children.Add(input);
        if (compact)
            Grid.SetRow(results, 1);
        else
            Grid.SetColumn(results, 1);
        layout.Children.Add(results);
        return layout;
    }

    private static TextBox ReplTextBox(bool readOnly)
    {
        var textBox = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = readOnly,
            TextWrapping = TextWrapping.NoWrap,
            MinWidth = 12,
            MinHeight = 3,
            Background = ManagementUi.Canvas,
            Foreground = ManagementUi.Soft,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        ScrollViewer.SetVerticalScrollBarVisibility(textBox, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(textBox, ScrollBarVisibility.Auto);
        return textBox;
    }

    private Control BuildInputPane()
    {
        var pane = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0, 0, 1, 0),
        };
        pane.Children.Add(ManagementUi.Section(Strings.ScriptsUiInput));
        Grid.SetRow(_input, 1);
        pane.Children.Add(_input);
        Button evaluate = ManagementUi.Button(
            Strings.ScriptsUiEvaluate,
            () => _ = EvaluateAsync(),
            ManagementButtonKind.Primary);
        Control actions = BuildInputActions(evaluate, _seed);
        Grid.SetRow(actions, 2);
        pane.Children.Add(actions);
        return pane;
    }

    internal static Grid BuildInputActions(Button evaluate, TextBox seed)
    {
        var actions = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0, 1, 0, 0),
        };
        evaluate.Padding = new Thickness(2, 0);
        evaluate.MinHeight = 3;
        Grid.SetColumn(evaluate, 0);
        actions.Children.Add(evaluate);
        Grid.SetColumn(seed, 2);
        actions.Children.Add(seed);
        return actions;
    }

    private Control BuildResultPane()
    {
        var pane = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,2*,Auto,*"),
            Margin = new Thickness(1, 0, 0, 0),
        };
        pane.Children.Add(ManagementUi.Section(Strings.ScriptsUiOutput));
        Grid.SetRow(_output, 1);
        pane.Children.Add(_output);
        TextBlock localsHeading = ManagementUi.Section(Strings.ScriptsUiLocals);
        Grid.SetRow(localsHeading, 2);
        pane.Children.Add(localsHeading);
        Grid.SetRow(_locals, 3);
        pane.Children.Add(_locals);
        return pane;
    }

    private void BuildLayout()
    {
        if (Content is Grid oldLayout)
            oldLayout.Children.Clear();
        Content = null;
        Content = BuildSplitLayout(_inputPane, _resultPane, _compact);
    }

    private async Task EvaluateAsync()
    {
        string line = (_input.Text ?? string.Empty).Trim();
        if (line.Length == 0)
            return;

        int? seed = int.TryParse(_seed.Text, out int parsed) ? parsed : null;
        try
        {
            BeaconReplResult result = await _client.Scripts.EvaluateAsync(line, seed, _workspace.Closed);
            _history.Add(line);
            _historyIndex = _history.Count;
            string prefix = _output.Text is { Length: > 0 } ? _output.Text + "\n" : string.Empty;
            _output.Text = prefix + "> " + line + "\n" + (result.Success ? result.Output : result.Error);
            _output.CaretIndex = _output.Text.Length;
            _locals.Text = _client.Scripts.ReplLocals();
            _input.Text = string.Empty;
            _input.Focus();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsReplShortcut(e.Key, e.KeyModifiers))
            return;

        if (e.Key == Key.Enter)
        {
            _ = EvaluateAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.Up && _history.Count > 0)
        {
            _historyIndex = Math.Max(0, _historyIndex - 1);
            _input.Text = _history[_historyIndex];
            _input.CaretIndex = _input.Text.Length;
            e.Handled = true;
        }
        else if (e.Key == Key.Down && _history.Count > 0)
        {
            _historyIndex = Math.Min(_history.Count, _historyIndex + 1);
            _input.Text = _historyIndex == _history.Count ? string.Empty : _history[_historyIndex];
            _input.CaretIndex = _input.Text.Length;
            e.Handled = true;
        }
    }

    internal static bool IsReplShortcut(Key key, KeyModifiers modifiers)
        => modifiers.HasFlag(KeyModifiers.Control) && key is Key.Enter or Key.Up or Key.Down;
}
