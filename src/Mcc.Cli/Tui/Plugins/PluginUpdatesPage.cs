using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// The updates page: <c>plugins outdated</c> as selectable rows (per-row Update, pinned and blocked rows marked, never offered), plus Update all.
/// Pin state changes here refresh in place.
/// </summary>
internal sealed class PluginUpdatesPage : StackPanel
{
    private readonly PluginManagerContext _ctx;
    private readonly ListBox _list = new()
    {
        SelectionMode = SelectionMode.Single,
        AutoScrollToSelectedItem = true,
        MinHeight = 5,
        MaxHeight = 16,
    };
    private readonly TextBlock _header = new() { Foreground = PluginUi.Gold };
    private readonly Button _updateButton;
    private readonly Button _pinButton;
    private readonly List<PluginUpdateInfo> _shown = new();
    private int _selected;

    public PluginUpdatesPage(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        _ctx = ctx;
        Spacing = 1;
        Focusable = true;

        Children.Add(PluginUi.PageHeader(Strings.PmUpdatesTitle, Strings.PmUpdatesSubtitle));
        Children.Add(_header);
        Children.Add(PluginUi.Card(_list));
        _updateButton = PluginUi.Button(Strings.PmUpdate, UpdateSelected, PluginButtonKind.Primary);
        _pinButton = PluginUi.Button(Strings.PmPin, () => _ = TogglePinSelectedAsync());
        Children.Add(PluginUi.Actions(
            _updateButton,
            _pinButton,
            PluginUi.Button(Strings.PmUpdateAll, () => _ = UpdateAsync(null))));

        _ctx.Owner.SetHints(Strings.PmUpdatesHints);
        _list.SelectionChanged += (_, _) => Select(_list.SelectedIndex);
        _list.KeyDown += OnKeyDown;
        KeyDown += OnKeyDown;
        _ = LoadAsync();
    }

    /// <summary>Focuses the selected row once shown (pre-attach focus is a no-op).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_shown.Count > 0)
        {
            _list.SelectedIndex = Math.Clamp(_selected, 0, _shown.Count - 1);
            _list.Focus();
        }
        else
            Focus();
    }

    private async Task LoadAsync()
    {
        _list.Items.Clear();
        _shown.Clear();

        if (_ctx.Market is null)
        {
            _list.Items.Add(new TextBlock { Text = Strings.PmNoMarket, Foreground = PluginUi.Red });
            _header.Text = Strings.PmNoMarket;
            Select(-1);
            return;
        }

        IReadOnlyList<PluginUpdateInfo> updates;
        try
        {
            updates = await _ctx.Market.OutdatedAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
            return;
        }

        if (updates.Count == 0)
        {
            _list.Items.Add(new TextBlock { Text = Strings.PmNoUpdates, Foreground = PluginUi.Soft });
            _header.Text = Strings.PmNoUpdates;
            Select(-1);
            return;
        }

        int held = 0;
        foreach (PluginUpdateInfo update in updates)
        {
            if (update.Pinned || update.BlockedReason is { Length: > 0 })
                held++;
            _shown.Add(update);
            _list.Items.Add(new TextBlock
            {
                Text = RowText(update),
                Foreground = update.Error is { Length: > 0 }
                    ? PluginUi.Red
                    : update.Pinned || update.BlockedReason is { Length: > 0 }
                        ? PluginUi.Gold
                        : PluginUi.White,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        _header.Text = Strings.PmUpdatesHeader(updates.Count - held, held);

        _selected = Math.Clamp(_selected, 0, Math.Max(0, _shown.Count - 1));
        if (_shown.Count > 0)
        {
            _list.SelectedIndex = _selected;
            _list.Focus();
        }
    }

    private void Select(int selected)
    {
        _selected = selected;
        bool hasSelection = selected >= 0 && selected < _shown.Count;
        _updateButton.IsEnabled = hasSelection && CanUpdate(_shown[selected]);
        _pinButton.IsEnabled = hasSelection;
        _pinButton.Content = hasSelection && _shown[selected].Pinned ? Strings.PmUnpin : Strings.PmPin;
    }

    private static bool CanUpdate(PluginUpdateInfo update)
        => !update.Pinned
           && update.BlockedReason is not { Length: > 0 }
           && update.Error is not { Length: > 0 };

    private void UpdateSelected()
    {
        if (_selected >= 0 && _selected < _shown.Count && CanUpdate(_shown[_selected]))
            _ = UpdateAsync(_shown[_selected].Id);
    }

    private async Task TogglePinSelectedAsync()
    {
        if (_ctx.Market is null || _selected < 0 || _selected >= _shown.Count)
            return;

        PluginUpdateInfo update = _shown[_selected];
        try
        {
            PluginActionResult result = await _ctx.RunMarketAsync((market, ct) => update.Pinned
                ? market.UnpinAsync(update.Id, ct)
                : market.PinAsync(update.Id, null, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginUpdatesPage(_ctx));
    }

    private static string RowText(PluginUpdateInfo update)
    {
        if (update.Error is { Length: > 0 } error)
            return $"{update.Id}: {error}";

        var text = new System.Text.StringBuilder(
            $"{update.Id} {update.InstalledVersion} -> {update.AvailableVersion ?? "?"}");
        if (update.BlockedReason is { Length: > 0 } blocked)
            text.Append(' ').Append(Strings.PmBlockedTagFor(blocked));

        if (update.Pinned)
            text.Append(' ').Append(Strings.PmPinnedTag);

        return text.ToString();
    }

    private async Task UpdateAsync(string? id)
    {
        if (_ctx.Market is null)
            return;

        _ctx.Owner.SetStatus(Strings.PmWorking);
        try
        {
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.UpdateAsync(id, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginUpdatesPage(_ctx));
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_shown.Count == 0)
            return;

        switch (e.Key)
        {
            case Key.Enter when !e.Handled
                                && _selected >= 0
                                && _selected < _shown.Count
                                && CanUpdate(_shown[_selected]):
                UpdateSelected();
                e.Handled = true;
                break;
            case Key.P when !e.Handled && _selected >= 0 && _selected < _shown.Count:
                _ = TogglePinSelectedAsync();
                e.Handled = true;
                break;
        }
    }
}
