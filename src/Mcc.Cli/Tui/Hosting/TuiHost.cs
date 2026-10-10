using ServerStatusPanel = Mcc.Cli.Tui.Presentation.ServerStatusPanel;
using Mcc.Cli.Commands;
using Mcc.Cli.Hosting;
using Mcc.Cli.Input;
using Mcc.Cli.Localization;
using Mcc.Cli.Logging;
using Mcc.Cli.Startup;
using Mcc.Cli.Tui.Authentication;
using Mcc.Cli.Tui.Commands;
using Mcc.Cli.Tui.Input;
using Mcc.Cli.Tui.Presentation;
using Mcc.Cli.Presentation;
using Mcc.Cli.Configuration;
using Mcc.Cli.Tui.Map;
using Mcc.Cli.Tui.Minimap;
using DMCBK.Core;
using DMCBK.Core.Commands;
using DMCBK.Core.Configuration;
using DMCBK.Core.Presentation;
using Microsoft.Extensions.Logging.Abstractions;
using Umpk.Client;
using Umpk.Client.Chat;

namespace Mcc.Cli.Tui.Hosting;

/// <summary>
/// The TUI backend's run loop: the Consolonia analogue of the classic <c>CliHost.RunClientAsync</c> flow.
/// It brings up the TUI, wires chat/status/suggestions/input to the client, starts the session, and drives input from either the TUI input line or the file-input tail.
/// The host contract is preserved: exit codes 0-4, the "Server was successfully joined" line (surfaced in the TUI log), and the file-input drive.
/// All game data comes from <see cref="GameApi"/>; no console coupling leaks in.
/// </summary>
internal static class TuiHost
{
    private const int ExitClean = HostExit.Clean;
    private const int ExitConnectionLost = HostExit.ConnectionLost;
    private const int ExitLoginRejected = HostExit.LoginRejected;
    private const int ExitVersionResolution = HostExit.VersionResolution;

