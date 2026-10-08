using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli.Startup;
using System.Text;
using Umpk.Auth;
using Umpk.Text;

namespace Mcc.Cli.Hosting.Classic;

/// <summary>
/// The classic-console implementation of UMPK's <see cref="IAuthInteraction"/>: it prints the device code and URL, prints the browser sign-in URL and reads the pasted redirect code, and prompts for Yggdrasil credentials on stdin.
/// All user-facing text flows through <see cref="Strings"/>.
/// <para>
/// Every line goes through <see cref="HostConsole"/>, never raw <see cref="Console"/>.
/// A raw write while the rich reader owns the terminal skips <c>ConsoleWriter</c>'s input-area handling, so the live <c>"> "</c> prompt is left on screen and the output lands after it on the same line (<c>"> [MCC] To sign in, ..."</c>), and the line never reaches the suggestion popup's recent-message ring, drifting every later restore.
/// Prompts are full lines (the answer goes on the next line, the same shape <see cref="GuidedLogin"/> already uses) because the writer only writes whole lines.
/// </para>
/// </summary>
internal sealed class ConsoleAuthInteraction(ConsoleColorDepth colorDepth) : IAuthInteraction
{
    public Task ShowDeviceCodeAsync(DeviceCodePrompt prompt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        // The code is the one thing the user has to copy out of this line and type somewhere else, and it used to sit in the middle of a sentence in the same colour as the rest of it.
        // Yellow makes it findable in a console that is also printing connect logs around it.
        HostConsole.WriteLine(Strings.Host(Strings.DeviceCode(
            Ansi.Colorize(prompt.UserCode, TextColor.Yellow, colorDepth),
            prompt.VerificationUri.ToString())));
        return Task.CompletedTask;
    }

    public async Task<string> GetBrowserAuthCodeAsync(Uri signInUrl, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(signInUrl);
        HostConsole.WriteLine(Strings.Host(Strings.BrowserSignIn(signInUrl.ToString())));
        HostConsole.WriteLine(Strings.Host(Strings.BrowserCodePrompt));
        string code = await ReadLineAsync(ct).ConfigureAwait(false);
        return code.Trim();
    }

    public async Task<YggdrasilCredentials> GetYggdrasilCredentialsAsync(CancellationToken ct)
    {
        HostConsole.WriteLine(Strings.Host(Strings.YggdrasilUsernamePrompt));
        string username = (await ReadLineAsync(ct).ConfigureAwait(false)).Trim();
        HostConsole.WriteLine(Strings.Host(Strings.YggdrasilPasswordPrompt));
        string password = await ReadPasswordAsync(ct).ConfigureAwait(false);
        return new YggdrasilCredentials(username, password);
    }

    // Console.ReadLine blocks; run it off the calling path so cancellation can abandon the await.
    private static async Task<string> ReadLineAsync(CancellationToken ct)
    {
        string? line = await Task.Run(Console.ReadLine, ct).ConfigureAwait(false);
        return line ?? string.Empty;
    }

    /// <summary>
    /// Reads the password line with an asterisk echo instead of plaintext.
    /// The rich ConsoleInteractive reader is not running at this point in startup (it only starts once the REPL begins, well after this auth prompt returns), and spinning it up just for one password prompt would be more machinery than the read is worth, so this reads raw keys directly instead.
    /// Falls back to a plain (unmasked) line read when input is redirected, since <see cref="Console.ReadKey()"/> requires a real console and throws under redirection.
    /// </summary>
    private static async Task<string> ReadPasswordAsync(CancellationToken ct)
    {
        if (Console.IsInputRedirected)
            return await ReadLineAsync(ct).ConfigureAwait(false);

        return await Task.Run(() => ReadMaskedLine(ct), ct).ConfigureAwait(false);
    }

    private static string ReadMaskedLine(CancellationToken ct)
    {
        var buffer = new StringBuilder();
        while (true)
        {
            if (ct.IsCancellationRequested)
                return string.Empty;

            if (!Console.KeyAvailable)
            {
                Thread.Sleep(15);
                continue;
            }

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}
