using Mcc.Cli.Tui.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Mcc.Cli.Hosting;

/// <summary>
/// Requests a clean shutdown when the process ends from outside the normal Ctrl+C path: the console window closing, a logoff/shutdown (Windows), or a SIGTERM/SIGHUP (Linux/macOS).
/// Legacy parity for <c>MinecraftClient/WinAPI/ExitCleanUp.cs</c>, minus the mono ctrl+c-only fallback: this client targets .NET directly, not mono.
/// Ctrl+C itself is already handled by <c>Console.CancelKeyPress</c> in <c>Program.cs</c>/<c>TuiHost</c>; this covers only the additional close signals legacy also caught, and it stays small on purpose: one best-effort callback, not a cleanup framework.
/// </summary>
internal static class ProcessCloseGuard
{
    // CTRL_CLOSE_EVENT / CTRL_LOGOFF_EVENT / CTRL_SHUTDOWN_EVENT, matching legacy's CtrlType.
    // CTRL_C_EVENT (0) and CTRL_BREAK_EVENT (1) are deliberately not handled here: Console.CancelKeyPress already owns those.
    private const int CtrlCloseEvent = 2;
    private const int CtrlLogoffEvent = 5;
    private const int CtrlShutdownEvent = 6;

    private static Action? _onClose;

    // Kept alive for the process lifetime: SetConsoleCtrlHandler does not root the delegate on its own, and an unrooted delegate can be collected out from under the native callback.
    private static HandlerRoutine? _windowsHandler;

    // Retained so the registrations themselves are not eligible for collection before the process exits.
    private static readonly List<PosixSignalRegistration> PosixRegistrations = [];

    /// <summary>
    /// Registers the action to run once, best-effort, on a window close/logoff/shutdown/SIGTERM/SIGHUP.
    /// The action may block briefly (the OS gives a short grace window before forcing termination); exceptions are swallowed since the process is already going down.
    /// Call once per process.
    /// </summary>
    public static void Register(Action onClose)
    {
        ArgumentNullException.ThrowIfNull(onClose);
        _onClose = onClose;

        if (OperatingSystem.IsWindows())
            RegisterWindows();
        else
            RegisterPosix();
    }

    private static void Invoke()
    {
        try
        {
            Interlocked.Exchange(ref _onClose, null)?.Invoke();
        }
        catch
        {
            // Best-effort: the process is already going down, there is nothing left to report this to.
        }
    }

    [SupportedOSPlatform("windows")]
    private static void RegisterWindows()
    {
        _windowsHandler = ctrlType =>
        {
            if (ctrlType is CtrlCloseEvent or CtrlLogoffEvent or CtrlShutdownEvent)
                Invoke();

            return false; // let the OS continue its own default handling (matches legacy's CleanUp).
        };

        NativeMethods.SetConsoleCtrlHandler(_windowsHandler, true);
    }

    private static void RegisterPosix()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Invoke();

        PosixRegistrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx =>
        {
            Invoke();
            ctx.Cancel = false; // run cleanup, then let the default termination proceed.
        }));
        PosixRegistrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGHUP, ctx =>
        {
            Invoke();
            ctx.Cancel = false;
        }));
    }

    private delegate bool HandlerRoutine(int ctrlType);

    [SupportedOSPlatform("windows")]
    private static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, [MarshalAs(UnmanagedType.Bool)] bool add);
    }
}
