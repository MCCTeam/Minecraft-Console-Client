using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using DMCBK.Core;
using DMCBK.Core.Plugins;
using DMCBK.Core.Presentation;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>
/// The services every plugin-manager page shares: the backend (UI thread posts, log lines), the live client (plugin host, market, commands, glyphs) and navigation on the owning overlay.
/// One instance per open manager; pages never reach past it.
/// </summary>
internal sealed class PluginManagerContext
{
    public PluginManagerContext(PluginManagerOverlay owner, MainTuiView view, TuiBackend backend, Client client)
    {
        Owner = owner;
        View = view;
        Backend = backend;
        Client = client;
    }

    /// <summary>The owning overlay (page stack).</summary>
    public PluginManagerOverlay Owner { get; }

    public MainTuiView View { get; }

    public TuiBackend Backend { get; }

    public Client Client { get; }

    /// <summary>The plugin host, or null when this host attached none (an embedding without plugins).</summary>
    public IPluginHost? Host => Client.PluginHost;

    /// <summary>The plugin market, or null when this host attached none.</summary>
    public IPluginMarket? Market => Client.PluginMarket;

    /// <summary>The host-resolved status glyphs (checkboxes included).</summary>
    public GlyphSet Glyphs => Client.Commands.Glyphs;

    /// <summary>Shows a page on the overlay stack.</summary>
    public void Show(Avalonia.Controls.Control page) => Owner.Show(page);

    /// <summary>Returns to the previous page, or closes the manager at the root.</summary>
    public void Back() => Owner.RequestBack();

    /// <summary>Closes the whole manager.</summary>
    public void Close() => View.HideOverlay();

    /// <summary>Writes a result line to the TUI log (the transcript outlives the overlay).</summary>
    public void Log(string line) => Backend.WriteLine(line);

    /// <summary>
    /// Runs a market operation with the overlay's install-confirm page answering its <see cref="IPluginMarket.Confirm"/> calls, restoring the host's confirmer afterwards.
    /// Without the swap the host's input-line confirmer would wait on an input line hidden behind this fullscreen overlay, hanging the operation with no way to answer.
    /// </summary>
    public async Task<PluginActionResult> RunMarketAsync(
        Func<IPluginMarket, CancellationToken, Task<PluginActionResult>> op, CancellationToken ct = default)
    {
        if (Market is not { } market)
            throw new InvalidOperationException(Strings.PmNoMarket);

        var saved = market.Confirm;
        market.Confirm = DialogConfirmAsync;
        try
        {
            return await op(market, ct).ConfigureAwait(false);
        }
        finally
        {
            market.Confirm = saved;
        }
    }

    private async ValueTask<bool> DialogConfirmAsync(InstallConfirmation confirmation, CancellationToken ct)
    {
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // The market may call from any thread; overlay work always marshals to the UI thread.
        Backend.Post(() => Owner.Show(new PluginConfirmPage(this, confirmation, answer)));

        using CancellationTokenRegistration registration = ct.Register(() => answer.TrySetCanceled(ct));
        return await answer.Task.ConfigureAwait(false);
    }

    /// <summary>The settings file a plugin's editor reads and writes.</summary>
    public string SettingsPath(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return Path.Combine(Host!.PluginsRoot, "userdata", id.ToLowerInvariant(), "settings.toml");
    }
}
