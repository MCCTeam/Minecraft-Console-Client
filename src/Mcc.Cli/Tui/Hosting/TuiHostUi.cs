using Mcc.Cli.Commands;
using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Overlays;
using Mcc.Cli.Tui.Presentation;
using Avalonia.Controls;
using DMCBK.Core;
using DMCBK.Core.Commands;

namespace Mcc.Cli.Tui.Hosting;

/// <summary>
/// The TUI host UI: the real <see cref="IHostUi"/> the command system consults instead of falling back to text.
/// It opens Consolonia overlays for containers, the book editor, dialogs and the tab list, and surfaces notifications.
/// Fed exclusively by <see cref="GameApi"/> (set after the client is built).
/// The hooks are invoked from command execution (a background thread), so each marshals its view work through the <see cref="TuiBackend"/> onto the UI thread.
/// </summary>
internal sealed class TuiHostUi : IHostUi, IMccNavigation
{
    public string? FormatCommandIndex(IReadOnlyList<CommandBase> commands, string prefix, DMCBK.Core.Presentation.GlyphSet glyphs)
        => Mcc.Cli.Presentation.TerminalHelpRenderer.Index(commands, prefix,
            new Mcc.Cli.Presentation.TerminalHelpRenderer.GlyphContext(glyphs.Ok, glyphs.Fail));

    public string? FormatCommandHelp(CommandHelpPresentation request, DMCBK.Core.Presentation.GlyphSet glyphs)
        => Mcc.Cli.Presentation.TerminalHelpRenderer.RenderPage(request, glyphs);

    public bool ShowInventoryLayout { get; set; } = true;

    public bool TryPresentChunkMap(ChunkMapPresentation request, DMCBK.Core.Presentation.GlyphSet glyphs)
    {
        foreach (string line in Mcc.Cli.Presentation.ChunkMapRenderer.Render(request, glyphs))
            _backend.WriteLine(line);
        return true;
    }

    public string? FormatPlayerStatus(DMCBK.Core.PlayerStatus status, DMCBK.Core.Presentation.GlyphSet glyphs)
        => Mcc.Cli.Presentation.PlayerStatusRenderer.Render(status, glyphs);

    public string? FormatProgress(double fraction, DMCBK.Core.Presentation.GlyphSet glyphs)
        => "`" + Mcc.Cli.Presentation.Meter.Render(fraction, 15, glyphs.IsEmoji) + "` " + Mcc.Cli.Presentation.Meter.Percent(fraction);

    public string? GetInventoryLayout(string? menuType, bool playerInventory)
        => !ShowInventoryLayout ? null : playerInventory ? Mcc.Cli.Localization.ContainerArt.PlayerInventory : Mcc.Cli.Localization.ContainerArt.ForMenuType(menuType);

    private readonly TuiBackend _backend;
    private readonly bool _tabListShowTeams;
    private Scoreboard.ScoreboardWindow? _scoreboardWindow;

    public TuiHostUi(TuiBackend backend, bool tabListShowTeams = false)
    {
        _backend = backend;
        _tabListShowTeams = tabListShowTeams;
    }

