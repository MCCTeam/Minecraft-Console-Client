using Mcc.Cli.Localization;
using Avalonia.Controls;
using Avalonia.Media;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>Shared uninstall confirmation used by plugin details and catalogue rows.</summary>
internal static class PluginUninstallDialog
{
    internal static void Open(PluginManagerContext ctx, string id, Action onSuccess)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(onSuccess);

        bool purge = false;
        Button check = PluginUi.Button(
            $"{ctx.Glyphs.CheckboxOff} {Strings.PmPurgeLabel}",
            () => { },
            PluginButtonKind.Quiet);
        check.Click += (_, _) =>
        {
            purge = !purge;
            check.Content = $"{(purge ? ctx.Glyphs.CheckboxOn : ctx.Glyphs.CheckboxOff)} {Strings.PmPurgeLabel}";
        };

        Border frame = PluginModal.Frame(ctx, Strings.PmUninstallTitleFor(id), out StackPanel content);
        content.Children.Add(check);
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmPurgeNote(id),
            Foreground = PluginUi.Muted,
            TextWrapping = TextWrapping.Wrap,
        });
        var warningText = new TextBlock
        {
            Foreground = PluginUi.Red,
            TextWrapping = TextWrapping.Wrap,
        };
        Border warning = PluginUi.Card(warningText, danger: true);
        warning.IsVisible = false;
        content.Children.Add(warning);
        content.Children.Add(PluginModal.ButtonRow(
            PluginModal.ActionButton(
                ctx,
                Strings.PmUninstall,
                () => _ = UninstallAsync(ctx, id, purge, warning, warningText, onSuccess),
                danger: true,
                closeModal: false),
            PluginModal.ActionButton(ctx, Strings.PmBack, () => { })));
        ctx.Owner.ShowModal(frame);
    }

    private static async Task UninstallAsync(
        PluginManagerContext ctx,
        string id,
        bool purge,
        Border warning,
        TextBlock warningText,
        Action onSuccess)
    {
        if (ctx.Market is null)
        {
            ctx.Owner.SetStatus(Strings.PmNoMarket);
            ShowWarning(warning, warningText, Strings.PmNoMarket);
            return;
        }

        try
        {
            PluginActionResult result = await ctx.RunMarketAsync(
                (market, ct) => market.UninstallAsync(id, purge, ct)).ConfigureAwait(true);
            ctx.Owner.SetStatus(result.Message);
            ctx.Log(result.Message);
            if (!result.Success)
            {
                ShowWarning(warning, warningText, result.Message);
                return;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ctx.Owner.SetStatus(ex.Message);
            ShowWarning(warning, warningText, ex.Message);
            return;
        }

        ctx.Owner.HideModal();
        onSuccess();
    }

    private static void ShowWarning(Border warning, TextBlock warningText, string message)
    {
        warningText.Text = message;
        warning.IsVisible = true;
    }
}
