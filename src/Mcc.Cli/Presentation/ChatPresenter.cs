using Mcc.Cli.Logging;
using Umpk.Client.Events;
using Umpk.Text;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Renders and routes an inbound chat message for the classic host: Component -> ANSI via <see cref="AnsiComponentRenderer"/>, the signature-standing marker (<see cref="ChatStandingMarker"/>), an optional wall-clock timestamp (console.toml), the chat filter (<c>ChatFilterRegex</c> + <c>FilterMode</c>), the console write (through an injected sink so the plain and the ConsoleInteractive paths share this logic), and the optional log-file copy (when <c>ChatMessages</c> is on; the sink applies its own timestamp/color-strip policy).
/// The empty-render short-circuit preserves the legacy "drop empty chat" behavior.
/// <para>
/// The standing marker is a prefix, so it never alters the message text the chat filter matches against: a user regex written against the server's wording keeps working regardless of a message's signature.
/// </para>
/// </summary>
internal sealed class ChatPresenter
{
    private readonly AnsiComponentRenderer _renderer;
    private readonly bool _timestamps;
    private readonly LogFilter _chatFilter;
    private readonly LogFileSink? _fileSink;
    private readonly bool _logChat;
    private readonly Action<string> _write;
    private readonly ChatStandingMarker _standing;

    /// <summary>Builds a presenter. <paramref name="write"/> is the console line sink (plain or ConsoleInteractive).</summary>
    public ChatPresenter(
        AnsiComponentRenderer renderer,
        bool timestamps,
        LogFilter chatFilter,
        LogFileSink? fileSink,
        bool logChat,
        Action<string> write,
        ChatStandingMarker standing)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(chatFilter);
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(standing);
        _renderer = renderer;
        _timestamps = timestamps;
        _chatFilter = chatFilter;
        _fileSink = fileSink;
        _logChat = logChat;
        _write = write;
        _standing = standing;
    }

    /// <summary>Renders, marks, filters, prints, and (optionally) logs one inbound chat message.</summary>
    public void Print(ChatMessageReceived message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // The only standing that suppresses a message is a REJECTED signature with ShowIllegalSignedChat off, which is the legacy rule.
        // Unverified and insecure chat is always delivered and marked.
        if (_standing.ShouldHide(message))
            return;

        Print(message.Message, _standing.AnsiPrefix(message));
    }

    /// <summary>Renders, filters, prints, and (optionally) logs one component with an already-built prefix.</summary>
    public void Print(Component message, string prefix = "")
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(prefix);
        string rendered = _renderer.Render(message);
        if (rendered.Length == 0)
            return;

        // Filter on the message text only: the standing marker is presentation and must not make a user's chat regex match or miss.
        string plain = _renderer.ColorEnabled ? Ansi.Strip(rendered) : rendered;
        if (plain.Length == 0 || !_chatFilter.ShouldShow(plain))
            return;

        string marked = prefix + rendered;
        string consoleLine = _timestamps
            ? $"[{DateTime.Now:HH:mm:ss}] {marked}"
            : marked;
        _write(consoleLine);

        if (_logChat)
            _fileSink?.Write(marked);
    }
}
