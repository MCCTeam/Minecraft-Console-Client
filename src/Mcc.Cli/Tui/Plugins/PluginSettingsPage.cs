using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// One plugin's settings editor: every modeled <c>settings.toml</c> row as a labeled editor (toggles for bools, fields for the rest, sections as headers, comments as descriptions), unmodeled content shown read-only.
/// Save merges values line by line (comments and unknown shapes survive) and reloads the plugin; Reset rewrites from the defaults behind a confirm; Regenerate rewrites the comments.
/// </summary>
internal sealed class PluginSettingsPage : StackPanel
{
    private readonly PluginManagerContext _ctx;
    private readonly string _id;
    private readonly StackPanel _content = new() { Spacing = 1 };
    private readonly List<RowEditor> _editors = new();
    private Control? _firstFocus;

    public PluginSettingsPage(PluginManagerContext ctx, string id)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(id);
        _ctx = ctx;
        _id = id;
        Spacing = 1;
        Focusable = true;

        Children.Add(PluginUi.PageHeader(Strings.PmSettingsTitle(id), Strings.PmSettingsSubtitle));
        Children.Add(PluginUi.Card(_content));
        _ctx.Owner.SetHints(Strings.PmSettingsHints);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _content.Children.Clear();
        _editors.Clear();
        if (_ctx.Host is null)
        {
            _content.Children.Add(new TextBlock { Text = Strings.PmNoHost, Foreground = PluginUi.Red });
            return;
        }

        string path = _ctx.SettingsPath(_id);
        string toml;
        try
        {
            toml = await File.ReadAllTextAsync(path).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _content.Children.Add(new TextBlock { Text = Strings.PmNoSettingsFile, Foreground = PluginUi.Soft });
            _content.Children.Add(new TextBlock { Text = ex.Message, Foreground = PluginUi.Muted });
            AddFooter();
            return;
        }

        foreach (SettingRow row in PluginSettingsModel.ParseRows(toml))
        {
            if (row.Kind == SettingValueKind.Section)
            {
                _content.Children.Add(PluginUi.SectionTitle($"[{row.Section}]"));
                continue;
            }

            if (row.Kind == SettingValueKind.Unsupported)
            {
                _content.Children.Add(new TextBlock { Text = row.Key, Foreground = PluginUi.Soft });
                _content.Children.Add(new TextBlock
                {
                    Text = row.Value,
                    Foreground = PluginUi.Muted,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(2, 0, 0, 0),
                });
                continue;
            }

            _content.Children.Add(PluginModal.LabeledField(row.Key, BuildEditor(row)));
            if (row.Comment.Length > 0)
            {
                _content.Children.Add(new TextBlock
                {
                    Text = row.Comment,
                    Foreground = PluginUi.Muted,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
            }
        }

        AddFooter();
        FocusFirstEditor();
    }

    /// <summary>Focuses the first editor once shown (pre-attach focus is a no-op).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_firstFocus is not null)
            _firstFocus.Focus();
        else
            Focus();
    }

    private void FocusFirstEditor()
    {
        // Row editors render in row order; the first focusable control found is the first row.
        foreach (Control child in _content.Children)
        {
            if (FindFocusable(child) is { } focusable)
            {
                _firstFocus = focusable;
                focusable.Focus();
                return;
            }
        }
    }

    private static Control? FindFocusable(Control root)
    {
        if (root is TextBox or Button)
            return root;

        IEnumerable<Control> children = root switch
        {
            Panel panel => panel.Children.OfType<Control>(),
            ContentControl content => content.Content is Control single ? [single] : [],
            _ => [],
        };

        foreach (Control child in children)
        {
            if (FindFocusable(child) is { } found)
                return found;
        }

        return null;
    }

