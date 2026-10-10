using Mcc.Cli.Tui.Hosting;
using DMCBK.Core;
using DMCBK.Core.Configuration;
using DMCBK.Core.Localization;

namespace Mcc.Cli.Presentation;

/// <summary>
/// The unsolicited session notices both CLI hosts print: status effects gained and lost, and container windows the server opens and closes.
/// Nothing here is asked for by a command; it is the running commentary the legacy client kept up while you played.
/// </summary>
/// <remarks>
/// One class because there are two hosts and the wiring kept landing in only one of them.
/// The effect announcer was attached in <c>Program.RunClientAsync</c> under a comment claiming both hosts ran through it; they do not - TUI mode returns from <c>TuiHost.RunAsync</c> well before that - so the TUI silently had no effect notifications at all.
/// Everything either host wants unprompted goes through <see cref="Attach"/>, and the only host-specific part left is the sink it writes to.
/// </remarks>
internal sealed class SessionNotices : IAsyncDisposable
{
    /// <summary>Vanilla's own inventory, which the server never opens and legacy never announced.</summary>
    private const int PlayerWindowId = 0;

    private readonly Client _client;
    private readonly Action<string> _writeLine;
    private readonly EffectAnnouncer? _effects;
    private readonly EventHandler<ContainerOpenedEventArgs> _onContainerOpened;
    private readonly EventHandler<ContainerClosedEventArgs> _onContainerClosed;

    private SessionNotices(Client client, EffectAnnouncer? effects, Action<string> writeLine)
    {
        _client = client;
        _writeLine = writeLine;
        _effects = effects;

        // Legacy McClient.OnInventoryOpen (McClient.cs:3856-3874): announce the window, say which command operates on it, and let a rich host show it.
        // None of this existed in the new client, so opening a chest - or clicking a compass that opens a server-selector menu - produced no output at all.
        _onContainerOpened = (_, e) =>
        {
            if (e.WindowId == PlayerWindowId)
                return;

            _writeLine(Mcc.Cli.Localization.MccStrings.Format("extra.inventory_open", e.WindowId, e.Title));
            _writeLine(Mcc.Cli.Localization.MccStrings.Get("extra.inventory_interact"));

            // False on the classic host, which has no view to open: there the two lines above are the whole answer, exactly as they were the legacy classic console's.
            // In the TUI this is legacy's InventoryTuiHost.Launch.
            _client.HostUi?.TryOpenContainerView(e.WindowId);
        };

        // Legacy McClient.OnInventoryClose (McClient.cs:3880-3903), including its call into the TUI so a view of a window the server just closed does not linger.
        _onContainerClosed = (_, e) =>
        {
            if (e.WindowId == PlayerWindowId)
                return;

            _writeLine(Mcc.Cli.Localization.MccStrings.Format("extra.inventory_close", e.WindowId));
            _client.HostUi?.CloseContainerView(e.WindowId);
        };

        _client.Game.Inventory.ContainerOpened += _onContainerOpened;
        _client.Game.Inventory.ContainerClosed += _onContainerClosed;
    }

    /// <summary>
    /// Attaches every notice to <paramref name="client"/>, writing through <paramref name="writeLine"/>.
    /// Call once per host, before the first connect, and dispose with the host.
    /// </summary>
    public static SessionNotices Attach(Client client, DmcbkConfiguration config, Action<string> writeLine)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(writeLine);

        // Gated by the same setting legacy used (Advanced.ShowEffectMessages).
        EffectAnnouncer? effects = config.Gameplay.ShowEffectMessages
            ? new EffectAnnouncer(client.Game, client.Translations, writeLine)
            : null;
        effects?.Start();

        return new SessionNotices(client, effects, writeLine);
    }

    /// <summary>
    /// Reports that the live session is being handed to another server, and drops any container view that belongs to the server being left.
    /// </summary>
    /// <remarks>
    /// A 1.20.2+ server re-enters the configuration phase to move a client somewhere else, which is how a proxy's "send me to survival" button works.
    /// Vanilla shows a screen for it; a headless client that says nothing makes a switch that IS happening look like a button that did nothing.
    /// </remarks>
    public void AnnounceServerSwitch()
    {
        _writeLine(Mcc.Cli.Localization.MccStrings.Get("mcc.switching_servers"));

        // The open window belongs to the server being left; nothing on the next one answers for it.
        _client.HostUi?.CloseContainerView();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _client.Game.Inventory.ContainerOpened -= _onContainerOpened;
        _client.Game.Inventory.ContainerClosed -= _onContainerClosed;

        if (_effects is not null)
            await _effects.DisposeAsync().ConfigureAwait(false);
    }
}
