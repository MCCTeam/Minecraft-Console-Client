using Umpk.Client;

namespace Mcc.Cli.Hosting;

/// <summary>
/// The process exit-code contract, in one place for both hosts.
/// The classic host and the TUI host each used to carry their own copy of the constants and their own end-of-run expression, which is how they came to disagree about the one case below.
/// </summary>
internal static class HostExit
{
    /// <summary>The run ended the way it was asked to.</summary>
    public const int Clean = 0;

    /// <summary>The command line or the configuration could not be used.</summary>
    public const int Usage = 1;

    /// <summary>The server version could not be resolved.</summary>
    public const int VersionResolution = 2;

    /// <summary>The connection was lost, or never established.</summary>
    public const int ConnectionLost = 3;

    /// <summary>The login was rejected (auth failure or a server-side refusal).</summary>
    public const int LoginRejected = 4;

    /// <summary>
    /// Folds one lifecycle transition into the exit-code state.
    /// Shared by both hosts so the classic host and the TUI cannot answer a server-side kill differently, which is exactly how they drifted before.
    /// <para>
    /// A remote stop is recorded, and a session that comes back clears it: <c>reco</c> after a kick, or the auto-reconnect supervisor re-joining, both mean the client really is connected again, and quitting from there is a clean 0.
    /// </para>
    /// </summary>
    public static void ObserveStatus(HostControl control, ClientStatusChangedEventArgs status)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(status);

        if (status.Current == ClientStatus.Playing)
            control.MarkSessionLive();
        else if (status.Current == ClientStatus.Disconnected && status.Disconnect is { WasLocal: false })
            control.MarkSessionEndedRemotely();
    }

    /// <summary>
    /// The exit code for a run that is finishing.
    /// <para>
    /// The rule, in order:
    /// <list type="number">
    /// <item>An explicit <c>exit &lt;code&gt;</c> always wins. The user named a number; honour it.</item>
    /// <item>
    /// Otherwise, a session the server ended and that never came back is <see cref="ConnectionLost"/>.
    /// This is the case that was wrong: a bare <c>quit</c> after the server had killed the session returned <see cref="Clean"/>, so a caller scripting against the exit codes could not tell a healthy session from a corpse.
    /// It is deliberately NOT latched: a session that was lost and then re-established by <c>reco</c>/<c>connect</c> clears it, because by the time the user quits the client really is connected and 0 is the truth.
    /// </item>
    /// <item>
    /// Otherwise a bare <c>exit</c>/<c>quit</c> yields its (zero) code, and a run that ended for some other reason yields the caller's <paramref name="fallback"/>.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="control">The host control the exit command and the status handler write to.</param>
    /// <param name="fallback">The code for a run that ended without an exit command being issued.</param>
    public static int Resolve(HostControl control, int fallback = Clean)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (control.ExitCodeExplicit)
            return control.ExitCode;

        if (control.SessionEndedRemotely)
            return ConnectionLost;

        return control.ExitRequested ? control.ExitCode : fallback;
    }
}
