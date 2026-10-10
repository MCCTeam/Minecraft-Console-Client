using Mcc.Cli.Localization;
using DMCBK.Core.Configuration;
using Umpk.Client.Chat;
using Umpk.Client.Events;

namespace Mcc.Cli.Presentation;

/// <summary>One standing marker to draw: the glyph or word, and the family that colours it.</summary>
/// <param name="Text">The marker text. A single bar when colour distinguishes the kinds, else a word tag.</param>
/// <param name="Kind">The standing family, for the host's colour lookup.</param>
internal readonly record struct ChatStandingMark(string Text, ChatStanding Kind);

/// <summary>
/// Turns the signature standing UMPK reports on an inbound chat message (<see cref="Umpk.Client.Chat.ChatStandingRule"/>) into the marker a host shows, under the <c>[Chat.Signature]</c> toggles.
/// <para>
/// A message is NEVER hidden because of its standing, with the single exception the legacy client already had: <c>ShowIllegalSignedChat = false</c> drops messages whose signature was checked and REJECTED.
/// Everything else is delivered and marked, because a consumer that wants to treat unsigned or uncheckable chat differently has to see it first.
/// </para>
/// <para>
/// When the host can colour, the marker is a single bar, the ASCII analogue of the legacy coloured block.
/// When it cannot, only the ABNORMAL standings get a short word tag: an uncoloured bar carries no information, and tagging the normal case in text on every line is the spam this deliberately avoids.
/// </para>
/// </summary>
internal sealed class ChatStandingMarker
{
    private const string Reset = "[0m";

    private readonly SignatureConfig _config;
    private readonly bool _color;

    /// <summary>Builds a marker over the signature toggles. <paramref name="color"/> is the host's colour capability.</summary>
    public ChatStandingMarker(SignatureConfig config, bool color)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        _color = color;
    }

    /// <summary>
    /// Whether this message must not be shown at all.
    /// Only a REJECTED signature qualifies, and only when the operator turned <c>ShowIllegalSignedChat</c> off; this is the one legacy suppression rule.
    /// </summary>
    public bool ShouldHide(ChatMessageReceived message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return ChatStandingRule.IsHidable(ChatStandingRule.Classify(message)) && !_config.ShowIllegalSignedChat;
    }

    /// <summary>The marker for this message, or null when this standing is not marked.</summary>
    public ChatStandingMark? Mark(ChatMessageReceived message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ChatStanding standing = ChatStandingRule.Classify(message);
        if (standing == ChatStanding.None || !IsEnabled(standing))
            return null;

        if (_color)
            return new ChatStandingMark(Strings.ChatStandingBar, standing);

        // Uncoloured: mark only what a reader needs to act on.
        // A verified message on an online-mode server is the expected case, and a bare bar without colour says nothing about which case it is.
        string? tag = PlainTagOf(standing);
        return tag is null ? null : new ChatStandingMark(tag, standing);
    }

    /// <summary>The ANSI-coloured prefix for the classic console host, with its trailing space, or empty.</summary>
    public string AnsiPrefix(ChatMessageReceived message)
    {
        if (Mark(message) is not { } mark)
            return string.Empty;

        return _color ? $"[{AnsiCodeOf(mark.Kind)}m{mark.Text}{Reset} " : mark.Text + " ";
    }

    /// <summary>The stable, colour-free name of a standing, for logs and diagnostics.</summary>
    public static string LabelOf(ChatVerification verification) => verification switch
    {
        ChatVerification.Verified => Strings.ChatStandingLabelVerified,
        ChatVerification.Unverified => Strings.ChatStandingLabelUnverified,
        ChatVerification.Failed => Strings.ChatStandingLabelRejected,
        ChatVerification.Insecure => Strings.ChatStandingLabelInsecure,
        _ => Strings.ChatStandingLabelNotApplicable,
    };

    /// <summary>The SGR colour code for a standing family.</summary>
    public static int AnsiCodeOf(ChatStanding kind) => kind switch
    {
        ChatStanding.Verified => 32,      // green
        ChatStanding.Rejected => 31,      // red
        ChatStanding.Unverified => 33,    // yellow
        ChatStanding.Insecure => 34,      // blue
        _ => 90,                          // bright black (gray)
    };

    private bool IsEnabled(ChatStanding kind) => kind switch
    {
        ChatStanding.Verified => _config.MarkLegallySignedMsg,
        ChatStanding.Rejected => _config.MarkIllegallySignedMsg,
        ChatStanding.Unverified => _config.MarkUnverifiedMsg,
        ChatStanding.Insecure => _config.MarkInsecureMsg,
        _ => _config.MarkSystemMessage,
    };

    private static string? PlainTagOf(ChatStanding kind) => kind switch
    {
        ChatStanding.Rejected => Strings.ChatStandingRejected,
        ChatStanding.Unverified => Strings.ChatStandingUnverified,
        ChatStanding.Insecure => Strings.ChatStandingInsecure,
        _ => null,
    };
}
