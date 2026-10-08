using Mcc.Cli.Localization;
using Mcc.Cli;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// The two chat notices that carry NO message text, and must not start carrying any.
/// A fully filtered message is one the server delivered and told the client to display none of; a stream gap is a message the server never delivered at all.
/// Vanilla renders nothing for the first (<c>ChatListener.showMessageToPlayer</c> is gated on <c>!isFullyFiltered()</c>) and disconnects on the second (<c>ClientPacketListener.handlePlayerChat</c> -> <c>BAD_CHAT_INDEX</c>), so neither has a rendering to copy.
/// For a headless client the honest rendering is to name what happened.
/// </summary>
public sealed class ChatSurfaceNoticeTests
{
    private static readonly Guid Sender = Guid.Parse("bd90c77b-03cb-394f-bdc0-e4ff70a95c6a");

    /// <summary>
    /// The withheld notice names the sender and nothing else.
    /// The frame still carries the whole signed body, so any content here would hand the user exactly what the server said to hold back.
    /// </summary>
    [Fact]
    public void WithheldNotice_NamesTheSender_AndCarriesNoContent()
    {
        string line = Strings.ChatMessageWithheld(Sender);

        Assert.Equal($"[chat] The server withheld a message from {Sender}.", line);
        Assert.Contains(Sender.ToString(), line, StringComparison.Ordinal);
    }

    /// <summary>
    /// The gap notice reports both indices and the running total, so a user can tell a single dropped frame from a stream that is losing messages continuously.
    /// The running total is the only production reader of <c>ChatState.ObservedGaps</c>; a counter nothing reads is the defect family this work closes.
    /// </summary>
    [Fact]
    public void GapNotice_ReportsBothIndicesAndTheRunningTotal()
    {
        string line = Strings.ChatStreamGapDetected(expected: 7, actual: 9, total: 3);

        Assert.Contains("expected index 7", line, StringComparison.Ordinal);
        Assert.Contains("got 9", line, StringComparison.Ordinal);
        Assert.Contains("3 gap(s) this session", line, StringComparison.Ordinal);
    }

    /// <summary>Neither notice may use an em dash (repository rule), and both are plain ASCII.</summary>
    [Fact]
    public void Notices_AreAsciiAndFreeOfEmDashes()
    {
        foreach (string line in new[]
                 {
                     Strings.ChatMessageWithheld(Sender),
                     Strings.ChatStreamGapDetected(1, 2, 1),
                 })
        {
            Assert.DoesNotContain('—', line);
            Assert.All(line, c => Assert.True(c < 128, $"non-ASCII character '{c}' in \"{line}\""));
        }
    }
}
