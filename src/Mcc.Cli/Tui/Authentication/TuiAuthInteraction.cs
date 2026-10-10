using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Input;
using Mcc.Cli.Tui.Terminal;
using Umpk.Auth;

namespace Mcc.Cli.Tui.Authentication;

/// <summary>
/// The TUI implementation of UMPK's <see cref="IAuthInteraction"/>: device-code and browser sign-in steps surface as dialogs with fields and buttons (<see cref="TuiPrompt"/>), never as typed commands or input-line questions.
/// Offline accounts never use it.
/// <para>
/// The device and browser steps are also written to the TUI log, so dismissing their dialogs loses nothing: the code and the URL stay in the scrollback.
/// The Yggdrasil credential prompt is dialog-only (a log line could keep no answer worth keeping).
/// A cancelled dialog answers empty (the same shape the classic host's end-of-input produces), which fails the sign-in with the provider's own error rather than hanging the session.
/// </para>
/// </summary>
internal sealed class TuiAuthInteraction : IAuthInteraction
{
    private const string CodeKey = "code";
    private const string UsernameKey = "username";
    private const string PasswordKey = "password";

    private readonly TuiBackend _backend;

    public TuiAuthInteraction(TuiBackend backend) => _backend = backend;

    public Task ShowDeviceCodeAsync(DeviceCodePrompt prompt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        // Display only: the flow polls in the background, so the dialog must not be awaited (waiting for a dismissal before polling would stall the sign-in past the code's expiry).
        // It stays up until dismissed or replaced, and the log line below outlives both.
        string url = prompt.VerificationUri.AbsoluteUri;
        _backend.WriteLine(Strings.DeviceCode(prompt.UserCode, url));
        _ = ShowDeviceDialogAsync(prompt.UserCode, url, ct);
        return Task.CompletedTask;
    }

    public async Task<string> GetBrowserAuthCodeAsync(Uri signInUrl, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(signInUrl);
        _backend.WriteLine(Strings.BrowserSignIn(signInUrl.ToString()));

        TuiPromptResult? answer = await TuiPrompt.AskAsync(
            _backend,
            Strings.TuiAuthBrowserTitle,
            [.. Strings.BrowserSignIn(signInUrl.ToString()).Split('\n')],
            [new TuiPromptField(CodeKey, Strings.TuiFieldCode)],
            [Strings.TuiPromptContinue, Strings.TuiPromptCancel],
            ct).ConfigureAwait(false);
        if (answer is null || answer.ButtonIndex != 0)
            return string.Empty;

        return answer.Values.TryGetValue(CodeKey, out string? code) ? code.Trim() : string.Empty;
    }

    public async Task<YggdrasilCredentials> GetYggdrasilCredentialsAsync(CancellationToken ct)
    {
        TuiPromptResult? answer = await TuiPrompt.AskAsync(
            _backend,
            Strings.TuiAuthYggdrasilTitle,
            [],
            [
                new TuiPromptField(UsernameKey, Strings.TuiFieldUsername),
                new TuiPromptField(PasswordKey, Strings.TuiFieldPassword, Secret: true),
            ],
            [Strings.TuiPromptSignIn, Strings.TuiPromptCancel],
            ct).ConfigureAwait(false);
        if (answer is null || answer.ButtonIndex != 0)
            return new YggdrasilCredentials(string.Empty, string.Empty);

        string username = answer.Values.TryGetValue(UsernameKey, out string? name) ? name.Trim() : string.Empty;
        string password = answer.Values.TryGetValue(PasswordKey, out string? secret) ? secret : string.Empty;
        return new YggdrasilCredentials(username, password);
    }

    // The link dialog: the code as the hero, side-effect buttons beside the dismissal.
    // Opening hands off to the browser and dismisses (the log line keeps the code); copying reshows with a status so the code stays on screen either way.
    private async Task ShowDeviceDialogAsync(string code, string url, CancellationToken ct)
    {
        string? status = null;
        while (true)
        {
            List<string> body = [Strings.TuiAuthDeviceBody, url];
            if (status is not null)
                body.Add(status);

            TuiPromptResult? answer;
            try
            {
                answer = await TuiPrompt.AskAsync(
                    _backend,
                    Strings.TuiAuthDeviceTitle,
                    body,
                    [],
                    [Strings.TuiPromptOpenBrowser, Strings.TuiPromptCopyCode, Strings.TuiPromptOk],
                    ct,
                    heroText: code).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Dismissed by shutdown: nothing to answer, nothing to report.
                return;
            }

            if (answer is null || answer.ButtonIndex == 2)
                return;

            if (answer.ButtonIndex == 0)
            {
                if (BrowserLauncher.TryOpen(url))
                    return;

                status = Strings.TuiAuthBrowserFailed;
                continue;
            }

            status = await TuiClipboard.TryCopyAsync(_backend, code).ConfigureAwait(false)
                ? Strings.TuiAuthCopied
                : Strings.TuiAuthCopyFailed;
        }
    }
}
