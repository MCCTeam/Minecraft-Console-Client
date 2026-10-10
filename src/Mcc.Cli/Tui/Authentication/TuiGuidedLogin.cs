using Mcc.Cli.Localization;
using Mcc.Cli.Startup;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Input;
using Mcc.Cli.Tui.Terminal;
using DMCBK.Core;
using DMCBK.Core.Configuration;
using DMCBK.Core.Localization;
using Umpk.Auth;

namespace Mcc.Cli.Tui.Authentication;

/// <summary>
/// The answered TUI login: the chosen account, plus the resolved profile when Microsoft was signed in up front (so the caller skips the deferred-sign-in notice).
/// </summary>
/// <param name="Account">Null when the login was cancelled.</param>
/// <param name="PreAuthedProfile">The signed-in profile name, or null when sign-in is still deferred.</param>
internal sealed record TuiLoginChoice(ConfiguredAccount? Account, string? PreAuthedProfile);

/// <summary>
/// The TUI first-run login: the same three account kinds <see cref="GuidedLogin"/> offers on the classic console, asked with dialogs (buttons and fields) instead of printed questions.
/// It runs after the backend is up, so everything here happens inside the TUI and nothing leaks to the plain console underneath.
/// <para>
/// The same rules as the console flow, so the two cannot disagree: offline validates the name the same way (<see cref="GuidedLogin.TryCleanOfflineName"/>), Yggdrasil asks only for the provider URL (credentials belong to the auth interaction at login time).
/// Microsoft signs in NOW, through the same device flow a later connect would run, against the same token store the session will read: picking it shows the link/code dialog immediately, and the later connect resumes the cached session with no second prompt.
/// Nothing is written to accounts.toml here; the caller persists the choice only after a login actually succeeds.
/// </para>
/// </summary>
internal static class TuiGuidedLogin
{
    private const string UsernameKey = "username";
    private const string ProviderUrlKey = "url";

    /// <summary>
    /// Runs the prompt.
    /// Returns the chosen account, or null when it was cancelled (Escape) or the view is not up.
    /// Cancellation throws <see cref="OperationCanceledException"/>.
    /// </summary>
    /// <param name="probe">Checks a normalized provider URL; defaults to a live HTTP probe.</param>
    public static async Task<TuiLoginChoice> PromptAsync(
        TuiBackend backend,
        DmcbkConfiguration config,
        Func<Uri, CancellationToken, Task<AuthServerProbeResult>>? probe = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(config);
        probe ??= AuthServerProbe.ProbeAsync;

        // Back from a sub-dialog returns here (the method picker), not out: there is nowhere earlier to go, while cancelling (Escape) gives up the whole login.
        while (true)
        {
            TuiPromptResult? method = await TuiPrompt.AskAsync(
                backend,
                Strings.TuiLoginTitle,
                [],
                [],
                [Strings.TuiLoginOffline, Strings.TuiLoginOnline, Strings.TuiLoginYggdrasil],
                ct).ConfigureAwait(false);
            if (method is null)
                return new TuiLoginChoice(null, null);

            SubResult sub = method.ButtonIndex switch
            {
                0 => await PromptOfflineAsync(backend, ct).ConfigureAwait(false),
                1 => await PromptMicrosoftAsync(backend, config, ct).ConfigureAwait(false),
                _ => await PromptYggdrasilAsync(backend, probe, ct).ConfigureAwait(false),
            };

            if (!sub.Back)
                return new TuiLoginChoice(sub.Account, sub.PreAuthedProfile);
        }
    }

    /// <summary>
    /// A sub-dialog's answer: the account, Back to the method picker, and the signed-in profile when Microsoft authenticated up front.
    /// </summary>
    private sealed record SubResult(ConfiguredAccount? Account, bool Back, string? PreAuthedProfile = null);

    private static ConfiguredAccount PlaceholderMicrosoft() => new()
    {
        Name = GuidedLogin.MicrosoftPlaceholderName,
        Kind = DmcbkAccountKind.MicrosoftDeviceCode,
    };

    private static async Task<SubResult> PromptOfflineAsync(TuiBackend backend, CancellationToken ct)
    {
        string? error = null;
        while (true)
        {
            TuiPromptResult? answer = await TuiPrompt.AskAsync(
                backend,
                Strings.TuiLoginUsernameTitle,
                error is null ? [] : [error],
                [new TuiPromptField(UsernameKey, Strings.TuiFieldUsername)],
                [Strings.TuiPromptContinue, Strings.TuiPromptBack],
                ct).ConfigureAwait(false);
            if (answer is null)
                return new SubResult(null, Back: false);

            if (answer.ButtonIndex == 1)
                return new SubResult(null, Back: true);

            if (GuidedLogin.TryCleanOfflineName(
                answer.Values.TryGetValue(UsernameKey, out string? raw) ? raw : null, out string clean))
            {
                return new SubResult(
                    new ConfiguredAccount { Name = clean, Kind = DmcbkAccountKind.Offline, Login = clean },
                    Back: false);
            }

            error = DMCBK.Core.Localization.McStrings.mcc_auth_method_offline_name_invalid;
        }
    }

