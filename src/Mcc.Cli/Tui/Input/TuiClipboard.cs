using Mcc.Cli.Tui.Hosting;
using System.Diagnostics;
using Avalonia.Controls;

namespace Mcc.Cli.Tui.Input;

/// <summary>
/// Copies text to the system clipboard, best-effort: false means "show the value instead", never an exception out of a button handler.
/// Two layers, in order:
/// <list type="number">
/// <item><description>
/// Avalonia's clipboard (Consolonia implements it over OSC 52): no subprocess, and the only layer that can work over SSH.
/// </description></item>
/// <item><description>
/// Native tools via stdin, per platform: <c>clip</c> on Windows, <c>pbcopy</c> on macOS, and on Linux <c>wl-copy</c> on Wayland else <c>xclip</c>/<c>xsel</c> (each also tried as fallback, since XWayland mixes both).
/// A missing tool fails fast; a hanging one is killed after a bounded wait.
/// </description></item>
/// </list>
/// </summary>
internal static class TuiClipboard
{
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(5);

    internal static async Task<bool> TryCopyAsync(TuiBackend backend, string text)
    {
        ArgumentNullException.ThrowIfNull(backend);
        if (string.IsNullOrEmpty(text))
            return false;

        if (await TryCopyAvaloniaAsync(backend, text).ConfigureAwait(false))
            return true;

        // Subprocesses block; never on the caller's thread (dialog loops run off-UI, but stay safe).
        return await TryCopyNativeAsync(text).ConfigureAwait(false);
    }

    /// <summary>Tries the native tools only, off the caller's thread. Test seam: needs no UI.</summary>
    internal static Task<bool> TryCopyNativeAsync(string text)
        => Task.Run(() => TryCopyNative(text, Candidates()));

    /// <summary>Tries explicit candidates so tests do not depend on installed clipboard tools.</summary>
    internal static Task<bool> TryCopyNativeAsync(string text, IReadOnlyList<(string File, string Args)> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return Task.Run(() => TryCopyNative(text, candidates));
    }

    private static async Task<bool> TryCopyAvaloniaAsync(TuiBackend backend, string text)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.Post(() =>
        {
            try
            {
                TopLevel? top = backend.View is { } view ? TopLevel.GetTopLevel(view) : null;
                if (top?.Clipboard is not { } clipboard)
                {
                    completion.TrySetResult(false);
                    return;
                }

                clipboard.SetTextAsync(text).ContinueWith(
                    copy => completion.TrySetResult(copy.Status == TaskStatus.RanToCompletion),
                    TaskScheduler.Default);
            }
            catch (Exception)
            {
                // No clipboard on this platform, or it refused: fall through to native tools.
                completion.TrySetResult(false);
            }
        });

        try
        {
            return await completion.Task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryCopyNative(string text, IReadOnlyList<(string File, string Args)> candidates)
    {
        foreach ((string file, string args) in candidates)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = file,
                        Arguments = args,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };

                process.Start();
                process.StandardInput.Write(text);
                process.StandardInput.Close();

                if (!process.WaitForExit((int)ToolTimeout.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (Exception)
                    {
                        // Already exiting underneath us; the wait below still reaps it.
                    }

                    process.WaitForExit();
                    continue;
                }

                if (process.ExitCode == 0)
                    return true;
            }
            catch (Exception)
            {
                // Missing tool, no permission, refused input: try the next candidate.
            }
        }

        return false;
    }

    /// <summary>The native copy tools to try, in order, for the given platform and session type.</summary>
    internal static IReadOnlyList<(string File, string Args)> Candidates(
        bool windows, bool macos, bool wayland, bool x11)
    {
        if (windows)
            return [("clip", "")];

        if (macos)
            return [("pbcopy", "")];

        var candidates = new List<(string, string)>();
        if (wayland)
            candidates.Add(("wl-copy", ""));

        candidates.Add(("xclip", "-selection clipboard"));
        candidates.Add(("xsel", "--clipboard --input"));

        if (!wayland)
            candidates.Add(("wl-copy", ""));

        // An X11 session may still reach a Wayland compositor (and vice versa under XWayland), so every tool is attempted regardless of which display variable is set.
        if (!x11)
        {
            candidates.Add(("xclip", "-selection clipboard"));
            candidates.Add(("xsel", "--clipboard --input"));
        }

        return [.. candidates.Distinct()];
    }

    /// <summary>The native copy tools for this process, from the OS and the display session.</summary>
    internal static IReadOnlyList<(string File, string Args)> Candidates()
        => Candidates(
            OperatingSystem.IsWindows(),
            OperatingSystem.IsMacOS(),
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")),
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")));
}
