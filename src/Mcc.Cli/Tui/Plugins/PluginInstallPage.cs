using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core.Plugins;
using Mcc.Cli.Localization;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// The acquire flows: Install first offers direct ID entry or a complete marketplace catalogue; direct install and search use centered forms, while confirmations and result lists are pages.
/// </summary>
internal static class PluginInstallForm
{
    /// <summary>Opens the install-path chooser.</summary>
    internal static void Open(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        Border frame = PluginModal.Frame(ctx, Strings.PmInstallTitle, out StackPanel content);
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmInstallChoiceHint,
            Foreground = PluginUi.Soft,
            TextWrapping = TextWrapping.Wrap,
        });
        Button byId = PluginModal.ActionButton(
            ctx, Strings.PmInstallById, () => OpenById(ctx), primary: true);
        Button browse = PluginModal.ActionButton(
            ctx, Strings.PmBrowsePlugins, () => ctx.Show(new PluginCatalogPage(ctx)));
        content.Children.Add(PluginModal.ButtonRow(byId, browse));
        ctx.Owner.ShowModal(frame);
        byId.Focus();
    }

    /// <summary>Opens the direct install form (plugin ID + optional version).</summary>
    private static void OpenById(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var source = new TextBox();
        var version = new TextBox();
        var sourcePreference = new CheckBox { Content = MccStrings.Get("tui.plugins.source") };
        var sourceFallback = new CheckBox { Content = MccStrings.Get("tui.plugins.source_fallback") };
        var prerelease = new CheckBox { Content = MccStrings.Get("tui.plugins.prerelease") };

        Border frame = PluginModal.Frame(ctx, Strings.PmInstallById, out StackPanel content);
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldPluginId, source));
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldVersion, version));
        content.Children.Add(sourcePreference);
        content.Children.Add(sourceFallback);
        content.Children.Add(prerelease);
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmInstallHint,
            Foreground = PluginUi.Muted,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(PluginModal.ButtonRow(PluginModal.ActionButton(
            ctx,
            Strings.PmInstall,
            () => _ = RunInstallAsync(ctx, source.Text ?? string.Empty, version.Text ?? string.Empty,
                new PluginInstallOptions(sourcePreference.IsChecked == true, sourceFallback.IsChecked == true, prerelease.IsChecked == true)),
            primary: true,
            closeModal: false)));
        PluginModal.EscToHide(ctx, source, version);
        ctx.Owner.ShowModal(frame);
        source.Focus();
    }

    /// <summary>Opens the search form modal (query + marketplace).</summary>
    internal static void OpenSearch(PluginManagerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var query = new TextBox();
        var marketplace = new ComboBox
        {
            PlaceholderText = Strings.PmMarketplaceAll,
            ItemsSource = new[] { Strings.PmMarketplaceAll },
            SelectedIndex = 0,
        };

        Border frame = PluginModal.Frame(ctx, Strings.PmSearchTitle, out StackPanel content);
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldQuery, query));
        content.Children.Add(PluginModal.LabeledField(Strings.PmFieldMarketplace, marketplace));
        content.Children.Add(new TextBlock
        {
            Text = Strings.PmSearchStaleHint,
            Foreground = PluginUi.Muted,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(PluginModal.ButtonRow(PluginModal.ActionButton(
            ctx,
            Strings.PmSearch,
            () => _ = RunSearchAsync(
                ctx, query.Text ?? string.Empty, marketplace.SelectedItem as string ?? string.Empty),
            primary: true,
            closeModal: false)));
        PluginModal.EscToHide(ctx, query, marketplace);
        ctx.Owner.ShowModal(frame);
        query.Focus();
        _ = PopulateMarketplacesAsync(ctx, marketplace);
    }

    private static async Task PopulateMarketplacesAsync(PluginManagerContext ctx, ComboBox marketplace)
    {
        if (ctx.Market is null)
            return;

        try
        {
            IReadOnlyList<MarketplaceInfo> markets = await ctx.Market.MarketplacesAsync().ConfigureAwait(true);
            marketplace.ItemsSource = new[] { Strings.PmMarketplaceAll }.Concat(markets.Select(m => m.Name)).ToArray();
            marketplace.SelectedIndex = 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ctx.Owner.SetStatus(ex.Message);
        }
    }

    private static async Task RunInstallAsync(PluginManagerContext ctx, string what, string version, PluginInstallOptions options)
    {
        if (ctx.Market is null)
        {
            ctx.Owner.SetStatus(Strings.PmNoMarket);
            return;
        }

        if (string.IsNullOrWhiteSpace(what))
        {
            ctx.Owner.SetStatus(Strings.PmInstallNeedsSource);
            return;
        }

        ctx.Owner.HideModal();
        ctx.Owner.SetStatus(Strings.PmWorking);
        try
        {
            PluginActionResult result = await ctx.RunMarketAsync(
                (market, ct) => market.InstallAsync(
                    what.Trim(), options, string.IsNullOrWhiteSpace(version) ? null : version.Trim(), false, ct))
                .ConfigureAwait(true);
            ctx.Owner.SetStatus(result.Message);
            ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ctx.Owner.SetStatus(ex.Message);
        }

        ctx.Owner.BackToList();
    }

    private static async Task RunSearchAsync(PluginManagerContext ctx, string query, string marketplace)
    {
        if (ctx.Market is null)
        {
            ctx.Owner.SetStatus(Strings.PmNoMarket);
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            ctx.Owner.SetStatus(Strings.PmSearchNeedsQuery);
            return;
        }

        ctx.Owner.HideModal();
        IReadOnlyList<MarketplaceSearchResult> rows;
        try
        {
            rows = await ctx.Market.SearchAsync(
                query.Trim(),
                string.IsNullOrWhiteSpace(marketplace)
                    || string.Equals(marketplace, Strings.PmMarketplaceAll, StringComparison.Ordinal)
                    ? null
                    : marketplace.Trim())
                .ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ctx.Owner.SetStatus(ex.Message);
            return;
        }

        ctx.Show(new PluginSearchResultsPage(ctx, query.Trim(), rows));
    }
}