    private static async Task<SubResult> PromptYggdrasilAsync(
        TuiBackend backend,
        Func<Uri, CancellationToken, Task<AuthServerProbeResult>> probe,
        CancellationToken ct)
    {
        string? error = null;
        while (true)
        {
            TuiPromptResult? answer = await TuiPrompt.AskAsync(
                backend,
                Strings.TuiLoginYggdrasilTitle,
                error is null ? [] : [error],
                [new TuiPromptField(ProviderUrlKey, Strings.TuiFieldProviderUrl)],
                [Strings.TuiPromptContinue, Strings.TuiPromptBack],
                ct).ConfigureAwait(false);
            if (answer is null)
                return new SubResult(null, Back: false);

            if (answer.ButtonIndex == 1)
                return new SubResult(null, Back: true);

            string url = answer.Values.TryGetValue(ProviderUrlKey, out string? raw) ? raw : string.Empty;
            if (!AuthServerProbe.TryNormalize(url, out Uri? provider))
            {
                error = DMCBK.Core.Localization.McStrings.mcc_yggdrasil_invalid_url;
                continue;
            }

            switch (await probe(provider, ct).ConfigureAwait(false))
            {
                case AuthServerProbeResult.Valid:
                    return new SubResult(
                        new ConfiguredAccount
                        {
                            Name = GuidedLogin.YggdrasilPlaceholderName,
                            Kind = DmcbkAccountKind.Yggdrasil,
                            AuthServer = provider.AbsoluteUri,
                        },
                        Back: false);
                case AuthServerProbeResult.Unreachable:
                    error = DMCBK.Core.Localization.McStrings.mcc_yggdrasil_server_unreachable;
                    break;
                default:
                    error = DMCBK.Core.Localization.McStrings.mcc_yggdrasil_server_invalid;
                    break;
            }
        }
    }