    private Control BuildEditor(SettingRow row)
    {
        if (row.Kind == SettingValueKind.Bool)
        {
            bool on = string.Equals(row.Value, "true", StringComparison.OrdinalIgnoreCase);
            var toggle = new Button
            {
                Content = on ? _ctx.Glyphs.CheckboxOn : _ctx.Glyphs.CheckboxOff,
                Background = PluginUi.Raised,
                Foreground = PluginUi.Cyan,
                BorderBrush = PluginUi.CyanDark,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1, 0),
            };
            var editor = new RowEditor(row, () => on ? "true" : "false");
            toggle.Click += (_, _) =>
            {
                on = !on;
                toggle.Content = on ? _ctx.Glyphs.CheckboxOn : _ctx.Glyphs.CheckboxOff;
            };
            _editors.Add(editor);
            return toggle;
        }

        var field = new TextBox { Text = row.Value };
        _editors.Add(new RowEditor(row, () => field.Text ?? string.Empty));
        return field;
    }

    private void AddFooter()
    {
        var buttons = new WrapPanel();
        AddButton(buttons, Strings.PmSave, () => _ = SaveAsync(), PluginButtonKind.Primary);
        AddButton(buttons, Strings.PmResetDefaults, ShowResetModal, PluginButtonKind.Danger);
        AddButton(buttons, Strings.PmRegenComments, () => _ = RegenAsync());
        _content.Children.Add(PluginUi.SectionTitle(Strings.PmActions));
        _content.Children.Add(buttons);
    }

    private void AddButton(
        WrapPanel buttons,
        string label,
        Action onClick,
        PluginButtonKind kind = PluginButtonKind.Secondary)
    {
        Button button = PluginUi.Button(label, onClick, kind, isDefault: kind == PluginButtonKind.Primary);
        button.Margin = new Thickness(0, 0, 1, 1);
        buttons.Children.Add(button);
    }

    private async Task SaveAsync()
    {
        if (_ctx.Host is null)
            return;

        string path = _ctx.SettingsPath(_id);
        string original;
        try
        {
            original = await File.ReadAllTextAsync(path).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _ctx.Owner.SetStatus(ex.Message);
            return;
        }

        var edits = new List<SettingEdit>();
        foreach (RowEditor editor in _editors)
        {
            string display = editor.Read();
            if (string.Equals(display, editor.Row.Value, StringComparison.Ordinal))
                continue;

            if (!PluginSettingsModel.TryRender(editor.Row, display, out string rendered, out string? error))
            {
                _ctx.Owner.SetStatus($"{editor.Row.Key}: {error}");
                return;
            }

            edits.Add(new SettingEdit(editor.Row.LineIndex, rendered));
        }

        if (edits.Count == 0)
        {
            _ctx.Owner.SetStatus(Strings.PmSettingsSavedNothing);
            return;
        }

        try
        {
            await File.WriteAllTextAsync(path, PluginSettingsModel.ApplyEdits(original, edits)).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _ctx.Owner.SetStatus(ex.Message);
            return;
        }

        try
        {
            DMCBK.Core.Plugins.PluginActionResult result =
                await _ctx.Host.ReloadAsync(_id).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private void ShowResetModal()
    {
        Border frame = PluginModal.Frame(_ctx, Strings.PmResetTitleFor(_id), out StackPanel content);
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmResetBody,
            Foreground = PluginUi.White,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(PluginModal.ButtonRow(
            PluginModal.ActionButton(_ctx, Strings.PmReset, () => _ = ResetAsync(), danger: true),
            PluginModal.ActionButton(_ctx, Strings.PmBack, () => { })));
        _ctx.Owner.ShowModal(frame);
    }

    private async Task ResetAsync()
    {
        if (_ctx.Host is null)
            return;

        try
        {
            DMCBK.Core.Plugins.PluginActionResult result =
                await _ctx.Host.ResetSettingsAsync(_id).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RegenAsync()
    {
        if (_ctx.Host is null)
            return;

        try
        {
            DMCBK.Core.Plugins.PluginActionResult result =
                await _ctx.Host.RegenerateSettingsAsync(_id).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        await LoadAsync().ConfigureAwait(true);
    }

    /// <summary>A row's live editor: reads current display text for rendering.</summary>
    private sealed record RowEditor(SettingRow Row, Func<string> Read);
}
