using System.Diagnostics;

namespace Mcc.Cli.Tui.Terminal;

/// <summary>
/// Opens a URL in the platform default browser: ShellExecute on Windows, <c>open</c> on macOS, <c>xdg-open</c> elsewhere.
/// Returns false instead of throwing (a kiosk or container with no browser installed, a hardened sandbox), so callers can fall back to showing the link.
/// </summary>
internal static class BrowserLauncher
{
    internal static bool TryOpen(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        try
        {
            if (OperatingSystem.IsWindows())
            {
                using Process? _ = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsMacOS())
            {
                using Process? _ = Process.Start("open", url);
            }
            else
            {
                using Process? _ = Process.Start("xdg-open", url);
            }

            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
            or FileNotFoundException or UnauthorizedAccessException or InvalidOperationException)
        {
            return false;
        }
    }
}