/// <summary>
/// The market's install/update confirmation as a manager page: the already-rendered confirmation text plus what it pulls along, answered with Install or Cancel (Escape cancels).
/// </summary>
internal sealed class PluginConfirmPage : StackPanel, IOverlayPage
{
    private readonly PluginManagerContext _ctx;
    private readonly TaskCompletionSource<bool> _answer;
    private Button? _installButton;

    public PluginConfirmPage(
        PluginManagerContext ctx, InstallConfirmation confirmation, TaskCompletionSource<bool> answer)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(answer);
        _ctx = ctx;
        _answer = answer;
        // Single-spaced like the console verb that prints the same text: Spacing is in cells, so the old 4 meant four blank rows between every line (the absurd gaps).
        Spacing = 1;
        Focusable = true;

        Children.Add(PluginUi.PageHeader(
            Strings.PmConfirmInstallTitleFor(confirmation.Id),
            Strings.PmInstallSubtitle));

        string[] lines = confirmation.Text.Split('\n');
        int warningIndex = Array.FindLastIndex(lines, static line => !string.IsNullOrWhiteSpace(line));
        var disclosure = new StackPanel { Spacing = 0 };

        for (int index = 0; index < warningIndex; index++)
        {
            string line = lines[index].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (index == 0)
            {
                disclosure.Children.Add(new TextBlock
                {
                    Text = line,
                    Foreground = PluginUi.Gold,
                    FontWeight = FontWeight.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 1),
                });
                continue;
            }

            if (TrySplitMetadata(line, out string label, out string value))
            {
                disclosure.Children.Add(PluginUi.KeyValue(label, value));
                continue;
            }

