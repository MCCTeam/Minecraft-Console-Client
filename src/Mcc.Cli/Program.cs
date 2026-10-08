using Mcc.Cli.BeaconTooling;
using Mcc.Cli.Commands;
using Mcc.Cli.Hosting;
using Mcc.Cli.Hosting.Classic;
using Mcc.Cli.Input;
using Mcc.Cli.Localization;
using Mcc.Cli.Logging;
using Mcc.Cli.Plugins;
using Mcc.Cli.Startup;
using Mcc.Cli.Tui.Authentication;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Presentation;
using Mcc.Cli.Presentation;
using System.Globalization;
using DMCBK.Core.Localization;
using Mcc.Cli;
using Mcc.Cli.Configuration;
using Mcc.Cli.Diagnostics;
using Mcc.Cli.Tui;
using DMCBK.Core;
using DMCBK.Core.Commands;
using DMCBK.Core.Presentation;
using DMCBK.Core.Configuration;
using DMCBK.Core.Plugins;
using DMCBK.PluginSdk;
using DMCBK.Marketplace;
using Microsoft.Extensions.Logging;
using Umpk.Client;
using Umpk.Client.Events;
using Umpk.Data.Java;

return await CliHost.RunAsync(args).ConfigureAwait(false);

/// <summary>
/// The classic console host: parse the positional/dotted contract, load (and first-run generate) the configurations/ folder, build the client from the snapshot, then drive it either from stdin or, when MCC_FILE_INPUT=1, from the tailed input file.
/// Exit codes and the "Server was successfully joined" log line are preserved for the test harness.
/// </summary>
internal static class CliHost
{
    // Exit codes (legacy semantic shape): 0 clean, 1 usage, 2 version-resolution, 3 connection lost, 4 login rejected.
    // Defined once in HostExit and shared with the TUI host.
    private const int ExitClean = HostExit.Clean;
    private const int ExitUsage = HostExit.Usage;
    private const int ExitVersionResolution = HostExit.VersionResolution;
    private const int ExitConnectionLost = HostExit.ConnectionLost;
    private const int ExitLoginRejected = HostExit.LoginRejected;

