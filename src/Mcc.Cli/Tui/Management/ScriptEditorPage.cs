using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Beacon;

namespace Mcc.Cli.Tui.Management;

internal sealed class ScriptEditorPage : DockPanel, IManagementWorkspacePage
{
    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly string _id;
    private readonly TextBlock _title;
    private readonly TextBlock _caret;
    private readonly TextBox _editor;
    private readonly ListBox _diagnostics;
    private readonly Task _loadTask;
    private ScriptDocument? _document;
    private bool _dirty;
    private bool _loading;

    public ScriptEditorPage(ManagementWorkspace workspace, Client client, string id)
    {
        _workspace = workspace;
        _client = client;
        _id = id;

        _title = ManagementUi.Section(id);
        _caret = ManagementUi.Body(Strings.ScriptsUiCaret(1, 1), ManagementUi.Muted);
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        heading.Children.Add(_title);
        Grid.SetColumn(_caret, 1);
        heading.Children.Add(_caret);
        SetDock(heading, Dock.Top);
        Children.Add(heading);

        var actions = ManagementUi.Actions(
            ManagementUi.Button(Strings.ScriptsUiSave, () => _ = SaveAsync(false, false), ManagementButtonKind.Primary),
            ManagementUi.Button(Strings.ScriptsUiLint, Lint),
            ManagementUi.Button(Strings.ScriptsUiFormat, PreviewFormat));
        SetDock(actions, Dock.Top);
        Children.Add(actions);

        _diagnostics = new ListBox
        {
            MaxHeight = 7,
            MinHeight = 3,
            Background = ManagementUi.Panel,
            Foreground = ManagementUi.Soft,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
        };
        _diagnostics.SelectionChanged += (_, _) => FocusDiagnostic();
        ScrollViewer.SetVerticalScrollBarVisibility(_diagnostics, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(_diagnostics, ScrollBarVisibility.Disabled);
        var diagnosticBlock = new StackPanel { Spacing = 0 };
        diagnosticBlock.Children.Add(ManagementUi.Section(Strings.ScriptsUiDiagnostics));
        diagnosticBlock.Children.Add(_diagnostics);
        SetDock(diagnosticBlock, Dock.Bottom);
        Children.Add(diagnosticBlock);

        _editor = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            MinWidth = 24,
            MinHeight = 6,
            Background = ManagementUi.Canvas,
            Foreground = ManagementUi.Soft,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_editor, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(_editor, ScrollBarVisibility.Auto);
        _editor.TextChanged += (_, _) =>
        {
            if (!_loading)
                SetDirty(true);
            UpdateCaret();
        };
        _editor.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.CaretIndexProperty)
                UpdateCaret();
        };
        _editor.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
        Children.Add(_editor);
        _loadTask = LoadAsync();
    }

    public bool UseWorkspaceScroll => false;

    public bool HandleBack()
    {
        if (!_dirty)
            return false;

        ShowDirtyClose();
        return true;
    }

    public void PreviewFormat() => _ = PreviewFormatAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _document = await _client.Scripts.LoadDocumentAsync(_id, _workspace.Closed);
            _editor.Text = _document.Source;
            _editor.CaretIndex = 0;
            SetDirty(false);
            Lint();
            _editor.Focus();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
        finally
        {
            _loading = false;
        }
    }

    private void Lint()
    {
        if (_document is null)
            return;

        BeaconLintReport report = _client.Scripts.Lint(
            Path.GetFileName(_document.Path), _editor.Text ?? string.Empty);
        ShowDiagnostics(report.Diagnostics);
        _workspace.SetStatus(report.Diagnostics.Count == 0
            ? Strings.ScriptsUiNoDiagnostics
            : string.Join(" · ", report.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
    }

    private async Task SaveAsync(bool overwrite, bool closeAfter)
    {
        await _loadTask;
        if (_document is null)
            return;

        ScriptSaveResult result;
        try
        {
            result = await _client.Scripts.SaveDocumentAsync(
                _document, _editor.Text ?? string.Empty, overwrite, _workspace.Closed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
            return;
        }

        ShowDiagnostics(result.Diagnostics);
        if (result.Outcome == ScriptSaveOutcome.ValidationErrors)
        {
            _workspace.SetStatus(Strings.ScriptsUiSaveBlocked);
            FocusFirstError();
            return;
        }

        if (result.Outcome == ScriptSaveOutcome.ExternalConflict)
        {
            ShowConflict(closeAfter);
            return;
        }

        _document = result.Document;
        SetDirty(false);
        bool running = _client.Scripts.Discover().Any(script => script.Id == _id && script.Running);
        _workspace.SetStatus(running && !_client.Scripts.WatchEnabled
            ? Strings.ScriptsUiSavedReload
            : Strings.ScriptsUiSaved);
        if (closeAfter)
        {
            _workspace.HideModal();
            _workspace.RequestBack();
        }
    }

    private async Task PreviewFormatAsync()
    {
        await _loadTask;
        if (_document is null)
            return;

        BeaconFormatResult formatted = _client.Scripts.Format(
            Path.GetFileName(_document.Path), _editor.Text ?? string.Empty);
        if (!formatted.Changed)
        {
            _workspace.SetStatus(Strings.ScriptsUiFormatClean);
            return;
        }

        Control heading = ManagementUi.Section(Strings.ScriptsUiFormatPreview);
        Control preview = BuildDiffPreview(formatted.Diff);
        Control actions = ManagementUi.Actions(
            ManagementUi.Button(Strings.ScriptsUiApplyBuffer, () =>
            {
                _editor.Text = formatted.Formatted;
                SetDirty(true);
                _workspace.HideModal();
                _editor.Focus();
            }, ManagementButtonKind.Primary),
            ManagementUi.Button(Strings.ScriptsUiCancel, _workspace.HideModal, ManagementButtonKind.Quiet));
        Grid content = BuildFormatPreviewLayout(heading, preview, actions);
        _workspace.ShowModal(ManagementUi.Card(content));
    }

    internal static Grid BuildFormatPreviewLayout(Control heading, Control preview, Control actions)
    {
        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            ColumnDefinitions = new ColumnDefinitions("*"),
            MaxWidth = 96,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        content.Children.Add(heading);
        Grid.SetRow(preview, 1);
        content.Children.Add(preview);
        Grid.SetRow(actions, 2);
        content.Children.Add(actions);
        return content;
    }

    private void ShowDirtyClose()
    {
        var content = new StackPanel { Spacing = 1 };
        content.Children.Add(ManagementUi.Section(Strings.ScriptsUiDirtyClose));
        content.Children.Add(ManagementUi.Actions(
            ManagementUi.Button(Strings.ScriptsUiSave, () => _ = SaveAsync(false, true), ManagementButtonKind.Primary),
            ManagementUi.Button(Strings.ScriptsUiDiscard, () =>
            {
                SetDirty(false);
                _workspace.HideModal();
                _workspace.RequestBack();
            }, ManagementButtonKind.Danger),
            ManagementUi.Button(Strings.ScriptsUiCancel, _workspace.HideModal, ManagementButtonKind.Quiet)));
        _workspace.ShowModal(ManagementUi.Card(content, compact: true));
    }

    private void ShowConflict(bool closeAfter)
    {
        var content = new StackPanel { Spacing = 1 };
        content.Children.Add(ManagementUi.Section(Strings.ScriptsUiSaveConflict));
        content.Children.Add(ManagementUi.Actions(
            ManagementUi.Button(Strings.ScriptsUiReloadDisk, async () =>
            {
                _workspace.HideModal();
                await ReloadFromDiskAsync();
            }),
            ManagementUi.Button(Strings.ScriptsUiOverwrite, () => _ = SaveAsync(true, closeAfter), ManagementButtonKind.Danger),
            ManagementUi.Button(Strings.ScriptsUiCancel, _workspace.HideModal, ManagementButtonKind.Quiet)));
        _workspace.ShowModal(ManagementUi.Card(content, danger: true));
    }

    private async Task ReloadFromDiskAsync()
    {
        _loading = true;
        try
        {
            _document = await _client.Scripts.LoadDocumentAsync(_id, _workspace.Closed);
            _editor.Text = _document.Source;
            SetDirty(false);
            Lint();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
        finally
        {
            _loading = false;
        }
    }

    private void ShowDiagnostics(IReadOnlyList<BeaconDiagnostic> diagnostics)
    {
        _diagnostics.ItemsSource = diagnostics.Count == 0
            ? [new DiagnosticRow(null, Strings.ScriptsUiNoDiagnostics)]
            : diagnostics.Select(diagnostic => new DiagnosticRow(
                diagnostic,
                $"{diagnostic.Severity} {diagnostic.Code} · {diagnostic.Span.Line}:{diagnostic.Span.Column} · {diagnostic.Message}"))
                .ToArray();
    }

    private void FocusFirstError()
    {
        if ((_diagnostics.ItemsSource as IEnumerable<DiagnosticRow>)?
            .FirstOrDefault(row => row.Diagnostic?.Severity == BeaconSeverity.Error) is { } row)
        {
            _diagnostics.SelectedItem = row;
            FocusDiagnostic();
        }
    }

    private void FocusDiagnostic()
    {
        if (_diagnostics.SelectedItem is not DiagnosticRow { Diagnostic: { } diagnostic })
            return;

        string source = _editor.Text ?? string.Empty;
        int index = IndexAt(source, diagnostic.Span.Origin.Line, diagnostic.Span.Origin.Column);
        _editor.CaretIndex = index;
        _editor.Focus();
    }

    private void UpdateCaret()
    {
        string source = _editor.Text ?? string.Empty;
        int caret = Math.Clamp(_editor.CaretIndex, 0, source.Length);
        int line = 1;
        int column = 1;
        for (int i = 0; i < caret; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
                column++;
        }
        _caret.Text = Strings.ScriptsUiCaret(line, column);
    }

    private void SetDirty(bool dirty)
    {
        _dirty = dirty;
        _title.Text = _id + (dirty ? " · " + Strings.ScriptsUiDirty : string.Empty);
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = SaveAsync(false, false);
            e.Handled = true;
        }
    }

    private static int IndexAt(string source, int line, int column)
    {
        int index = 0;
        for (int current = 1; current < line && index < source.Length; current++)
        {
            int newline = source.IndexOf('\n', index);
            index = newline < 0 ? source.Length : newline + 1;
        }
        return Math.Min(source.Length, index + Math.Max(0, column - 1));
    }

    internal static DiffLineKind ClassifyDiffLine(string line)
    {
        if (line.StartsWith("+++", StringComparison.Ordinal))
            return DiffLineKind.AddedHeader;
        if (line.StartsWith("---", StringComparison.Ordinal))
            return DiffLineKind.RemovedHeader;
        if (line.StartsWith('+'))
            return DiffLineKind.Added;
        if (line.StartsWith('-'))
            return DiffLineKind.Removed;
        if (line.StartsWith("@@", StringComparison.Ordinal))
            return DiffLineKind.Hunk;
        return DiffLineKind.Context;
    }

    private static Control BuildDiffPreview(string diff)
    {
        var block = new TextBlock
        {
            Background = ManagementUi.Canvas,
            Foreground = ManagementUi.Soft,
            TextWrapping = TextWrapping.NoWrap,
        };
        string[] lines = diff.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            IBrush foreground = ClassifyDiffLine(line) switch
            {
                DiffLineKind.Added or DiffLineKind.AddedHeader => ManagementUi.Green,
                DiffLineKind.Removed or DiffLineKind.RemovedHeader => ManagementUi.Red,
                DiffLineKind.Hunk => ManagementUi.Cyan,
                _ => ManagementUi.Soft,
            };
            block.Inlines!.Add(new Run(line + (i + 1 < lines.Length ? "\n" : string.Empty))
            {
                Foreground = foreground,
                FontWeight = ClassifyDiffLine(line) is DiffLineKind.AddedHeader
                    or DiffLineKind.RemovedHeader
                    or DiffLineKind.Hunk
                    ? FontWeight.Bold
                    : FontWeight.Normal,
            });
        }

        return new Border
        {
            Child = new ScrollViewer
            {
                Content = block,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            },
            Background = ManagementUi.Canvas,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(1),
        };
    }

    internal enum DiffLineKind
    {
        Context,
        Added,
        Removed,
        AddedHeader,
        RemovedHeader,
        Hunk,
    }

    private sealed record DiagnosticRow(BeaconDiagnostic? Diagnostic, string Label)
    {
        public override string ToString() => Label;
    }
}
