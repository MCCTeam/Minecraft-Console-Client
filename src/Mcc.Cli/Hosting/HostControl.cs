using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Hosting;

/// <summary>
/// Shared exit signal the <c>exit</c>/<c>quit</c> command raises for the REPL loop, plus the one piece of session history the exit code depends on (see <see cref="HostExit.Resolve"/>).
/// Written from the client's status event, which runs on whatever thread ended the session, so every field is guarded.
/// </summary>
internal sealed class HostControl
{
    private readonly object _gate = new();
    private bool _exitRequested;
    private bool _exitCodeExplicit;
    private int _exitCode;
    private bool _endedRemotely;

    /// <summary>True once <c>exit</c>/<c>quit</c> has been issued.</summary>
    public bool ExitRequested
    {
        get { lock (_gate) return _exitRequested; }
    }

    /// <summary>The requested code; meaningful only with <see cref="ExitRequested"/>.</summary>
    public int ExitCode
    {
        get { lock (_gate) return _exitCode; }
    }

    /// <summary>
    /// True when the user named the code (<c>exit 3</c>) rather than taking the default.
    /// A bare <c>exit</c> and an explicit <c>exit 0</c> are otherwise indistinguishable, and they must not be: the first defers to what actually happened to the session, the second is an instruction.
    /// </summary>
    public bool ExitCodeExplicit
    {
        get { lock (_gate) return _exitCodeExplicit; }
    }

    /// <summary>True when the session was ended by the remote side and has not been re-established.</summary>
    public bool SessionEndedRemotely
    {
        get { lock (_gate) return _endedRemotely; }
    }

    /// <summary>Requests exit with the default (unnamed) code.</summary>
    public void RequestExit(int code) => RequestExit(code, explicitCode: false);

    /// <summary>Requests exit, recording whether the user named the code.</summary>
    public void RequestExit(int code, bool explicitCode)
    {
        lock (_gate)
        {
            _exitRequested = true;
            _exitCode = code;
            _exitCodeExplicit |= explicitCode;
        }
    }

    /// <summary>Records that the remote side ended the session.</summary>
    public void MarkSessionEndedRemotely()
    {
        lock (_gate)
            _endedRemotely = true;
    }

    /// <summary>Records that a session is live again (a reconnect cleared the loss).</summary>
    public void MarkSessionLive()
    {
        lock (_gate)
            _endedRemotely = false;
    }
}
