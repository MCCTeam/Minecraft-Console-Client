using Mcc.Cli.Input;
using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Plugins;

/// <summary>
/// How this host asks the plugin market's install question.
/// The market renders the confirmation itself, so all three paths print the same text and differ only in where the answer comes from:
/// <list type="bullet">
///   <item><description>the TUI reads it on its input line, the way the auth prompts do;</description></item>
/// <item><description>
/// the classic console takes the next line from the reader that already owns the keyboard, so nothing races the ConsoleInteractive read thread;
/// </description></item>
/// <item><description>
/// a run with no interactive input declines and names the keyword that would have answered, because nobody is there to ask.
/// </description></item>
/// </list>
/// </summary>
internal static class PluginInstallPrompt
{
    /// <summary>The callback for this host: the TUI's when a backend is running, else the console's.</summary>
    internal static Func<InstallConfirmation, CancellationToken, ValueTask<bool>> For(TuiBackend? backend)
        => backend is null ? AskConsoleAsync : (confirmation, ct) => AskTuiAsync(backend, confirmation, ct);

    private static async ValueTask<bool> AskTuiAsync(
        TuiBackend backend, InstallConfirmation confirmation, CancellationToken ct)
    {
        backend.WriteLine(confirmation.Text);
        backend.WriteLine(Strings.Host(Strings.PluginInstallQuestion));
        string answer = await backend.RequestLineAsync(ct).ConfigureAwait(false);
        return IsYes(answer);
    }

    private static async ValueTask<bool> AskConsoleAsync(InstallConfirmation confirmation, CancellationToken ct)
    {
        HostConsole.WriteLine(confirmation.Text);

        if (!RichConsole.CanRequestLine && (Console.IsInputRedirected || FileInputDriver.IsEnabled))
        {
            HostConsole.WriteLine(Strings.Host(Strings.PluginInstallNotInteractive));
            return false;
        }

        HostConsole.WriteLine(Strings.Host(Strings.PluginInstallQuestion));
        string? answer = RichConsole.CanRequestLine
            ? await RichConsole.RequestLineAsync(ct).ConfigureAwait(false)
            : await Task.Run(Console.ReadLine, ct).ConfigureAwait(false);
        return IsYes(answer);
    }

    /// <summary>Anything but an explicit yes is a no.</summary>
    private static bool IsYes(string? answer)
    {
        string value = (answer ?? string.Empty).Trim();
        return value.Equals("y", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
