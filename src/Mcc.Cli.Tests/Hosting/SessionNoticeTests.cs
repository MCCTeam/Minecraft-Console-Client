using DMCBK.Core;
using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core.Commands;
using DMCBK.Core.Configuration;
using Umpk.Auth;
using Xunit;

namespace Mcc.Cli.Tests.Hosting;

/// <summary>
/// The unsolicited container notices, which the new client did not have at all: opening a chest, or clicking a compass that opens a server-selector menu, produced no output whatsoever, and in the TUI no view either.
/// The legacy client announced every server-opened window, named the command that operates on it, and opened its TUI view (<c>McClient.OnInventoryOpen</c>, McClient.cs:3856-3874), then announced the close and dismissed that view (<c>OnInventoryClose</c>, :3880-3903).
/// </summary>
public sealed class SessionNoticeTests
{
    private sealed class RecordingUi : IHostUi
    {
        public List<int> Opened { get; } = [];

        public List<int?> Closed { get; } = [];

        public bool TryOpenContainerView(int windowId)
        {
            Opened.Add(windowId);
            return true;
        }

        public void CloseContainerView(int? windowId = null) => Closed.Add(windowId);
    }

    private sealed class UiOnlyHost(IHostUi ui) : IHostInterface
    {
        public IUserPrompt? Prompt => null;

        public IAuthInteraction? AuthInteraction => null;

        public IHostUi? Ui { get; } = ui;
    }

    private static Client Build(RecordingUi ui) => new ClientBuilder().UseCommands().UseBeacon()
        .UseUsername("Tester")
        .UseServer("localhost")
        .UseHostInterface(new UiOnlyHost(ui))
        .Build();

    // The effect announcer is a separate notice with its own polling loop; off here so these tests are about the container notices alone.
    private static DmcbkConfiguration QuietConfig() => new()
    {
        Gameplay = new GameplayConfig { ShowEffectMessages = false },
    };

    [Fact]
    public async Task ContainerOpen_AnnouncesTheWindow_AndOpensTheHostView()
    {
        var ui = new RecordingUi();
        await using Client client = Build(ui);
        var lines = new List<string>();
        await using SessionNotices notices = SessionNotices.Attach(client, QuietConfig(), lines.Add);

        client.Game.Inventory.OnOpened(new Umpk.Client.Events.ContainerOpened(1));

        // Legacy's two lines, in legacy's order: what opened, then what to do about it.
        Assert.Equal(2, lines.Count);
        Assert.StartsWith("Inventory # 1 opened:", lines[0], StringComparison.Ordinal);
        Assert.Equal("Use /inventory to interact with it.", lines[1]);
        Assert.Equal([1], ui.Opened);
    }

    [Fact]
    public async Task ContainerClose_AnnouncesTheWindow_AndDismissesThatViewOnly()
    {
        var ui = new RecordingUi();
        await using Client client = Build(ui);
        var lines = new List<string>();
        await using SessionNotices notices = SessionNotices.Attach(client, QuietConfig(), lines.Add);

        client.Game.Inventory.OnClosed(new Umpk.Client.Events.ContainerClosed(3));

        Assert.Equal(["Inventory # 3 closed."], lines);

        // The window id, not null: a close names one window, and a host showing some other one must keep it.
        Assert.Equal([(int?)3], ui.Closed);
    }

    /// <summary>
    /// Window 0 is the player's own inventory.
    /// The server never opens it, and legacy filtered it out of this announcement explicitly (<c>if (inventoryID != 0)</c>); announcing it would put a line on screen for something that was never an event.
    /// </summary>
    [Fact]
    public async Task PlayerWindow_IsNeverAnnounced()
    {
        var ui = new RecordingUi();
        await using Client client = Build(ui);
        var lines = new List<string>();
        await using SessionNotices notices = SessionNotices.Attach(client, QuietConfig(), lines.Add);

        client.Game.Inventory.OnOpened(new Umpk.Client.Events.ContainerOpened(0));
        client.Game.Inventory.OnClosed(new Umpk.Client.Events.ContainerClosed(0));

        Assert.Empty(lines);
        Assert.Empty(ui.Opened);
        Assert.Empty(ui.Closed);
    }

    /// <summary>
    /// A server switch closes EVERY container view (null, not an id): the window belongs to the server being left, and nothing on the next one answers for it.
    /// </summary>
    [Fact]
    public async Task ServerSwitch_IsAnnounced_AndDropsEveryContainerView()
    {
        var ui = new RecordingUi();
        await using Client client = Build(ui);
        var lines = new List<string>();
        await using SessionNotices notices = SessionNotices.Attach(client, QuietConfig(), lines.Add);

        notices.AnnounceServerSwitch();

        Assert.Equal(["Switching servers..."], lines);
        Assert.Equal([(int?)null], ui.Closed);
    }

    /// <summary>
    /// Disposing unsubscribes: a host that has torn its notices down must not keep printing, and the subscriptions live on a facade that outlives any one session.
    /// </summary>
    [Fact]
    public async Task Dispose_StopsAnnouncing()
    {
        var ui = new RecordingUi();
        await using Client client = Build(ui);
        var lines = new List<string>();

        SessionNotices notices = SessionNotices.Attach(client, QuietConfig(), lines.Add);
        await notices.DisposeAsync();

        client.Game.Inventory.OnOpened(new Umpk.Client.Events.ContainerOpened(1));

        Assert.Empty(lines);
        Assert.Empty(ui.Opened);
    }
}