    public static async Task<int> RunAsync(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Before everything, including the writer that clears the terminal: this one is a check that prints and exits, and it must not read or generate a configuration folder on the way.
        if (CliHelp.TryHandle(args, out int helpExit))
            return helpExit;

        if (PluginValidateOneShot.TryHandle(args, out int checkExit))
            return checkExit;

        if (await MarketplaceValidateOneShot.HandleAsync(args).ConfigureAwait(false) is { } marketplaceExit)
            return marketplaceExit;

        if (BeaconLintOneShot.TryHandle(args, out int lintExit))
            return lintExit;

        if (BeaconRunOneShot.TryHandle(args, out int runExit))
            return runExit;

        if (BeaconFormatOneShot.TryHandle(args, out int formatExit))
            return formatExit;

        if (!CliArguments.TryParse(args, out CliArguments parsed, out string? argError))
        {
            if (argError is not null)
                HostConsole.WriteErrorLine(Strings.Host(argError));

            HostConsole.WriteErrorLine(Strings.Usage);
            return ExitUsage;
        }

        if (parsed.Exercise is not null && !string.Equals(parsed.Exercise, SmokeExercise.Name, StringComparison.Ordinal))
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.UnknownExercise(parsed.Exercise)));
            return ExitUsage;
        }

        string configArg = parsed.ConfigArg ?? ConfigurationPaths.DefaultFolderName;
        string folder = ConfigurationPaths.ResolveFolder(configArg);

        // Secrets live in <folder>/.env and settings link to them with env:NAME.
        // Loading here, before any config or plugin reads the environment, means nobody sources anything by hand.
        DotEnv.Load(folder);

        // console.toml loads first (independent of client.toml/accounts.toml/servers.toml) so the classic banner can print before any client.toml config-load message, and so the color/timestamp settings it carries are known in time for the client.toml loader bootstrap logger below.
        // Bootstrap color assumes defaults from console.toml (24-bit color on, no timestamp) since console.toml itself has not loaded yet.
        // A malformed console.toml still falls back to defaults, and logs a visible warning through this bootstrap logger instead of failing silently.
        ConsoleColorDepth bootstrapDepth = TerminalCapability.ResolveColorDepth(ConsoleColorDepth.Vt10024Bit);
        var consoleBootstrapLogger = new ConsoleLoggerFactory(LogLevel.Warning, bootstrapDepth, timestamps: false);
        ConsoleHostConfig console = ConsoleHostConfig.Load(
            folder, consoleBootstrapLogger.CreateLogger("Mcc.Cli.Configuration"), parsed.ConsoleOverrides);
        ConsoleColorDepth colorDepth = TerminalCapability.ResolveColorDepth(console.ColorDepth);

        bool interactive = parsed.Exercise is null && !FileInputDriver.IsEnabled;
        bool tuiMode = string.Equals(console.ConsoleMode, "tui", StringComparison.OrdinalIgnoreCase)
            && parsed.Exercise is null;
        bool rich = interactive && !tuiMode && RichConsole.IsSupported;

        // Bootstrap logging at the now-known console settings so the client.toml loader itself can log; level toggles are refined once the client.toml logging block is known.
        ConfigurationLoadResult loaded;
        DmcbkConfigurationLoader loader;
        try
        {
            var bootstrapLogger = new ConsoleLoggerFactory(LogLevel.Warning, colorDepth, console.Timestamps);
            loader = new DmcbkConfigurationLoader(folder, loggerFactory: bootstrapLogger);
            loaded = loader.Load(parsed.Overrides, generateMissing: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.ConfigError(ex.Message)));
            return ExitUsage;
        }

        DmcbkConfiguration config = loaded.Config;

        // The language, applied HERE and nowhere later.
        // Everything downstream reads a culture: the guided login prompts, the plugin host with every plugin's lang/ table and settings comments, the manual, and any config file generated from this point on.
        // Until this work the one line that applied the setting sat inside RunClientAsync, after all of them, so the setting could not reach the things it was for.
        // CurrentCulture goes with it, because Mcc.Cli.Localization.MccStrings.Format formats its arguments with the ambient culture; files are unaffected, every writer already forces invariant around its serializer.
        CultureInfo uiCulture = UiCulture.Apply(config.Localization.Language);
        CultureInfo.DefaultThreadCurrentCulture = uiCulture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        loader.UseCulture(uiCulture);

        // First-run guided login: with nothing in accounts.toml that can log in, ask which account kind to use rather than failing the build with "an account is required" and leaving the user to hand-write TOML.
        // In TUI mode the whole startup (welcome, login, notices) happens inside the TUI: the backend starts first and everything goes to its log or dialogs, so no plain-console line ever appears under it.
        // On the classic host the welcome block prints first (banner, star nudge, early-build warning), so a fresh user sees what they started before being asked anything; the login answers themselves are the problem child there (terminal echo outside ConsoleWriter drifts the popup ring), hence the clear-and-reprint after a classic login (see RichConsole.ClearScreen).
        ConfiguredAccount? pendingAccount = null;
        string? offlineSavedName = null;
        string? offlineSaveError = null;
        string? rememberedActive = null;
        string? preAuthedProfile = null;

        // The classic welcome block, printed before the login prompt and re-printed after a login clears the echo drift.
        // One place so the two copies cannot disagree.
        void PrintWelcome()
        {
            PrintClassicBanner(console, colorDepth);
            HostConsole.WriteErrorLine(Strings.Host(
                Strings.StarUs(GlyphModeResolver.Resolve(console.GlyphMode).IsEmoji)));
            new MarkdownConsoleRenderer(colorDepth, GlyphModeResolver.Resolve(console.GlyphMode))
                .Write(Strings.EarlyBuildWarningMarkdown);
        }

        // The TUI welcome: the view already paints its own icon banner with the version range, so the log needs only the star nudge and the early-build warning, in plain lines (the TUI paints its own cells; SGR and Markdown would print literally).
        void PrintTuiWelcome(TuiBackend tui)
        {
            bool emoji = GlyphModeResolver.Resolve(console.GlyphMode).IsEmoji;
            tui.WriteLine(Strings.Host(Strings.StarUs(emoji)));
            tui.WriteLine(Strings.Host(Strings.TuiWelcomeEarlyBuild));
            tui.WriteLine(Strings.Host(Strings.TuiWelcomeUnfinished));
        }

        bool loginNeeded = GuidedLogin.IsNeeded(config);
        if (loginNeeded && (Console.IsInputRedirected || FileInputDriver.IsEnabled || parsed.Exercise is not null))
        {
            // Nothing here can answer a prompt: a closed stdin, a run driven from the input file, or a scripted exercise.
            // Say what is missing instead of reading end-of-input in a loop.
            // This exit is never rich and never TUI (no writer, no backend, no popup), so the plain stream split holds.
            HostConsole.WriteErrorLine(Strings.Host(DMCBK.Core.Localization.McStrings.mcc_auth_method_unavailable));
            return ExitUsage;
        }

        // TUI early start: the backend owns the terminal from here, so every later startup line must go through it (the notice delegate below), never through the plain console.
        // Started this early so the welcome block and the login dialogs render inside the TUI instead of above it.
        TuiBackend? tuiBackend = null;
        if (tuiMode)
        {
            tuiBackend = new TuiBackend();
            bool tuiColor = TerminalCapability.ResolveColor(console.ColorEnabled);
            tuiBackend.Start(
                tuiColor, console.Timestamps, console.TuiLogScrollback, console.SuggestionMaxDisplayed,
                console.DisplayIconBanner);
        }

        // Every host notice from here on: the TUI log when it owns the screen, else the classic sink (rich writer when active, stderr otherwise).
        Action<string> notice = tuiBackend is not null ? tuiBackend.WriteLine : HostConsole.WriteErrorLine;

        // ConsoleWriter.Init() clears the terminal when output is not redirected (ConsoleInteractive/ConsoleWriter.cs:18-19).
        // Initialising here, ahead of the welcome block, means the clear happens first and everything printed afterwards survives.
        // The submodule is not ours to change.
        // Classic only: TUI mode never marks the rich writer.
        if (rich)
        {
            RichConsole.InitWriter(console, colorDepth);
            HostConsole.UseRichWriter();
        }

        if (tuiBackend is not null)
            PrintTuiWelcome(tuiBackend);
        else
            PrintWelcome();

        if (loginNeeded)
        {
            // Guided answers are MCC's questions, so they carry the stamp; the stamp is added here, at the sink, leaving the shared corpus strings untouched for every other reader.
            ConfiguredAccount? chosen;
            if (tuiBackend is not null)
            {
                TuiLoginChoice choice = await TuiGuidedLogin
                    .PromptAsync(tuiBackend, config)
                    .ConfigureAwait(false);
                if (choice.Account is null)
                {
                    notice(Strings.Host(DMCBK.Core.Localization.McStrings.error_login_cancel));
                    await ShutdownTuiEarlyAsync(tuiBackend).ConfigureAwait(false);
                    return ExitLoginRejected;
                }

                chosen = choice.Account;
                preAuthedProfile = choice.PreAuthedProfile;
            }
            else
            {
                chosen = await GuidedLogin
                    .PromptAsync(line => HostConsole.WriteLine(Strings.Host(line)), Console.ReadLine)
                    .ConfigureAwait(false);
                if (chosen is null)
                {
                    notice(Strings.Host(DMCBK.Core.Localization.McStrings.error_login_cancel));
                    await ShutdownTuiEarlyAsync(tuiBackend).ConfigureAwait(false);
                    return ExitLoginRejected;
                }
            }

            config = WithChosenAccount(config, chosen);

            // Offline has no sign-in that could fail, so it is saved now (file only; the confirmation is printed with the rest below).
            // The online kinds are held until a login actually succeeds, so a wrong provider URL or an abandoned sign-in leaves accounts.toml untouched and the prompt runs again next start.
            if (chosen.Kind == DmcbkAccountKind.Offline)
            {
                try
                {
                    loader.SaveAccount(chosen, makeActive: true);
                    offlineSavedName = chosen.Name;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    offlineSaveError = ex.Message;
                }
            }
            else
                pendingAccount = chosen;

            if (rich)
            {
                // Resync the popup ring: the answers typed above were echoed by the terminal outside the writer.
                // ClearScreen evicts the ring's stale entries with blanks, and the reprint puts the welcome block back through the writer, so screen and ring agree again before the REPL (and its first popup) starts.
                // TUI needs none of this: dialogs never touch the classic ring, and the TUI log is the record.
                RichConsole.ClearScreen();
                PrintWelcome();
            }
        }
        else
        {
            // An account is already remembered, so it is named instead of silently assumed.
            // Same display fallback as the console title further down: the login when set, else the name.
            rememberedActive = string.IsNullOrWhiteSpace(config.ResolvedAccount.Login)
                ? config.ResolvedAccount.Name
                : config.ResolvedAccount.Login;
        }

        if (loaded.Generated)
            notice(Strings.Host(Strings.ConfigGenerated(folder)));

        foreach (ConfigurationWarning warning in loaded.Warnings)
            notice(Strings.ConfigWarning(warning.Message));

        if (offlineSavedName is not null)
            notice(Strings.Host(Mcc.Cli.Localization.MccStrings.Format("mcc.auth_method_saved", offlineSavedName)));
        else if (offlineSaveError is not null)
            notice(Strings.Host(Strings.ConfigError(offlineSaveError)));
        else if (pendingAccount is not null)
        {
            // Pre-authed at selection (TUI Microsoft): the sign-in already happened, so say who as instead of promising a future prompt.
            // Otherwise the deferred notice stands.
            notice(preAuthedProfile is null
                ? Strings.GuidedOnlineDeferred(colorDepth)
                : Strings.Host(Strings.TuiLoginSignedIn(preAuthedProfile)));
        }
        else if (rememberedActive is not null)
            notice(Strings.Host(Strings.UsingConfiguredAccount(rememberedActive)));

        LogFileSink? logFileSink = null;
        if (config.Logging.LogToFile)
        {
            try
            {
                logFileSink = new LogFileSink(
                    ResolveLogPath(folder, config.Logging.LogFile),
                    config.Logging.PrependTimestamp,
                    config.Logging.SaveColorCodes);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                notice(Strings.Host(Strings.LogFileError(ex.Message)));
            }
        }

        // The diagnostics bundle.
        // Opened here, before the client is built, so a failure during startup or connection is inside the transcript rather than before it.
        var diagnosticsSettings = new DiagnosticsSettings(
            config.Diagnostics.Enabled,
            config.Diagnostics.CapturePackets,
            config.Diagnostics.MaxCaptureMegabytes,
            config.Diagnostics.KeepSessions);

        string logsRoot = Path.Combine(folder, "logs");
        SessionDiagnostics? diagnostics = null;
        if (diagnosticsSettings.Enabled)
        {
            try
            {
                Directory.CreateDirectory(logsRoot);
                SessionDiagnostics.Prune(logsRoot, diagnosticsSettings.KeepSessions);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                notice(Strings.Host(Strings.LogFileError(ex.Message)));
            }

            diagnostics = SessionDiagnostics.TryOpen(
                logsRoot,
                diagnosticsSettings,
                string.IsNullOrWhiteSpace(config.ResolvedHost) ? "offline" : config.ResolvedHost);

            if (diagnostics is not null)
            {
                HostConsole.Transcript = diagnostics.WriteConsole;
                diagnostics.WriteManifest(
                    $"{config.ResolvedHost}:{config.ResolvedPort}", ClientVersion.Current);
                diagnostics.WriteJson("system.json", EnvironmentReport.Collect());
                diagnostics.WriteJson("client.json", ClientReport.Collect(config, console, parsed));
            }
        }

        // The full logger: config-driven toggles plus the file sink.
        // In TUI mode its lines go to the backend instead of the raw console (which the TUI owns by now), and timestamps come from the backend's own prefix rather than doubled from both.
        Action<string>? tuiSink = tuiBackend is not null ? tuiBackend.WriteLine : null;
        var loggerFactory = new ConsoleLoggerFactory(
            config.Logging, logFileSink, colorDepth, console.Timestamps && tuiSink is null, tuiSink);

        // Backend switch (console.toml Console.General.ConsoleMode): the TUI (Consolonia) host or the classic ConsoleInteractive host.
        // The TUI backend itself starts much earlier (before the welcome block), so by here it is either up or this is the classic path.
        // TUI needs a real terminal to render, so the scripted exercise path (which redirects output) always uses classic; the file-input drive works in both (it tails a file, not stdin).
        TuiHostUi? tuiHostUi = tuiMode ? new TuiHostUi(tuiBackend!, console.TabListShowTeams) { ShowInventoryLayout = console.ShowInventoryLayout } : null;
        IHostInterface hostInterface = tuiMode
            ? new TuiHostInterface(tuiBackend!, tuiHostUi!)
            : new ConsoleHostInterface(new AnsiComponentRenderer(new HostTranslations(), colorDepth), colorDepth);

        Client client;
        try
        {
            ClientBuilder builder = new ClientBuilder()
                .UseConfiguration(config)
                .UseLoggerFactory(loggerFactory)
                .UseHostInterface(hostInterface)
                .UseCommands()
                .UseBeacon()
                .UseConfigurationStorage(folder)
                .UseApplication(new HostApplication("mcc", ClientVersion.Current,
                    new HashSet<string> { "aspnetcore" }))
                // both CLI hosts render the connect-time status panel, so both want the display-only ping even when the version is pinned (off by default; see UsePingForDisplayWhenPinned).
                .UsePingForDisplayWhenPinned();
            if (config.Accounts.SessionCache == CacheMode.Disk)
                builder.UseTokenStorePath(Path.Combine(folder, config.Accounts.CacheDirectory, "auth-tokens"));
            client = builder.Build();
        }
        catch (InvalidOperationException ex)
        {
            notice(Strings.Host(Strings.ConfigError(ex.Message)));
            await ShutdownTuiEarlyAsync(tuiBackend).ConfigureAwait(false);
            return ExitUsage;
        }

        string username = string.IsNullOrWhiteSpace(config.ResolvedAccount.Login)
            ? config.ResolvedAccount.Name
            : config.ResolvedAccount.Login;

        if (pendingAccount is not null)
            AttachGuidedAccountSave(client, loader, pendingAccount, loggerFactory);

        // Plugin host: discover from the plugins/ folder and activate once, before the first connect, so plugins can subscribe to the session lifecycle.
        // Registered as the client's plugin host so the plugins / plugin load / reload commands reach it.
        string pluginsRoot = ResolvePluginsRoot(folder);
        var pluginHost = new PluginHost(
            client, pluginsRoot, loggerFactory, client.Translations, client.Variables, uiCulture,
            config.Plugins);

        // The market attaches itself to the client.
        // All this host supplies is how the install question gets asked, which is the part DMCBK.Core cannot own: console-free code cannot read a line.
        using var marketplaceHttp = new HttpClient();
        var pluginMarket = new MarketplaceService(client, pluginsRoot,
            Path.Combine(folder, "marketplaces.toml"), marketplaceHttp, pluginHost)
        {
            AutomaticUpdates = new() { Logger = loggerFactory.CreateLogger("Mcc.Plugins.Market") },
            Confirm = PluginInstallPrompt.For(tuiBackend),
            Culture = uiCulture,
        };

        client.AttachModule(pluginHost, "plugins");
        client.AttachModule(pluginMarket, "marketplace");
        pluginHost.InstallationSource = pluginMarket.ReadInstallationsAsync;
        pluginHost.ChangeEnabled = pluginMarket.SetEnabledAsync;

        try
        {
            PluginActionResult loadResult = await pluginHost.LoadAllAsync().ConfigureAwait(false);
            notice(Strings.PluginsLoaded(loadResult.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            notice(Strings.PluginsLoadError(ex.Message));
        }

        // The marketplace policies run on their own task with their own delay, so nothing here waits on the network before the first connect.
        // The token stops a pass that is still inside its delay when the client goes down, so it cannot wake up and swap plugin folders under a host that is shutting down.
        using var autoUpdate = new CancellationTokenSource();
        Task automaticUpdates = Task.Run(async () =>
        {
            try
            {
                await pluginMarket.StartAutoUpdateAsync(autoUpdate.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                loggerFactory.CreateLogger("Mcc.Plugins.Market")
                    .LogWarning(ex, "{Message}", Strings.MarketAutoUpdateFailed);
            }
        }, autoUpdate.Token);

        // A crash that escapes to here has already lost its stack by the time the process writes anything, so the bundle takes a copy first.
        // The AppDomain hook covers the other half: a fault on a background thread (the read loop, a plugin's timer) never passes through this try at all.
        UnhandledExceptionEventHandler? onUnhandled = null;
        if (diagnostics is not null)
        {
            onUnhandled = (_, e) =>
            {
                if (e.ExceptionObject is Exception unhandled)
                    diagnostics.WriteCrash("Unhandled exception", unhandled);
            };
            AppDomain.CurrentDomain.UnhandledException += onUnhandled;
        }

        try
        {
            using (logFileSink)
            await using (client)
            {
                if (tuiBackend is not null && tuiHostUi is not null)
                {
                    using var tuiCts = new CancellationTokenSource();
                    ConsoleCancelEventHandler onTuiCancel = (_, e) => e.Cancel = true; // the TUI owns Ctrl+C (double-press quits)
                    Console.CancelKeyPress += onTuiCancel;

                    // a window close/logoff/shutdown/SIGTERM/SIGHUP requests the same clean shutdown Ctrl+C does, then gives the async disconnect a bounded window to run before the process actually exits.
                    ProcessCloseGuard.Register(() =>
                    {
                        try
                        {
                            tuiCts.Cancel();
                            client.StopAsync().Wait(TimeSpan.FromSeconds(3));
                        }
                        catch
                        {
                            // Best-effort: the process is already going down.
                        }
                    });

                    try
                    {
                        return await TuiHost
                            .RunAsync(client, tuiBackend, tuiHostUi, console, config, new HostControl(), tuiCts.Token)
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        Console.CancelKeyPress -= onTuiCancel;
                    }
                }

                return await RunClientAsync(
                        client, hostInterface, console, config, logFileSink, diagnostics, parsed.Exercise, username)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (Record(diagnostics, ex))
        {
            throw; // unreachable: the filter always returns false, and exists only for its side effect
        }
        finally
        {
            autoUpdate.Cancel();
            try { await automaticUpdates.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            if (onUnhandled is not null)
                AppDomain.CurrentDomain.UnhandledException -= onUnhandled;

            if (diagnostics is not null)
            {
                // Last chance at the server report.
                // On a clean exit the session is usually already gone, in which case the one written shortly after joining is what survives.
                await TryWriteServerReportAsync(diagnostics, client).ConfigureAwait(false);

                string? bundle = diagnostics.Finish();
                if (bundle is not null)
                    HostConsole.WriteErrorLine(Strings.Host(Strings.DiagnosticsBundleWritten(bundle)));
            }
        }
    }

    /// <summary>
    /// Records a fault into the bundle from an exception FILTER, so the crash is captured while the stack is still live and the exception then continues to propagate untouched.
    /// Always returns false.
    /// </summary>
    private static bool Record(SessionDiagnostics? diagnostics, Exception exception)
    {
        diagnostics?.WriteCrash("Fatal", exception);
        return false;
    }

    /// <summary>
    /// Writes <c>server.json</c>, if a session is live enough to answer.
    /// </summary>
    /// <remarks>
    /// Called several times over a run and deliberately never destructive: a collection that found no session writes nothing, so the last report taken while the client was actually connected is the one that survives into the bundle.
    /// Later writes are better than earlier ones, because the brand arrives on a plugin channel and the tick rate is measured over several seconds, but only an earlier one survives a crash, so the run takes several and lets the good ones win.
    /// </remarks>
    private static async Task TryWriteServerReportAsync(SessionDiagnostics diagnostics, Client client)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (await ServerReport.CollectAsync(client, timeout.Token).ConfigureAwait(false) is { } report)
                diagnostics.WriteJson("server.json", report);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No session, or one that went away mid-collection.
            // Not worth a line of output.
        }
    }

    /// <summary>
    /// Folds a guided-login choice into the snapshot: it becomes the resolved account, is made active, and is upserted into the account list so a later reload of the in-memory snapshot agrees with what the builder was handed.
    /// </summary>
    private static DmcbkConfiguration WithChosenAccount(DmcbkConfiguration config, ConfiguredAccount chosen)
    {
        ConfiguredAccount[] accounts = config.Accounts.Accounts
            .Where(a => !string.Equals(a.Name, chosen.Name, StringComparison.OrdinalIgnoreCase))
            .Append(chosen)
            .ToArray();

        return config with
        {
            ResolvedAccount = chosen,
            Accounts = config.Accounts with { ActiveAccount = chosen.Name, Accounts = accounts },
        };
    }

    /// <summary>
    /// Persists a guided-login account into accounts.toml and makes it active, so later starts use it without asking again.
    /// A write failure is reported and swallowed: the session in hand is already authenticated, and taking it down over a config write nobody asked for would be worse than saying the choice was not saved.
    /// </summary>
    private static void SaveGuidedAccount(DmcbkConfigurationLoader loader, ConfiguredAccount account, ILogger? logger)
    {
        try
        {
            loader.SaveAccount(account, makeActive: true);
            string saved = Mcc.Cli.Localization.MccStrings.Format("mcc.auth_method_saved", account.Name);
            if (logger is null)
                // The logger path below already stamps Information lines with "[MCC] " of its own (ConsoleLogger), so only the direct write needs the stamp here.
                HostConsole.WriteErrorLine(Strings.Host(saved));
            else
                logger.LogInformation("{Message}", saved);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Through the logger when there is one: this runs during a live session, and in TUI mode the backend owns the terminal by then, so a raw console write would corrupt its screen.
            if (logger is null)
                HostConsole.WriteErrorLine(Strings.Host(Strings.ConfigError(ex.Message)));
            else
                logger.LogError("{Message}", Strings.ConfigError(ex.Message));
        }
    }

    /// <summary>
    /// Saves a guided-login online account once its first sign-in has resolved a profile, renaming it from the setup placeholder to that profile.
    /// UMPK caches a session under both the login hint it was given and the resolved profile name, so the rename costs the next start nothing: it resumes on the profile key.
    /// Status changes can arrive on any thread, hence the interlocked one-shot latch.
    /// </summary>
    private static void AttachGuidedAccountSave(
        Client client, DmcbkConfigurationLoader loader, ConfiguredAccount pending, ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("Mcc.Cli.Configuration");
        int saved = 0;
        client.StatusChanged += (_, _) =>
        {
            if (client.CurrentSession is not { } session || Interlocked.Exchange(ref saved, 1) != 0)
                return;

            string profile = session.Profile.Name;
            SaveGuidedAccount(loader, pending with { Name = profile, Login = profile }, logger);
        };
    }

    /// <summary>
    /// Shuts a just-started TUI backend back down for the exits that happen before <see cref="Tui.TuiHost"/> owns it (a cancelled login, a client that fails to build).
    /// Without this the UI thread and the terminal it owns leak past the process exit path.
    /// Null (classic mode) is a no-op.
    /// </summary>
    private static async Task ShutdownTuiEarlyAsync(TuiBackend? backend)
    {
        if (backend is null)
            return;

        backend.Shutdown();
        await backend.Completion.ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the log file path: an absolute path is used as-is; a relative path is taken relative to the configurations folder so the log lands next to the config the run used, not the process cwd.
    /// </summary>
    private static string ResolveLogPath(string configFolder, string logFile)
        => Path.IsPathRooted(logFile) ? logFile : Path.Combine(configFolder, logFile);

    /// <summary>
    /// Resolves the plugins root: the <c>MCC_PLUGINS</c> override when set, otherwise a <c>plugins/</c> folder adjacent to the configurations folder (its sibling).
    /// </summary>
    private static string ResolvePluginsRoot(string configFolder)
    {
        string? overridePath = Environment.GetEnvironmentVariable("MCC_PLUGINS");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        string full = Path.GetFullPath(configFolder);
        string? parent = Path.GetDirectoryName(full);
        return Path.Combine(parent ?? full, "plugins");
    }

    private static async Task<int> RunClientAsync(
        Client client,
        IHostInterface hostInterface,
        ConsoleHostConfig console,
        DmcbkConfiguration config,
        LogFileSink? logFileSink,
        SessionDiagnostics? diagnostics,
        string? exercise,
        string username)
    {
        // Set the console window title before connecting (best-known username/host), then again once Playing below with whatever the session actually resolved.
        UpdateConsoleTitle(console.ConsoleTitle, username, config.ResolvedHost);

        // The rich ConsoleInteractive reader is used only for a genuine interactive terminal; the file-input, exercise, and redirected paths keep the plain reader so the harness contract stays intact.
        // The writer half was already initialised in RunAsync, before the first line of host output, because its Init() clears the terminal; HostConsole.IsRich is that decision, not a second one.
        bool interactive = exercise is null && !FileInputDriver.IsEnabled;
        bool rich = HostConsole.IsRich;

        ConsoleColorDepth colorDepth = TerminalCapability.ResolveColorDepth(console.ColorDepth);

        Action<string> writeLine = HostConsole.Sink;

        // Effect and container notices.
        // TUI mode never reaches this method (it returns from TuiHost.RunAsync instead), so the wiring lives in SessionNotices and both hosts attach it.
        await using var notices = SessionNotices.Attach(client, config, writeLine);

        bool color = colorDepth != ConsoleColorDepth.Disable;
        var renderer = new AnsiComponentRenderer(client.Translations, colorDepth);
        var chatFilter = new LogFilter(config.Logging.ChatFilterRegex, config.Logging.FilterMode);
        var standing = new ChatStandingMarker(config.Chat.Signature, color);
        var presenter = new ChatPresenter(
            renderer, console.Timestamps, chatFilter, logFileSink, config.Logging.ChatMessages, writeLine, standing);

        // the server-status/MOTD panel, printed on every connect attempt (subscribed before StartAsync so the ping this raises from, whichever path it takes, is never missed).
        client.ServerStatusReceived += (_, status) =>
        {
            // Fully qualified: Program.cs imports both Mcc.Cli and Mcc.Cli.Tui, which each have their own (unrelated) ServerStatusPanel; this is the classic-host one.
            foreach (string line in Mcc.Cli.Presentation.ServerStatusPanel.BuildLines(status, renderer, colorDepth))
                writeLine(line);
        };

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += onCancel;

        // a window close/logoff/shutdown/SIGTERM/SIGHUP requests the same clean shutdown Ctrl+C does, then gives the async disconnect a bounded window to run before the process actually exits.
        ProcessCloseGuard.Register(() =>
        {
            try
            {
                cancellation.Cancel();
                client.StopAsync().Wait(TimeSpan.FromSeconds(3));
            }
            catch
            {
                // Best-effort: the process is already going down.
            }
        });

        var remoteStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // set when a Reconnecting transition was announced, cleared (and "Reconnected." printed) at the next Playing transition; see the StatusChanged handler below for why Previous alone cannot detect this.
        bool reconnectPending = false;

        // One server report per run.
        // A reconnect re-enters Playing and would otherwise overwrite the report with the new session's, losing the one that belongs to the disconnect being investigated.
        bool serverReported = false;

        // Host-registered commands and the mutable chat-visibility state the console-chat command toggles.
        var state = new ConsoleHostState(console.DisplayChat);
        var control = new HostControl();
        client.Commands.RegisterHostCommand(new MccMenuCommand());
        client.Commands.RegisterHostCommand(new ClearConsoleCommand());
        client.Commands.RegisterHostCommand(new ConsoleChatCommand(state));
        client.Commands.RegisterHostCommand(new ExitCommand(control));

        // An in-process shutdown request (the MCP quit tool, a bridge) ends the run the way a typed quit does: flag the shared exit signal and cancel the loop token, and the existing teardown below (StopAsync, HostExit.Resolve) runs unchanged.
        _ = client.ShutdownRequested.ContinueWith(
            _ =>
            {
                control.RequestExit(HostExit.Clean);
                cancellation.Cancel();
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        // Resolve console.toml's Glyphs setting against THIS terminal and hand the core the answer.
        // The core never asks; see GlyphModeResolver.
        GlyphSet glyphs = GlyphModeResolver.Resolve(console.GlyphMode);
        client.Commands.Glyphs = glyphs;
        client.Commands.EchoCommands = console.EchoCommands;

        // The packet tap.
        // Attached once; the seam re-attaches it to every reconnect, so a capture spans a session that dropped and came back rather than stopping at the first disconnect.
        if (diagnostics?.CapturingPackets == true)
        {
            client.ObservePacketFrames((protocol, observation) => diagnostics.WriteFrame(
                protocol,
                observation.Flow,
                observation.Phase,
                observation.WireId,
                observation.RawPayload));
        }

        // Give the host UI its /man renderer, now that both the colour depth and the glyph set are known.
        if (hostInterface.Ui is ConsoleHostUi consoleUi)
        {
            consoleUi.ShowInventoryLayout = console.ShowInventoryLayout;
            consoleUi.Documents = new MarkdownConsoleRenderer(console.ColorDepth, glyphs);
        }

        client.Game.Chat.MessageReceived += (_, message) =>
        {
            if (state.ChatVisible)
                presenter.Print(message);
        };

        // Both of these carry no message text on purpose, and both exist because silence would be a lie.
        // A fully filtered message is one the server DID deliver and told the client to show none of; a stream gap is a message the server never delivered at all.
        // Vanilla shows nothing for the first and disconnects on the second, so neither has a rendering to copy: for a headless client the honest rendering is to name what happened without reproducing what was withheld.
        client.Game.Chat.MessageSuppressed += (_, suppressed) =>
        {
            if (state.ChatVisible)
                presenter.Print(Umpk.Text.Component.Text(Strings.ChatMessageWithheld(suppressed.SenderId)));
        };

        client.Game.Chat.StreamGap += (_, gap) =>
            presenter.Print(Umpk.Text.Component.Text(
                Strings.ChatStreamGapDetected(gap.ExpectedIndex, gap.ActualIndex, gap.TotalGaps)));

        // The client follows the transfer itself; this is only so the user is told where they are going, as the legacy client did (McClient.cs:426).
        client.ServerTransferRequested += (_, e) => writeLine(Strings.Host(Strings.TransferInitiated(e.Host, e.Port)));

        client.StatusChanged += (_, e) =>
        {
            // The durable half of "the server killed this session" lives in the control, because the interactive REPL deliberately survives a remote disconnect: by the time it quits, the stop signal below has long since been consumed and the control is the only remaining record.
            HostExit.ObserveStatus(control, e);

            if (e.Current == ClientStatus.Playing)
            {
                UpdateConsoleTitle(console.ConsoleTitle, client.CurrentSession?.Profile.Name ?? username, config.ResolvedHost);

                // Reconnecting -> Playing goes through Connecting (and possibly Authenticating) in between (UmpkClientSupervisor.SuperviseAsync/RunOneAttemptAsync), so Previous at THIS transition is never actually Reconnecting; reconnectPending is set when Reconnecting was observed and cleared here, tracking "we are mid reconnect" across those intermediate statuses.
                if (reconnectPending)
                {
                    reconnectPending = false;
                    writeLine(Strings.Host(Strings.Reconnected));
                }

                // The stamp is added at the sink: the const itself stays byte-identical for the harness that greps it, and every one of those greps is a substring match.
                HostConsole.WriteLine(Strings.Host(Strings.ServerJoined));

                // The server report, taken a moment after joining rather than right now: the brand arrives on a plugin channel and the tick rate is measured over several seconds, so collecting on this line would write a file of nulls.
                // Fire and forget, and deliberately not awaited on the status-change path, which must not block the session loop.
                if (diagnostics is not null && !serverReported)
                {
                    serverReported = true;
                    _ = Task.Run(async () =>
                    {
                        // Twice, because the two useful reports are different files.
                        // The early one exists so a session that crashes or is quit within seconds still has a server report at all; the later one is the complete one, once the brand has arrived and the tick rate has had time to be measured.
                        foreach (TimeSpan delay in new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(13) })
                        {
                            await Task.Delay(delay).ConfigureAwait(false);
                            await TryWriteServerReportAsync(diagnostics, client).ConfigureAwait(false);
                        }
                    });
                }
            }
            // A LIVE session entering the configuration phase is the server moving this client somewhere
            // else: 1.20.2+ re-enter configuration to hand a player to another backend, which is how every
            // proxy's "send me to survival" button works.
            // Vanilla shows a screen for it; a headless client that says nothing makes a working switch look like a button that did nothing, which is exactly how it was reported.
            // The Playing arm above then announces the arrival.
            else if (e.Current == ClientStatus.Configuring && e.Previous == ClientStatus.Playing)
                notices.AnnounceServerSwitch();
            // the supervisor already reports Reconnecting, but neither host printed anything for it.
            // Quiet for the initial connect (Previous is Created/Connecting): only a transition away from a session that was actually live (or had just ended) is a reconnect worth announcing.
            else if (e.Current == ClientStatus.Reconnecting
                && e.Previous is not (ClientStatus.Created or ClientStatus.Connecting))
            {
                reconnectPending = true;
                writeLine(Strings.Host(Strings.Reconnecting));
            }
            else if (e.Current == ClientStatus.Disconnected && e.Disconnect is { WasLocal: false } info)
            {
                // The server's own reason, printed wherever the session ends.
                // This used to live only on the file-input path below, but the interactive REPL SURVIVES a remote disconnect and drops to the offline prompt, so an interactive user saw the prompt change and nothing
                // else. A kick that named its cause ("Invalid signature for profile public key", which is
                // what a signed profile gets from an offline-mode server) then looked like an unexplained failure to connect.
                // WriteFormatted, not the plain sink: the reason is the SERVER's text and it is routinely styled.
                // Kick screens on the popular Russian servers, for one, are section-coded ("§cНеверный регистр ника!"), and a plain write put the codes on screen as literal characters.
                // It also splits an embedded newline into real lines, which a multi-line kick ("suspicious activity" + "blocked for 5 minutes") needs to stay readable.
                HostConsole.WriteFormatted(Strings.Host(Strings.DisconnectedRemotely(Describe(info, renderer))));
                remoteStop.TrySetResult();
            }
        };

        // Nothing to dial, or a configured server this run was told to leave alone.
        // The prompt below is reached either way, with the plugins loaded and every command that does not need a session working, connect and reco included.
        if (!IdleStart.ShouldDial(config))
        {
            // Already stamped inside IdleStart.Banner; stamping again would double it.
            writeLine(IdleStart.Banner(config, client, colorDepth));
            Console.CancelKeyPress -= onCancel;
            return await RunInputAsync(
                    client, control, rich, writeLine, console.SuggestionsEnabled, remoteStop, cancellation)
                .ConfigureAwait(false);
        }

        try
        {
            await client.StartAsync(cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.LoginAborted));
            return ExitClean;
        }
        catch (VersionResolutionException ex)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.VersionResolutionFailed(Strings.Describe(ex))));
            return ExitVersionResolution;
        }
        catch (LoginRejectedException ex)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.LoginRejected(DescribeLoginRejection(renderer, ex))));
            return ExitLoginRejected;
        }
        catch (ConnectFailedException ex)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.ConnectFailed(Strings.Describe(ex))));
            if (!interactive)
                return ExitConnectionLost;

            // Interactive offline prompt (reco/connect/exit/help) re-implemented over the command service.
            await RunReplAsync(client, control, rich, writeLine, console.SuggestionsEnabled, cancellation.Token).ConfigureAwait(false);
            await client.StopAsync().ConfigureAwait(false);
            return HostExit.Resolve(control, ExitConnectionLost);
        }
        catch (Umpk.Auth.AuthException ex)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.AuthFailed(Strings.Describe(ex))));
            return ExitLoginRejected;
        }
        catch (DmcbkAuthInteractionUnavailableException ex)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.AuthFailed(Strings.Describe(ex))));
            return ExitLoginRejected;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }

        // Scripted diagnostic mode: once Playing, run the checks instead of the interactive/file input loop, then disconnect and return the exercise's pass/fail exit code.
        if (exercise is not null)
        {
            int exerciseExit;
            try
            {
                exerciseExit = await SmokeExercise
                    .RunAsync(client, username, HostConsole.Sink, cancellation.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                exerciseExit = ExitClean;
            }

            cancellation.Cancel();
            await client.StopAsync().ConfigureAwait(false);
            return exerciseExit;
        }

        return await RunInputAsync(
                client, control, rich, writeLine, console.SuggestionsEnabled, remoteStop, cancellation)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// The input loop, reached with a live session, with one that failed to start, and with one that was never dialled at all: the scripted file-input drive when it is enabled, otherwise the interactive REPL.
    /// Both stop on exit or quit, and the file-input drive also stops when the server ends the session under it.
    /// </summary>
    private static async Task<int> RunInputAsync(
        Client client,
        HostControl control,
        bool rich,
        Action<string> writeLine,
        bool suggestionsEnabled,
        TaskCompletionSource remoteStop,
        CancellationTokenSource cancellation)
    {
        if (FileInputDriver.IsEnabled)
        {
            // The driver returns on a bare quit/exit line, but "exit <code>" is a real command that routes
            // through the dispatcher and leaves the driver tailing, so the run has to watch the control too
            // or a scripted "exit 3" never terminates the process.
            var exitRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task inputTask = Task.Run(
                () => FileInputDriver.RunAsync(
                    async line =>
                    {
                        await ProcessLineAsync(client, line, writeLine, cancellation.Token).ConfigureAwait(false);
                        if (control.ExitRequested)
                            exitRequested.TrySetResult();
                    },
                    cancellation.Token));
            Task completed = await Task.WhenAny(inputTask, exitRequested.Task, remoteStop.Task).ConfigureAwait(false);
            if (completed == remoteStop.Task)
                // The reason itself is printed by the StatusChanged handler above, for every drive.
                return ExitConnectionLost;

            cancellation.Cancel();
            await client.StopAsync().ConfigureAwait(false);
            return HostExit.Resolve(control);
        }

        // Interactive REPL: survives remote disconnects (offline prompt: reco/connect still route through the command service) and stops on exit/quit or stdin EOF.
        await RunReplAsync(client, control, rich, writeLine, suggestionsEnabled, cancellation.Token).ConfigureAwait(false);
        cancellation.Cancel();
        await client.StopAsync().ConfigureAwait(false);
        return HostExit.Resolve(control);
    }

    private static async Task RunReplAsync(
        Client client, HostControl control, bool rich, Action<string> writeLine, bool suggestionsEnabled, CancellationToken ct)
    {
        if (rich)
        {
            await RichConsole
                .RunAsync(client, control, line => ProcessLineAsync(client, line, writeLine, ct), suggestionsEnabled, ct)
                .ConfigureAwait(false);
            return;
        }

        while (!control.ExitRequested && !ct.IsCancellationRequested)
        {
            string? line = Console.ReadLine();
            if (line is null)
                return; // stdin EOF: treat as a clean quit

            if (line.Length == 0)
                continue;

            await ProcessLineAsync(client, line, writeLine, ct).ConfigureAwait(false);
            if (control.ExitRequested)
                return;
        }
    }

    private static async Task ProcessLineAsync(
        Client client, string line, Action<string> writeLine, CancellationToken ct)
    {
        try
        {
            InputRouting routing = await client.Commands.HandleInputAsync(line, ct).ConfigureAwait(false);
            if (routing.Result is { Message: { Length: > 0 } message } result
                && routing.Action is InputAction.CommandExecuted or InputAction.NotConnected)
            {
                // Decorated by STATUS, not printed raw: a success and a refusal used to arrive identically.
                // Formatted, not the plain sink, because the result message carries section codes just as the body does (see HostConsole.WriteFormatted) and the decoration adds more.
                HostConsole.WriteFormatted(
                    ResultFormatting.Decorate(result.Status, message, client.Commands.Glyphs) ?? message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            HostConsole.WriteErrorLine(Strings.Host(Strings.SendFailed(ex.Message)));
        }
    }

    /// <summary>
    /// Renders a disconnect for the console.
    /// The wording, including the kick case that now carries the server's own reason, lives once in <see cref="DisconnectDescription.Describe(DisconnectInfo?, bool)"/> so the CLI, the TUI and an embedding host cannot drift apart; the only CLI-specific part is the diagnostic opt-in that prints the whole fault chain instead of just its message.
    /// </summary>
    private static string Describe(DisconnectInfo? info, AnsiComponentRenderer renderer)
        => DisconnectDescription.Describe(
            info,
            includeFaultDetail: Environment.GetEnvironmentVariable("MCC_CLI_FAULT_DETAIL") == "1",
            renderMessage: renderer.Render);

    /// <summary>
    /// Renders a <see cref="LoginRejectedException"/>'s server-supplied reason for the console: the structured <see cref="Umpk.Text.Component"/> when the server sent one, rendered through the client's negotiated-era translations, else the exception's own message.
    /// <para>
    /// Through the ANSI renderer rather than <c>ToPlainText</c>, for the same reason the disconnect line is: a login-phase rejection is the same server text as a kick and carries the same colours, either as component style or as literal section-sign codes, and flattening printed the codes as characters.
    /// </para>
    /// </summary>
    private static string DescribeLoginRejection(AnsiComponentRenderer renderer, LoginRejectedException ex)
        => ex.Reason is { } reason && renderer.Render(reason).Trim() is { Length: > 0 } rendered
            ? rendered
            : ex.Message;

    /// <summary>
    /// Prints the classic startup banner, gated on the Display_Icon_Banner setting in console.toml (classic mode has no icon, so the toggle gates this text banner instead).
    /// Mirrors the legacy ShowClassicBanner / Translations.mcc_banner_classic: the client version and the MC version range this build supports (from UMPK's version catalog).
    /// Light gray; the project URL lives on the star nudge that follows instead.
    /// </summary>
    private static void PrintClassicBanner(ConsoleHostConfig console, ConsoleColorDepth depth)
    {
        if (!console.DisplayIconBanner)
            return;

        var versions = JavaVersions.All;
        string lowVersion = versions[0].Version.Name;
        string highVersion = versions[^1].Version.Name;
        HostConsole.WriteErrorLine(Strings.Host(Strings.ClassicBanner(
            ClientVersion.Current, lowVersion, highVersion, depth)));
    }

    /// <summary>
    /// Sets the console window title.
    /// The Console.Title setter is Windows-only supported, guarded exactly like the legacy Program.cs. Expands the legacy %username%/%serverip% placeholders in the ConsoleTitle template in console.toml.
    /// </summary>
    private static void UpdateConsoleTitle(string template, string username, string host)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(template))
            return;

        Console.Title = template
            .Replace("%username%", username, StringComparison.Ordinal)
            .Replace("%serverip%", host, StringComparison.Ordinal);
    }
}