            disclosure.Children.Add(new TextBlock
            {
                Text = line.Trim(),
                Foreground = PluginUi.White,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        Children.Add(PluginUi.Card(disclosure));

        if (warningIndex >= 0)
        {
            string warningPrefix = _ctx.Glyphs.IsEmoji ? $"{_ctx.Glyphs.Warn} " : string.Empty;
            var warning = new TextBlock
            {
                Text = warningPrefix + lines[warningIndex].Trim(),
                Foreground = PluginUi.Red,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
            };
            Children.Add(new Border
            {
                Background = PluginUi.Panel,
                BorderBrush = PluginUi.Red,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Child = warning,
            });
        }

        Button install = PluginUi.Button(Strings.PmInstall, () => { }, PluginButtonKind.Primary, isDefault: true);
        _installButton = install;
        install.Click += (_, _) =>
        {
            _answer.TrySetResult(true);
            ctx.Owner.Back();
        };
        Button cancel = PluginUi.Button(Strings.PmBack, () => { }, PluginButtonKind.Quiet);
        cancel.Click += (_, _) =>
        {
            _answer.TrySetResult(false);
            ctx.Owner.Back();
        };
        Children.Add(PluginUi.Actions(install, cancel));
    }

    private static bool TrySplitMetadata(string line, out string label, out string value)
    {
        string trimmed = line.TrimStart();
        int separator = trimmed.IndexOf(' ');
        if (separator <= 0)
        {
            label = string.Empty;
            value = string.Empty;
            return false;
        }

        label = trimmed[..separator];
        value = trimmed[separator..].TrimStart();
        return value.Length > 0;
    }

    /// <summary>Focuses the Install button once shown (pre-attach focus is a no-op).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_installButton is not null)
            _installButton.Focus();
        else
            Focus();
    }

    /// <summary>
    /// Escape declines (never bare Back: an unanswered confirmation would hang the market operation waiting on it).
    /// </summary>
    public bool OnEscapeKey()
    {
        _answer.TrySetResult(false);
        _ctx.Owner.Back();
        return true;
    }
}

/// <summary>Catalogue search results: per-row Install buttons feeding the confirm flow.</summary>
internal sealed class PluginSearchResultsPage : StackPanel
{
    private readonly PluginManagerContext _ctx;

    public PluginSearchResultsPage(
        PluginManagerContext ctx, string query, IReadOnlyList<MarketplaceSearchResult> rows)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(rows);
        _ctx = ctx;
        Spacing = 1;
        Focusable = true;

        Children.Add(PluginUi.PageHeader(Strings.PmSearchTitle, Strings.PmSearchSubtitle));
        _ctx.Owner.SetHints(Strings.PmSearchResultsHints);

        if (rows.Count == 0)
        {
            Children.Add(new TextBlock
            {
                Text = Strings.PmSearchNone(query),
                Foreground = PluginUi.Soft,
            });
            return;
        }

        foreach (MarketplaceSearchResult row in rows)
        {
            var details = new StackPanel { Spacing = 0 };
            details.Children.Add(new TextBlock
            {
                Text = $"{row.Id} · {row.Marketplace} · {row.Version}",
                Foreground = PluginUi.White,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
            });

            if (row.Description.Length > 0)
            {
                details.Children.Add(new TextBlock
                {
                    Text = row.Description,
                    Foreground = PluginUi.Muted,
                    TextWrapping = TextWrapping.Wrap,
                });
            }

            if (row.Installed is { Length: > 0 } installed)
            {
                details.Children.Add(new TextBlock
                {
                    Text = Strings.PmSearchInstalled(installed),
                    Foreground = PluginUi.Soft,
                });
            }

            var line = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            line.Children.Add(details);

            if (row.Incompatibility is { Length: > 0 })
            {
                details.Children.Add(new TextBlock
                {
                    Text = row.Incompatibility,
                    Foreground = PluginUi.Red,
                    TextWrapping = TextWrapping.Wrap,
                });
            }
            else
            {
                string what = $"{row.Id}@{row.Marketplace}";
                Button install = PluginUi.Button(
                    Strings.PmInstall,
                    () => _ = InstallAsync(what),
                    PluginButtonKind.Primary);
                install.HorizontalAlignment = HorizontalAlignment.Right;
                install.VerticalAlignment = VerticalAlignment.Center;
                install.Margin = new Thickness(2, 0, 0, 0);
                Grid.SetColumn(install, 1);
                line.Children.Add(install);
            }

            Children.Add(PluginUi.Card(line));
        }
    }

    private async Task InstallAsync(string what)
    {
        if (_ctx.Market is null)
            return;

        _ctx.Owner.SetStatus(Strings.PmWorking);
        try
        {
            PluginActionResult result = await _ctx.RunMarketAsync(
                (market, ct) => market.InstallAsync(what, null, false, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.Owner.SetStatus(ex.Message);
        }

        _ctx.Owner.BackToList();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }
}
