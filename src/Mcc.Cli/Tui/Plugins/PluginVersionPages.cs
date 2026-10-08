using Avalonia.Controls;
using DMCBK.Core.Plugins;
using Mcc.Cli.Localization;

namespace Mcc.Cli.Tui.Plugins;

internal sealed class PluginVersionsPage : StackPanel
{
    private readonly PluginManagerContext _ctx;
    private readonly string _id;
    private readonly CheckBox _source = new() { Content = MccStrings.Get("tui.plugins.source") };
    private readonly CheckBox _fallback = new() { Content = MccStrings.Get("tui.plugins.source_fallback") };
    private readonly CheckBox _prerelease = new() { Content = MccStrings.Get("tui.plugins.prerelease") };
    private readonly StackPanel _releases = new() { Spacing = 1 };

    public PluginVersionsPage(PluginManagerContext ctx, string id)
    {
        _ctx = ctx;
        _id = id;
        Spacing = 1;
        Children.Add(PluginUi.PageHeader(id, MccStrings.Get("tui.plugins.versions")));
        Children.Add(new TextBlock { Text = MccStrings.Get("tui.plugins.version_help"), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        Children.Add(_source);
        Children.Add(_fallback);
        Children.Add(_prerelease);
        Children.Add(_releases);
        _prerelease.IsCheckedChanged += (_, _) => _ = LoadAsync();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _releases.Children.Clear();
        if (_ctx.Market is not { } market) return;
        try
        {
            IReadOnlyList<PluginReleaseInfo> releases = await market.VersionsAsync(_id).ConfigureAwait(true);
            foreach (PluginReleaseInfo release in releases)
            {
                if (!AllowsVersion(release.Version, _prerelease.IsChecked == true)) continue;
                var details = new StackPanel { Spacing = 0 };
                details.Children.Add(new TextBlock { Text = release.Version + " · " + release.Marketplace });
                details.Children.Add(new TextBlock { Text = string.Join(", ", release.Assets.Select(asset => asset.Kind + " " + asset.Target)) });
                if (release.Yanked) details.Children.Add(new TextBlock { Text = MccStrings.Get("tui.plugins.yanked"), Foreground = PluginUi.Red });
                if (release.Incompatibility is { } reason) details.Children.Add(new TextBlock { Text = reason, Foreground = PluginUi.Red });
                Button install = PluginUi.Button(Strings.PmInstall, () => _ = InstallAsync(release), PluginButtonKind.Primary);
                install.IsEnabled = !release.Yanked && release.Incompatibility is null;
                details.Children.Add(install);
                _releases.Children.Add(PluginUi.Card(details));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { _ctx.Owner.SetStatus(exception.Message); }
    }

    internal static bool AllowsVersion(string version, bool includePrerelease)
        => includePrerelease || !version.Split('+')[0].Contains('-', StringComparison.Ordinal);

    private async Task InstallAsync(PluginReleaseInfo release)
    {
        try
        {
            var options = new PluginInstallOptions(_source.IsChecked == true, _fallback.IsChecked == true, _prerelease.IsChecked == true);
            PluginActionResult result = await _ctx.RunMarketAsync((market, ct) =>
                market.InstallAsync(_id + "@" + release.Marketplace, options, release.Version, false, ct)).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
            _ctx.Owner.BackToList();
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { _ctx.Owner.SetStatus(exception.Message); }
    }
}

internal sealed class PluginRollbackPage : StackPanel
{
    private readonly PluginManagerContext _ctx;
    public PluginRollbackPage(PluginManagerContext ctx)
    {
        _ctx = ctx;
        Spacing = 1;
        Children.Add(PluginUi.PageHeader(MccStrings.Get("tui.plugins.rollback"), MccStrings.Get("tui.plugins.rollback_help")));
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_ctx.Market is not { } market) return;
        try
        {
            IReadOnlyList<PluginRollbackInfo> history = await market.RollbackHistoryAsync().ConfigureAwait(true);
            if (history.Count == 0) Children.Add(new TextBlock { Text = MccStrings.Get("tui.plugins.rollback_empty") });
            foreach (PluginRollbackInfo transaction in history)
            {
                var details = new StackPanel { Spacing = 0 };
                details.Children.Add(new TextBlock { Text = transaction.CommittedAt.ToString("u") + " · " + transaction.TransactionId });
                details.Children.Add(new TextBlock
                {
                    Text = transaction.PreviousVersions.Count == 0
                    ? MccStrings.Get("tui.plugins.empty_graph") : string.Join(", ", transaction.PreviousVersions),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });
                details.Children.Add(PluginUi.Button(MccStrings.Get("tui.plugins.rollback"), () => _ = RestoreAsync(transaction.TransactionId), PluginButtonKind.Primary));
                Children.Add(PluginUi.Card(details));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { _ctx.Owner.SetStatus(exception.Message); }
    }

    private async Task RestoreAsync(string transactionId)
    {
        if (_ctx.Market is not { } market) return;
        try
        {
            PluginActionResult result = await market.RollbackAsync(transactionId).ConfigureAwait(true);
            _ctx.Owner.SetStatus(result.Message);
            _ctx.Log(result.Message);
            _ctx.Owner.BackToList();
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { _ctx.Owner.SetStatus(exception.Message); }
    }
}