    /// <summary>
    /// Microsoft signs in now, through the same device flow (and the same token store) a later connect would use: the link/code dialog appears immediately, and the connect resumes the cached session with no second prompt.
    /// Without a disk cache there is nothing a later connect could resume, so the choice stays deferred exactly like the classic host.
    /// </summary>
    private static async Task<SubResult> PromptMicrosoftAsync(
        TuiBackend backend, DmcbkConfiguration config, CancellationToken ct)
    {
        if (config.Accounts.SessionCache != CacheMode.Disk || config.SourceFolder is null)
            return new SubResult(PlaceholderMicrosoft(), Back: false);

        using var flow = new MinecraftAuthFlow(new MinecraftAuthOptions
        {
            FlowKind = AuthFlowKind.MicrosoftDeviceCode,
            TokenStore = CreateTokenStore(config),
        });

        // Fast path first: a cached (or refreshable) session signs in with no dialog at all.
        try
        {
            JavaSession? resumed = await flow.TryResumeAsync(
                GuidedLogin.MicrosoftPlaceholderName, ct).ConfigureAwait(false);
            if (resumed is not null)
            {
                backend.WriteLine(Strings.Host(Strings.TuiLoginSignedIn(resumed.Profile.Name)));
                return new SubResult(PlaceholderMicrosoft(), Back: false, PreAuthedProfile: resumed.Profile.Name);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // A corrupt store or a dead network: fall through to the interactive flow, which reports with a Back button instead of dying here.
        }

        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var capture = new DevicePromptCapture();
        Task<JavaSession> loginTask = flow.LoginAsync(
            capture, pollCts.Token, GuidedLogin.MicrosoftPlaceholderName);
        using var dialogCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            // Until the device prompt surfaces, a finished login means failure before asking anything.
            Task first = await Task.WhenAny(loginTask, capture.Prompt.Task).ConfigureAwait(false);
            if (first == loginTask)
                return await ObservePrePromptOutcomeAsync(backend, loginTask, ct).ConfigureAwait(false);

            DeviceCodePrompt device = await capture.Prompt.Task.ConfigureAwait(false);
            string url = device.VerificationUri.AbsoluteUri;
            string? status = null;
            while (true)
            {
                List<string> body = [Strings.TuiAuthDeviceBody, url];
                if (status is not null)
                    body.Add(status);

                Task<TuiPromptResult?> dialogTask = TuiPrompt.AskAsync(
                    backend,
                    Strings.TuiAuthDeviceTitle,
                    body,
                    [],
                    [Strings.TuiPromptOpenBrowser, Strings.TuiPromptCopyCode, Strings.TuiPromptBack],
                    dialogCts.Token,
                    heroText: device.UserCode);
                Task done = await Task.WhenAny(loginTask, dialogTask).ConfigureAwait(false);
                if (done == loginTask)
                {
                    // The poll finished: drop a dialog nobody needs anymore, then read the verdict.
                    dialogCts.Cancel();
                    backend.Post(() => backend.View?.HideOverlay());
                    try
                    {
                        JavaSession session = await loginTask.ConfigureAwait(false);
                        backend.WriteLine(Strings.Host(Strings.TuiLoginSignedIn(session.Profile.Name)));
                        return new SubResult(PlaceholderMicrosoft(), Back: false, PreAuthedProfile: session.Profile.Name);
                    }
                    catch (OperationCanceledException)
                    {
                        if (ct.IsCancellationRequested)
                            throw;

                        // Back or Escape raced the poll ending: honor a Back press, else give up.
                        if (dialogTask.IsCompletedSuccessfully && dialogTask.Result is { ButtonIndex: 2 })
                            return new SubResult(null, Back: true);

                        return new SubResult(null, Back: false);
                    }
                    catch (Exception ex)
                    {
                        return await ShowAuthErrorAsync(backend, ex, ct).ConfigureAwait(false);
                    }
                }

                TuiPromptResult? answer;
                try
                {
                    answer = await dialogTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested)
                        throw;

                    continue;
                }

                if (answer is null)
                {
                    pollCts.Cancel();
                    await SwallowAsync(loginTask, ct).ConfigureAwait(false);
                    return new SubResult(null, Back: false);
                }

                if (answer.ButtonIndex == 2)
                {
                    pollCts.Cancel();
                    await SwallowAsync(loginTask, ct).ConfigureAwait(false);
                    return new SubResult(null, Back: true);
                }

                if (answer.ButtonIndex == 0)
                {
                    if (!BrowserLauncher.TryOpen(url))
                        status = Strings.TuiAuthBrowserFailed;

                    continue;
                }

                status = await TuiClipboard.TryCopyAsync(backend, device.UserCode).ConfigureAwait(false)
                    ? Strings.TuiAuthCopied
                    : Strings.TuiAuthCopyFailed;
            }
        }
        finally
        {
            dialogCts.Cancel();
        }
    }

    /// <summary>
    /// The login finished before surfacing any device prompt: success is impossible for a device flow (it always prompts), so this is a failure (or a cancellation).
    /// Observed, never abandoned.
    /// </summary>
    private static async Task<SubResult> ObservePrePromptOutcomeAsync(
        TuiBackend backend, Task<JavaSession> loginTask, CancellationToken ct)
    {
        try
        {
            JavaSession session = await loginTask.ConfigureAwait(false);

            // Defensive: a flow that returns without prompting still signed in.
            backend.WriteLine(Strings.Host(Strings.TuiLoginSignedIn(session.Profile.Name)));
            return new SubResult(PlaceholderMicrosoft(), Back: false, PreAuthedProfile: session.Profile.Name);
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested)
                throw;

            return new SubResult(null, Back: false);
        }
        catch (Exception ex)
        {
            return await ShowAuthErrorAsync(backend, ex, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Reports a failed sign-in with a way back to the method picker (Escape still cancels).</summary>
    private static async Task<SubResult> ShowAuthErrorAsync(
        TuiBackend backend, Exception error, CancellationToken ct)
    {
        TuiPromptResult? answer = await TuiPrompt.AskAsync(
            backend,
            Strings.TuiAuthDeviceTitle,
            [Strings.TuiAuthFailedHeading, error.Message],
            [],
            [Strings.TuiPromptBack],
            ct).ConfigureAwait(false);
        return new SubResult(null, Back: answer is not null);
    }

    /// <summary>Awaits an abandoned poll without reporting: the user is leaving anyway.</summary>
    private static async Task SwallowAsync(Task task, CancellationToken ct)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Our own cancel (Back/Escape): expected, nothing to say.
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // A poll fault at the exact moment of leaving: the error belongs to a flow nobody watches now, and the dialog that would have shown it is already gone.
        }
    }

    /// <summary>
    /// The same token-store derivation <see cref="ClientBuilder"/> uses for the session, or the later connect would miss the cache this run just filled.
    /// </summary>
    private static ITokenStore CreateTokenStore(DmcbkConfiguration config)
    {
        if (config.Accounts.SessionCache == CacheMode.Disk && config.SourceFolder is not null)
        {
            string directory = Path.Combine(config.SourceFolder, config.Accounts.CacheDirectory, "auth-tokens");
            return new FileTokenStore(directory, TokenProtectors.CreateDefault(), null);
        }

        return new InMemoryTokenStore();
    }

    /// <summary>
    /// Carries the flow's device prompt out to the dialog loop.
    /// Device-flow only: the other members never run for this kind and fail loudly if they ever do.
    /// </summary>
    private sealed class DevicePromptCapture : IAuthInteraction
    {
        public TaskCompletionSource<DeviceCodePrompt> Prompt { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ShowDeviceCodeAsync(DeviceCodePrompt prompt, CancellationToken ct)
        {
            Prompt.TrySetResult(prompt);
            return Task.CompletedTask;
        }

        public Task<string> GetBrowserAuthCodeAsync(Uri signInUrl, CancellationToken ct)
            => throw new InvalidOperationException("Device-flow capture cannot answer a browser prompt.");

        public Task<YggdrasilCredentials> GetYggdrasilCredentialsAsync(CancellationToken ct)
            => throw new InvalidOperationException("Device-flow capture cannot answer Yggdrasil credentials.");
    }
}