    public static async Task<int> RunAsync(
        Client client,
        TuiBackend backend,
        TuiHostUi hostUi,
        ConsoleHostConfig console,
        DmcbkConfiguration config,
        HostControl control,
        CancellationToken ct)
    {
        // The same capability the classic host folds (console.toml ConsoleColorMode, NO_COLOR, MCC_FORCE_COLOR).
        // The TUI used to assume colour unconditionally, so both switches were silently ignored by the very host the setting that selects the TUI lives next to.
        bool color = TerminalCapability.ResolveColor(console.ColorEnabled);

        // The backend is already up: Program starts it before the welcome block so startup itself renders inside the TUI.
        // Only the renderer (which needs the built client's translations) is wired here.
        backend.UseRenderer(client.Translations, color);

        hostUi.Game = client.Game;
        hostUi.Client = client;

        var state = new ConsoleHostState(console.DisplayChat);
        var minimap = new MinimapController(backend, console);
        var mapController = new MapController(backend);
        client.Commands.RegisterHostCommand(new TuiClearCommand(backend));
        client.Commands.RegisterHostCommand(new ConsoleChatCommand(state));
        client.Commands.RegisterHostCommand(new ExitCommand(control));
        client.Commands.RegisterHostCommand(new MinimapCommand(minimap));
        client.Commands.RegisterHostCommand(new MapCommand(mapController, client.Game));

        // The TUI paints its own cells, so it always has the glyph coverage the font gives it; "auto" resolves the same way as for the classic host and an explicit setting still wins.
        GlyphSet glyphs = GlyphModeResolver.Resolve(console.GlyphMode);
        client.Commands.Glyphs = glyphs;
        client.Commands.EchoCommands = console.EchoCommands;

        // The TUI renders /man into Consolonia controls, not ANSI: it paints its own cells and would show escape sequences as literal characters.
        // Same Markdown, different back end.
        hostUi.Documents = new MarkdownTuiRenderer(glyphs);

        var chatFilter = new LogFilter(config.Logging.ChatFilterRegex, config.Logging.FilterMode);

        // With colour the standing marker is the coloured bar; without it the marker degrades to the word tags the classic host already uses, because a grey bar on a monochrome pane says nothing about WHICH standing it is, which is the only reason the marker exists.
        var standing = new ChatStandingMarker(config.Chat.Signature, color);
        client.Game.Chat.MessageReceived += (_, message) =>
        {
            if (!state.ChatVisible || standing.ShouldHide(message))
                return;

            // Filter on the message text only: the marker is presentation and must not affect a user regex.
            string plain = message.Message.ToPlainText();
            if (!chatFilter.ShouldShow(plain))
                return;

            ChatStandingMark? mark = standing.Mark(message);
            backend.PostComponent(
                message.Message,
                mark?.Text ?? string.Empty,
                mark is { } m && color ? TuiStandingBrush(m.Kind) : null);
        };

        // the server-status/MOTD panel, appended to the scrollback on every connect attempt (subscribed before StartAsync so the ping this raises from, whichever path it takes, is never missed).
        client.ServerStatusReceived += (_, status) =>
            backend.Post(() => backend.View?.AppendControlLine(ServerStatusPanel.Build(status, backend.Renderer)));

        // Effect and container notices, the same set the classic host attaches.
        // This host used to attach none of them: the effect announcer was wired in Program.RunClientAsync, which TUI mode never reaches, and the container notices did not exist anywhere.
        await using SessionNotices notices = SessionNotices.Attach(client, config, backend.WriteLine);

        var remoteStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // set when a Reconnecting transition was announced, cleared (and "Reconnected." printed) at the next Playing transition.
        // Playing's immediate Previous is never actually Reconnecting (the supervisor passes through Connecting, and possibly Authenticating, in between - UmpkClientSupervisor.SuperviseAsync/RunOneAttemptAsync), so this flag is what actually detects "we are mid reconnect" across those intermediate statuses.
        bool reconnectPending = false;

        client.StatusChanged += (_, e) =>
        {
            HostExit.ObserveStatus(control, e);

            if (e.Current == ClientStatus.Playing)
            {
                if (reconnectPending)
                {
                    reconnectPending = false;
                    backend.WriteLine(Strings.Reconnected);
                }

                backend.WriteLine(Strings.ServerJoined);
                backend.Post(() =>
                {
                    MainTuiView? view = backend.View;
                    if (view is not null)
                    {
                        // Real translations (so effect names resolve from the vanilla tables) rather than the placeholder the view starts with.
                        view.Bind(client.Game, client.Translations, console.ShowEffectNamesInTui);
                        minimap.Attach(view, client.Game);
                        mapController.Attach(client.Game);
                    }
                });
            }
            // quiet for the initial connect (Previous is Created/Connecting); only a transition away from a session that was actually live (or had just ended) is a reconnect worth announcing.
            else if (e.Current == ClientStatus.Reconnecting
                && e.Previous is not (ClientStatus.Created or ClientStatus.Connecting))
            {
                reconnectPending = true;
                backend.WriteLine(Strings.Reconnecting);
            }
            // A live session entering configuration is the server handing this client to another server; see SessionNotices.AnnounceServerSwitch.
            else if (e.Current == ClientStatus.Configuring && e.Previous == ClientStatus.Playing)
                notices.AnnounceServerSwitch();
            else if (e.Current == ClientStatus.Disconnected && e.Disconnect is { WasLocal: false })
                remoteStop.TrySetResult();
        };

        MainTuiView? boundView = backend.View;
        if (boundView is not null)
        {
            boundView.InputChanged += (text, caret) => _ = RefreshSuggestionsAsync(client, backend, text, caret);
            boundView.MainMenuRequested += () => hostUi.TryOpenMccMenu();
        }

        var exitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // Off the UI thread: commands block sync-over-async at the command boundary (CommandContext.Run), which is harmless on a pool thread (the classic host runs commands on its reader thread the same way) but freezes the whole pane here.
        // A frozen UI thread cannot render anything posted to it (including the auth dialogs a connect was supposed to open), cannot take input, and cannot even see Ctrl+C: exactly the /connect hang, where offline froze for the second the handshake takes and Microsoft froze for the whole device-flow poll.
        backend.LineSubmitted += line => _ = Task.Run(async () =>
        {
            await ProcessLineAsync(client, backend, line, ct).ConfigureAwait(false);
            if (control.ExitRequested)
                exitTcs.TrySetResult();
        });

        // An in-process shutdown request (the MCP quit tool) ends the run without waiting for the next submitted line: flag the shared exit signal and trip the same completion the exit command trips, so HostExit.Resolve answers identically.
        _ = client.ShutdownRequested.ContinueWith(
            _ =>
            {
                control.RequestExit(ExitClean);
                exitTcs.TrySetResult();
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        if (!IdleStart.ShouldDial(config))
        {
            // Idle: no server, or one this run was told not to dial.
            // The pane is up and every command that does not need a session works, connect and reco included.
            // Plain text: Consolonia paints its own brushes rather than consuming SGR, so the classic host's colors would print literally.
            backend.WriteLine(IdleStart.Banner(config, client, ConsoleColorDepth.Disable));

            // ... but a bare banner tells a fresh user nothing to do next, so offer the saved servers (or a new address) as a dialog.
            // Skipped for the file-input drive: a file owns that run and nobody is there to click.
            if (!FileInputDriver.IsEnabled)
            {
                bool exitClient = await ShowConnectDialogAsync(client, backend, config, ct).ConfigureAwait(false);
                if (exitClient)
                {
                    control.RequestExit(ExitClean);
                    return await ShutdownAsync(client, backend, ExitClean).ConfigureAwait(false);
                }
            }
        }
        else
        {
            try
            {
                await client.StartAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return await ShutdownAsync(client, backend, ExitClean).ConfigureAwait(false);
            }
            catch (VersionResolutionException ex)
            {
                backend.WriteLine(Strings.VersionResolutionFailed(ex.Message));
                return await ShutdownAsync(client, backend, ExitVersionResolution).ConfigureAwait(false);
            }
            catch (LoginRejectedException ex)
            {
                backend.WriteLine(Strings.LoginRejected(DescribeLoginRejection(client, ex)));
                return await ShutdownAsync(client, backend, ExitLoginRejected).ConfigureAwait(false);
            }
            catch (ConnectFailedException ex)
            {
                // Interactive TUI stays up as an offline prompt (reco/connect/exit route through the command service).
                backend.WriteLine(Strings.ConnectFailed(ex.Message));
            }
            catch (Umpk.Auth.AuthException ex)
            {
                backend.WriteLine(Strings.AuthFailed(ex.Message));
                return await ShutdownAsync(client, backend, ExitLoginRejected).ConfigureAwait(false);
            }
            catch (DmcbkAuthInteractionUnavailableException ex)
            {
                backend.WriteLine(Strings.AuthFailed(ex.Message));
                return await ShutdownAsync(client, backend, ExitLoginRejected).ConfigureAwait(false);
            }
        }

        Task? fileInput = null;
        if (FileInputDriver.IsEnabled)
        {
            fileInput = Task.Run(
                () => FileInputDriver.RunAsync(line => ProcessLineAsync(client, backend, line, ct), ct), ct);
        }

        var completed = await Task.WhenAny(
            exitTcs.Task,
            remoteStop.Task,
            backend.Completion,
            fileInput ?? new TaskCompletionSource().Task).ConfigureAwait(false);

        int code;
        if (completed == remoteStop.Task)
        {
            backend.WriteLine(Strings.DisconnectedRemotely(Describe(client.LastDisconnect)));
            code = ExitConnectionLost;
        }
        else
            // Covers the exit command, the file-input drive running out, and the UI loop being closed (double Ctrl+C): each of them must still answer 3 when the server had already killed the session, which is what HostExit.Resolve folds in.
            code = HostExit.Resolve(control);

        return await ShutdownAsync(client, backend, code).ConfigureAwait(false);
    }

    private static async Task<int> ShutdownAsync(Client client, TuiBackend backend, int code)
    {
        await client.StopAsync().ConfigureAwait(false);
        backend.Shutdown();
        await backend.Completion.ConfigureAwait(false);
        return code;
    }

    /// <summary>
    /// Runs the idle connect dialog and acts on it: persists a save choice (refreshing the snapshot the commands read, the LangCommand write-back precedent) and feeds the connect through the normal input pipeline so it echoes and reports exactly like a typed line.
    /// Dismissal, a failed save, or cancellation leaves the prompt as it was.
    /// Returns true only for the explicit Exit client action.
    /// </summary>
    internal static async Task<bool> ShowConnectDialogAsync(
        Client client, TuiBackend backend, DmcbkConfiguration config, CancellationToken ct)
    {
        TuiConnectChoice? choice;
        try
        {
            choice = await TuiConnectDialog.PickAsync(backend, config, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (choice is null)
            return false;

        if (choice.ExitClient)
            return true;

        if (choice.ToSave is { } entry)
        {
            if (config.SourceFolder is not { } folder)
            {
                backend.WriteLine(Strings.Host(Strings.TuiConnectNoConfig));
                return false;
            }

            try
            {
                new DmcbkConfigurationLoader(folder, loggerFactory: NullLoggerFactory.Instance)
                    .SaveServer(entry, makeActive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                backend.WriteLine(Strings.Host(Strings.TuiConnectSaveFailed(ex.Message)));
                return false;
            }

            var servers = config.Servers.Servers
                .Where(s => !string.Equals(s.Name, entry.Name, StringComparison.OrdinalIgnoreCase))
                .Append(entry)
                .ToList();
            client.Commands.ReloadConfiguration(config with
            {
                Servers = config.Servers with { Servers = servers, ActiveServer = entry.Name },
            });
            backend.WriteLine(Strings.Host(Strings.TuiConnectSaved(entry.Name, entry.Host, entry.Port)));
        }

        await ProcessLineAsync(client, backend, choice.InputLine, ct).ConfigureAwait(false);
        return false;
    }

    private static async Task ProcessLineAsync(Client client, TuiBackend backend, string line, CancellationToken ct)
    {
        try
        {
            InputRouting routing = await client.Commands.HandleInputAsync(line, ct).ConfigureAwait(false);
            if (routing.Result is { Message: { Length: > 0 } message } result
                && routing.Action is InputAction.CommandExecuted or InputAction.NotConnected)
            {
                // Same decoration the classic host applies, from the same place, so the two cannot drift.
                backend.WriteLine(
                    ResultFormatting.Decorate(result.Status, message, client.Commands.Glyphs) ?? message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            backend.WriteLine(Strings.SendFailed(ex.Message));
        }
    }

    private static async Task RefreshSuggestionsAsync(Client client, TuiBackend backend, string text, int caret)
    {
        try
        {
            (IReadOnlyList<string> suggestions, int start, int end) =
                await TuiCompletion.ComputeAsync(client, text, caret).ConfigureAwait(false);
            backend.UpdateSuggestions(suggestions, start, end);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            backend.UpdateSuggestions([], caret, caret);
        }
    }

    /// <summary>The TUI brush for a chat signature-standing family (the ANSI palette's TUI counterpart).</summary>
    private static Avalonia.Media.IBrush TuiStandingBrush(ChatStanding kind) => kind switch
    {
        ChatStanding.Verified => Avalonia.Media.Brushes.Green,
        ChatStanding.Rejected => Avalonia.Media.Brushes.Red,
        ChatStanding.Unverified => Avalonia.Media.Brushes.Yellow,
        ChatStanding.Insecure => Avalonia.Media.Brushes.Blue,
        _ => Avalonia.Media.Brushes.Gray,
    };

    /// <summary>Renders a disconnect for the TUI, through the one shared describer every host uses.</summary>
    private static string Describe(DisconnectInfo? info) => DisconnectDescription.Describe(info);

    /// <summary>
    /// Renders a <see cref="LoginRejectedException"/>'s server-supplied reason for the TUI: the structured <see cref="Umpk.Text.Component"/> when the server sent one, rendered through the client's negotiated-era translations, else the exception's own message.
    /// </summary>
    private static string DescribeLoginRejection(Client client, LoginRejectedException ex)
        => ex.Reason?.ToPlainText(client.Translations) is { Length: > 0 } reason ? reason : ex.Message;
}
