using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core.Plugins;
using Mcc.Cli.Localization;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// One plugin's page: the <c>info</c> rows, the dependency lines of <c>deps</c>, and every per-plugin verb as a button (enable/disable, reload, unload, update, pin/unpin, settings, uninstall with a purge modal).
/// Loads async (host reads are instant, market reads are not) and rebuilds in place after every action, so it never shows a stale state.
/// </summary>
internal sealed class PluginDetailPage : StackPanel
{
    private readonly PluginManagerContext _ctx;
    private readonly string _id;
    private readonly StackPanel _content = new() { Spacing = 0 };
    private Control? _firstFocus;

    /// <param name="openUninstall">Open with the uninstall purge modal up (the list U shortcut).</param>
    public PluginDetailPage(PluginManagerContext ctx, string id, bool openUninstall = false)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(id);
        _ctx = ctx;
        _id = id;
        Spacing = 1;
        Focusable = true;

        Children.Add(PluginUi.PageHeader(id, Strings.PmPluginDetailSubtitle));
        Children.Add(PluginUi.Card(_content));
        KeyDown += OnKeyDown;
        _ = LoadAsync();

        if (openUninstall)
            ShowUninstallModal();
    }

    private async Task LoadAsync()
    {
        _content.Children.Clear();

        if (_ctx.Host is not { } host)
        {
            _content.Children.Add(new TextBlock { Text = Strings.PmNoHost, Foreground = PluginUi.Red });
            return;
        }

        PluginInfo? plugin = host.List().FirstOrDefault(
            p => string.Equals(p.Id, _id, StringComparison.OrdinalIgnoreCase));
        if (plugin is null)
        {
            _content.Children.Add(new TextBlock { Text = Strings.PmListEmpty(_id), Foreground = PluginUi.Soft });
            return;
        }

        _content.Children.Add(PluginUi.SectionTitle(Strings.PmOverview));
        string state = !plugin.Enabled
            ? Strings.PmStateDisabled
            : plugin.Loaded ? Strings.PmStateEnabledLoaded : Strings.PmStateEnabledNotLoaded;
        string entry = plugin.Entry == PluginEntryKind.Source ? Strings.PmEntrySource : Strings.PmEntryDll;
        Row(Strings.PmInfoEntry, $"{state} · {entry}");
        Row(Strings.PmInfoFolder, await FolderOfAsync(plugin.Id).ConfigureAwait(true));
        PluginMarketInfo? marketInfo = await AddMarketRowsAsync(plugin.Id).ConfigureAwait(true);
        AddDepRows(plugin.Id);

        if (!string.IsNullOrWhiteSpace(plugin.Status))
            Row(Strings.PmInfoErrors, plugin.Status);

        if (plugin.FaultCount > 0)
            Row(Strings.PmInfoErrors, $"{plugin.FaultCount} · {plugin.LastError ?? string.Empty}");

        _content.Children.Add(PluginUi.SectionTitle(Strings.PmActions));
        var buttons = new WrapPanel();
        AddButton(buttons, plugin.Enabled ? Strings.PmDisable : Strings.PmEnable, () => _ = ToggleAsync(plugin));
        AddButton(buttons, Strings.PmReload, () => _ = ActAsync(
            static (h, id, ct) => h.ReloadAsync(id, ct)));
        AddButton(buttons, Strings.PmUnload, () => _ = ActAsync(
            static (h, id, ct) => h.UnloadAsync(id, ct)));
        AddButton(buttons, Strings.PmUpdate, () => _ = UpdateAsync());
        AddButton(buttons, MccStrings.Get("tui.plugins.versions"), () => _ctx.Show(new PluginVersionsPage(_ctx, _id)));
        AddButton(buttons, MccStrings.Get("tui.plugins.rollback"), () => _ctx.Show(new PluginRollbackPage(_ctx)));
        AddButton(buttons, PinActionLabel(marketInfo is { Pinned: true }), () => _ = PinAsync());
        AddButton(buttons, Strings.PmSettings, () => _ctx.Show(new PluginSettingsPage(_ctx, _id)));
        AddButton(buttons, Strings.PmUninstall, ShowUninstallModal, kind: PluginButtonKind.Danger);
        _content.Children.Add(buttons);

        _firstFocus = null;
        foreach (Control child in buttons.Children)
        {
            if (child is Button button)
            {
                _firstFocus = button;
                break;
            }
        }

        _firstFocus?.Focus();
    }

    /// <summary>Focuses the first action once shown (pre-attach focus is a no-op).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_firstFocus is not null)
            _firstFocus.Focus();
        else
            Focus();
    }

    private void Row(string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        _content.Children.Add(PluginUi.KeyValue(label, value));
    }

    private void AddButton(
        WrapPanel buttons,
        string label,
        Action onClick,
        PluginButtonKind kind = PluginButtonKind.Secondary)
    {
        Button button = PluginUi.Button(label, onClick, kind);
        button.Margin = new Thickness(0, 0, 1, 1);
        buttons.Children.Add(button);
    }

    private async Task<string> FolderOfAsync(string id)
    {
        if (_ctx.Market is { } market)
        {
            try
            {
                if (await market.InfoAsync(id).ConfigureAwait(true) is { Folder: { Length: > 0 } folder })
                    return folder;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ctx.Owner.SetStatus(ex.Message);
            }
        }

        return _ctx.Host is null ? string.Empty : Path.Combine(_ctx.Host.PluginsRoot, id);
    }

    internal static string PinActionLabel(bool pinned) => pinned ? Strings.PmUnpin : Strings.PmPin;

    private async Task<PluginMarketInfo?> AddMarketRowsAsync(string id)
    {
        if (_ctx.Market is not { } market)
            return null;

        PluginMarketInfo? info;
        try
        {
            info = await market.InfoAsync(id).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
            return null;
        }

        if (info is null)
            return null;

        Row(MccStrings.Get("tui.plugins.version"), info.Version);
        Row(MccStrings.Get("tui.plugins.target"), info.Target ?? string.Empty);
        Row(MccStrings.Get("tui.plugins.hash"), info.AssetHash ?? string.Empty);
        Row(Strings.PmInfoSource, info.IsLocal ? Strings.PmInfoLocal : info.Source ?? string.Empty);
        Row(Strings.PmInfoMarket, info.Marketplace ?? string.Empty);
        Row(Strings.PmInfoInstalled, info.Installed?.ToString("u", CultureInfo.InvariantCulture) ?? string.Empty);
        Row(Strings.PmInfoPinned, info.Pinned ? Strings.PmInfoPinnedAtVersion(info.PinnedVersion ?? info.Version) : string.Empty);
        Row(Strings.PmInfoAbout, info.Description ?? string.Empty);
        Row(Strings.PmInfoHomepage, info.Homepage ?? string.Empty);
        Row(Strings.PmInfoTags, string.Join(", ", info.Tags));
        Row(Strings.PmInfoUses, string.Join(", ", info.Uses));
        Row(Strings.PmInfoRequires, string.Join(", ", info.Requires));
        Row(Strings.PmInfoOptional, string.Join(", ", info.Optional));
        Row(Strings.PmInfoNeededBy, string.Join(", ", info.Dependents));
        Row(Strings.PmInfoExports, string.Join(", ", info.Exports));
        Row(Strings.PmInfoLanguages, string.Join(", ", info.Languages));
        Row(Strings.PmInfoManual, string.Join(", ", info.ManualTopics));
        if (info.Offline)
            Row(Strings.PmInfoOffline, Strings.PmInfoOfflineYes);

        return info;
    }

    private void AddDepRows(string id)
    {
        if (_ctx.Host?.DependenciesOf(id) is not { } deps)
            return;

        if (deps.Missing.Count > 0)
            Row(Strings.PmInfoErrors, string.Join(", ", deps.Missing));
    }

    private async Task ToggleAsync(PluginInfo plugin)
        => await ActAsync(plugin.Enabled
            ? static (h, id, ct) => h.DisableAsync(id, ct)
            : static (h, id, ct) => h.EnableAsync(id, ct)).ConfigureAwait(true);

    private async Task ToggleCurrentAsync()
    {
        if (_ctx.Host is not { } host)
            return;

        PluginInfo? plugin = host.List().FirstOrDefault(
            p => string.Equals(p.Id, _id, StringComparison.OrdinalIgnoreCase));
        if (plugin is not null)
            await ToggleAsync(plugin).ConfigureAwait(true);
    }

    private async Task ActAsync(Func<IPluginHost, string, CancellationToken, Task<PluginActionResult>> op)
    {
        if (_ctx.Host is null)
        {
            _ctx.Owner.SetStatus(Strings.PmNoHost);
            return;
        }

        try
        {
            PluginActionResult result = await op(_ctx.Host, _id, CancellationToken.None).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginDetailPage(_ctx, _id));
    }

    private async Task UpdateAsync()
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
                (market, ct) => market.UpdateAsync(_id, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginDetailPage(_ctx, _id));
    }

    private async Task PinAsync()
    {
        if (_ctx.Market is null)
        {
            _ctx.Owner.SetStatus(Strings.PmNoMarket);
            return;
        }

        bool pinned;
        try
        {
            pinned = await _ctx.Market.InfoAsync(_id).ConfigureAwait(true) is { Pinned: true };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
            return;
        }

        if (pinned)
        {
            try
            {
                PluginActionResult result = await _ctx.RunMarketAsync(
                    (market, ct) => market.UnpinAsync(_id, ct)).ConfigureAwait(true);
                _ctx.Owner.SetStatus(result.Message);
                _ctx.Log(result.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ctx.Owner.SetStatus(ex.Message);
            }

            _ctx.Owner.ReplaceTop(new PluginDetailPage(_ctx, _id));
            return;
        }

        var version = new TextBox();
        Border frame = PluginModal.Frame(_ctx, Strings.PmPinTitleFor(_id), out StackPanel content);
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldPinVersion, version));
        content.Children.Add(PluginModal.ButtonRow(
            PluginModal.ActionButton(_ctx, Strings.PmSetPin, () => _ = PinSetAsync(version.Text ?? string.Empty)),
            PluginModal.ActionButton(_ctx, Strings.PmBack, () => { })));
        PluginModal.EscToHide(_ctx, version);
        _ctx.Owner.ShowModal(frame);
        version.Focus();
    }

    private async Task PinSetAsync(string version)
    {
        if (_ctx.Market is null)
            return;

        try
        {
            string? at = string.IsNullOrWhiteSpace(version) ? null : version.Trim();
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.PinAsync(_id, at, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.ReplaceTop(new PluginDetailPage(_ctx, _id));
    }

    private void ShowUninstallModal()
        => PluginUninstallDialog.Open(_ctx, _id, _ctx.Owner.BackToList);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Letters are shortcuts only when no editor owns the keystrokes (the pin form's version field would otherwise reload the plugin mid-typing).
        // Escape still bubbles to the overlay.
        if (e.Source is TextBox)
            return;

        switch (e.Key)
        {
            case Key.E:
                _ = ToggleCurrentAsync();
                e.Handled = true;
                break;
            case Key.R:
                _ = ActAsync(static (h, id, ct) => h.ReloadAsync(id, ct));
                e.Handled = true;
                break;
            case Key.U:
                ShowUninstallModal();
                e.Handled = true;
                break;
            case Key.P:
                _ = PinAsync();
                e.Handled = true;
                break;
            case Key.S:
            case Key.Enter:
                _ctx.Show(new PluginSettingsPage(_ctx, _id));
                e.Handled = true;
                break;
        }
    }
}