    /// <inheritdoc/>
    public bool TryPresentImage(ImagePresentationRequest request)
    {
        if (_backend.View is null) return false;
        _backend.Post(() =>
        {
            if (_backend.View is not { } view) return;
            var panel = new StackPanel();
            int scale = Math.Max(1, Math.Max((request.Image.Width + 79) / 80, (request.Image.Height + 59) / 60));
            for (int y = 0; y < request.Image.Height; y += scale * 2)
            {
                var row = new TextBlock();
                for (int x = 0; x < request.Image.Width; x += scale)
                {
                    RgbPixel top = request.Image.GetPixel(x, y);
                    RgbPixel bottom = request.Image.GetPixel(x, Math.Min(y + scale, request.Image.Height - 1));
                    row.Inlines!.Add(new Avalonia.Controls.Documents.Run("\u2580")
                    {
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(top.R, top.G, top.B)),
                        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(bottom.R, bottom.G, bottom.B)),
                    });
                }
                panel.Children.Add(row);
            }
            view.AppendControlLine(panel);
        });
        return true;
    }

    /// <summary>The live game facade; set once the client is built (before any hook can fire).</summary>
    public GameApi? Game { get; set; }

    /// <summary>The live client (plugin host, market, commands); set once it is built.</summary>
    public Client? Client { get; set; }

    /// <summary>
    /// The Markdown renderer for <c>/man</c>.
    /// Set during TUI startup, once the glyph set is resolved.
    /// </summary>
    public MarkdownTuiRenderer? Documents { get; set; }

    /// <inheritdoc/>
    public bool TryOpenPluginManager()
        => OpenPluginManager(Plugins.PluginManagerStartPage.Installed);

    private bool OpenPluginManager(Plugins.PluginManagerStartPage startPage)
    {
        if (_backend.View is null || Client is null)
            return false;

        // Built on the UI thread like every other overlay: Avalonia objects have thread affinity and this hook fires from command execution on a background thread.
        _backend.Post(() =>
        {
            if (_backend.View is { } view && Client is { } client)
                view.ShowOverlay(new Plugins.PluginManagerOverlay(view, _backend, client, startPage));
        });

        return true;
    }

    /// <inheritdoc/>
    public bool TryOpenMccMenu()
    {
        if (_backend.View is null || Client is null)
            return false;

        _backend.Post(() =>
        {
            if (_backend.View is not { } view || Client is not { } client)
                return;

            var workspace = new Management.ManagementWorkspace(
                view, Strings.MccMenuTitle, Strings.MccMenuSubtitle);
            Action Open(string command) => () => RunMenuCommand(workspace, client, command);
            Action openServers = () => OpenServerPicker(workspace, client);
            Action exitClient = () =>
            {
                workspace.Close();
                client.RequestShutdown();
            };
            workspace.SetRoot(new Management.MccMenuPage(
                new Management.MccMenuActions(
                    Open("help ui"),
                    Open("scripts ui"),
                    Open("recipebook ui"),
                    Open("entity ui"),
                    Open("advancements ui"),
                    Open("chunk ui"),
                    Open("inventory player open"),
                    Open("minimap on"),
                    Open("tab"),
                    Open("scoreboard ui"),
                    Open("plugins ui"),
                    () => OpenPluginManager(Plugins.PluginManagerStartPage.Marketplaces),
                    openServers,
                    exitClient)));
            view.ShowOverlay(workspace, workspace.Dispose);
        });

        return true;
    }

    private void OpenServerPicker(Management.ManagementWorkspace workspace, Client client)
    {
        DMCBK.Core.Configuration.DmcbkConfiguration? config = client.Commands.CurrentConfiguration ?? client.Configuration;
        if (config is null)
        {
            workspace.SetStatus(Strings.MccMenuOpenFailed(Strings.TuiConnectNoConfig));
            return;
        }

        _ = Task.Run(async () =>
        {
            bool exitClient = await TuiHost
                .ShowConnectDialogAsync(client, _backend, config, CancellationToken.None)
                .ConfigureAwait(false);
            if (exitClient)
                client.RequestShutdown();
        });
    }

    private void RunMenuCommand(
        Management.ManagementWorkspace workspace,
        Client client,
        string command)
    {
        _ = Task.Run(async () =>
        {
            CmdResult result;
            try
            {
                result = await client.Commands.DispatchAsync(command, workspace.Closed).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _backend.Post(() =>
                {
                    if (_backend.View?.CurrentOverlay == workspace)
                        workspace.SetStatus(Strings.MccMenuOpenFailed(ex.Message));
                });
                return;
            }

            if (result.Status == CmdStatus.Done || result.Message is not { Length: > 0 } message)
                return;

            _backend.Post(() =>
            {
                if (_backend.View?.CurrentOverlay == workspace)
                    workspace.SetStatus(message);
            });
        });
    }

    /// <inheritdoc/>
    public bool TryOpenCommandBrowser(CommandBrowserTab initialTab)
    {
        if (_backend.View is null || Client is null || Documents is null)
            return false;

        _backend.Post(() =>
        {
            if (_backend.View is not { } view || Client is not { } client || Documents is not { } renderer)
                return;

            var workspace = new Management.ManagementWorkspace(
                view, Strings.BrowserTitle, Strings.BrowserSubtitle);
            workspace.SetRoot(new Management.CommandManualBrowserPage(
                workspace, view, client, renderer, initialTab));
            view.ShowOverlay(workspace, workspace.Dispose);
        });

        return true;
    }

    /// <inheritdoc/>
    public bool TryOpenScriptManager()
    {
        if (_backend.View is null || Client is null)
            return false;

        _backend.Post(() =>
        {
            if (_backend.View is not { } view || Client is not { } client)
                return;

            var workspace = new Management.ManagementWorkspace(
                view, Strings.ScriptsUiTitle, Strings.ScriptsUiSubtitle);
            workspace.SetRoot(new Management.ScriptManagerPage(workspace, client));
            view.ShowOverlay(workspace, workspace.Dispose);
        });

        return true;
    }

    /// <inheritdoc/>
    public bool TryOpenRecipeBrowser()
        => OpenManagementBrowser(
            Strings.RecipeUiTitle,
            Strings.RecipeUiSubtitle,
            static (workspace, client) => new Management.RecipeBrowserPage(workspace, client));

    /// <inheritdoc/>
    public bool TryOpenEntityBrowser()
        => OpenManagementBrowser(
            Strings.EntityUiTitle,
            Strings.EntityUiSubtitle,
            static (workspace, client) => new Management.EntityBrowserPage(workspace, client));

    /// <inheritdoc/>
    public bool TryOpenAdvancementsBrowser()
        => OpenManagementBrowser(
            Strings.AdvancementUiTitle,
            Strings.AdvancementUiSubtitle,
            static (workspace, client) => new Management.AdvancementBrowserPage(workspace, client));

    /// <inheritdoc/>
    public bool TryOpenChunkBrowser()
        => OpenManagementBrowser(
            Strings.ChunkUiTitle,
            Strings.ChunkUiSubtitle,
            static (workspace, client) => new Management.ChunkBrowserPage(workspace, client));

    private bool OpenManagementBrowser(
        string title,
        string subtitle,
        Func<Management.ManagementWorkspace, Client, Control> pageFactory)
    {
        if (_backend.View is null || Client is null)
            return false;

        _backend.Post(() =>
        {
            if (_backend.View is not { } view || Client is not { } client)
                return;

            var workspace = new Management.ManagementWorkspace(view, title, subtitle);
            workspace.SetRoot(pageFactory(workspace, client));
            view.ShowOverlay(workspace, workspace.Dispose);
        });
        return true;
    }

    /// <inheritdoc/>
    public bool TryWriteDocument(string markdown)
    {
        if (Documents is not { } renderer || string.IsNullOrEmpty(markdown) || _backend.View is null)
            return false;

        // Built AND appended on the UI thread.
        // Avalonia objects have thread affinity, and a command runs on a background thread, so building the controls there threw a TypeInitializationException out of the renderer's brush table the first time /man ran in the TUI.
        // ServerStatusPanel is built inside its Post for the same reason; this follows it.
        _backend.Post(() =>
        {
            if (_backend.View is not { } view)
                return;

            foreach (Control control in renderer.Render(markdown))
                view.AppendControlLine(control);
        });

        return true;
    }

    /// <inheritdoc/>
    public bool TryOpenContainerView(int windowId)
    {
        MainTuiView? view = _backend.View;
        GameApi? game = Game;
        if (view is null || game is null)
            return false;

        _backend.Post(() => Container.ContainerOverlay.Open(
            view, game, _backend, windowId, () => TryOpenRecipeBrowser()));
        return true;
    }

    /// <inheritdoc/>
    public void CloseContainerView(int? windowId = null)
    {
        MainTuiView? view = _backend.View;
        if (view is null)
            return;

        _backend.Post(() =>
        {
            // Only a container overlay, and only the one being asked about: a book editor or a dialog the user has open is not this call's business, and neither is a container view of some other window (a host can only show one at a time today, but nothing here relies on that).
            if (view.CurrentOverlay is Container.ContainerOverlay container
                && (windowId is null || container.WindowId == windowId))
            {
                container.MarkWindowGone();
                view.HideOverlay();
            }
        });
    }

    /// <inheritdoc/>
    public bool TryOpenBookEditor(BookEditorRequest request)
    {
        MainTuiView? view = _backend.View;
        GameApi? game = Game;
        if (view is null || game is null)
            return false;

        _backend.Post(() => BookOverlay.Open(view, game, _backend, request));
        return true;
    }

    /// <inheritdoc/>
    public bool TryShowTabOverlay()
    {
        MainTuiView? view = _backend.View;
        GameApi? game = Game;
        if (view is null || game is null)
            return false;

        _backend.Post(() => TabListOverlay.Open(view, game, _backend, _tabListShowTeams));
        return true;
    }

    /// <inheritdoc/>
    public bool TryOpenScoreboard()
    {
        MainTuiView? view = _backend.View;
        GameApi? game = Game;
        if (view is null || game is null)
            return false;

        _backend.Post(() =>
        {
            if (_backend.View is not { } liveView || Game is not { } liveGame || _scoreboardWindow is not null)
                return;

            var window = new Scoreboard.ScoreboardWindow(liveGame, liveView.WindowSurface);
            _scoreboardWindow = window;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_scoreboardWindow, window))
                    _scoreboardWindow = null;
            };
            liveView.ShowWindow(window);
        });
        return true;
    }

    /// <inheritdoc/>
    public bool TryShowDialog(DialogViewRequest request)
    {
        MainTuiView? view = _backend.View;
        GameApi? game = Game;
        if (view is null || game is null || request is null)
            return false;

        _backend.Post(() => DialogOverlay.Open(view, game, _backend, request));
        return true;
    }

    /// <inheritdoc/>
    public bool TryNotify(HostNotification notification)
    {
        if (notification is null)
            return false;

        if (notification.Beep)
        {
            try
            {
                Console.Beep();
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException or IOException)
            {
                // Not every platform supports Beep; the written line is the fallback.
            }
        }

        _backend.WriteLine(notification.Message);
        return true;
    }
}
