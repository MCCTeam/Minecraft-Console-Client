using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core;
using DMCBK.Core.Commands;
using DMCBK.Core.Localization;
using Umpk.Client;
using Umpk.Protocol.Java;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// The kick reason keeps the server's formatting.
/// <para>
/// Reported live: a server that refuses a mis-cased nickname kicks with section-coded text, and the classic host printed <c>"Disconnected: kicked by the server: §cНеверный регистр ника!"</c> with the code left on screen as two literal characters.
/// <see cref="DisconnectDescription"/> flattened the message to plain text, which keeps neither a section code nor a component style, and the host then wrote that through the plain sink.
/// The describer takes the host's renderer now, so both forms of styling survive, and the classic host writes the line through the same formatted path its command output already used.
/// </para>
/// </summary>
public sealed class KickReasonFormattingTests
{
    private const string Esc = "\u001b";

    private static DisconnectInfo Kick(Component? message)
        => new() { Reason = CloseReason.DisconnectMessage, Message = message };

    private static AnsiComponentRenderer Renderer(ConsoleColorDepth depth = ConsoleColorDepth.Vt10024Bit)
        => new(new HostTranslations(), depth);

    [Fact]
    public void SectionCodedKickReason_RendersAsColour_ForTheClassicHost()
    {
        AnsiComponentRenderer renderer = Renderer();

        string described = Kick(Component.Text("§cНеверный регистр ника!")).Describe(renderMessage: renderer.Render);

        // red = 0xFF5555.
        // The code is gone from the text and has become an SGR run.
        Assert.Equal(
            CommandStrings.DisconnectKickedWith($"{Esc}[38;2;255;85;85mНеверный регистр ника!{Esc}[0m"),
            described);
        Assert.DoesNotContain("§", described, StringComparison.Ordinal);
    }

    [Fact]
    public void StyledKickReason_RendersAsColour_Too()
    {
        // The same defect wearing the other hat: a server that sends the reason as a styled component rather than as section codes was equally colourless, because plain flattening drops the style as well.
        AnsiComponentRenderer renderer = Renderer();
        var message = new Component(new TextContent("Banned"), new Style { Color = TextColor.Red });

        Assert.Equal(
            CommandStrings.DisconnectKickedWith($"{Esc}[38;2;255;85;85mBanned{Esc}[0m"),
            Kick(message).Describe(renderMessage: renderer.Render));
    }

    [Fact]
    public void ColourDisabled_StripsTheCodesInsteadOfPrintingThem()
    {
        // The renderer is also how the codes get REMOVED when colour is off: the pre-fix plain path left them in, so a no-colour terminal (or a redirected run) showed them as characters too.
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Disable);

        Assert.Equal(
            CommandStrings.DisconnectKickedWith("Неверный регистр ника!"),
            Kick(Component.Text("§cНеверный регистр ника!")).Describe(renderMessage: renderer.Render));
    }

    [Fact]
    public void MultiLineKickReason_KeepsItsLines()
    {
        // The second reported kick is two lines, each separately coded: the line break has to survive the describer, because the host splits on it when writing.
        AnsiComponentRenderer renderer = Renderer();
        var message = Component.Text("§cSuspicious activity\n§cBlocked for 5 minutes");

        string described = Kick(message).Describe(renderMessage: renderer.Render);

        Assert.Contains("\n", described, StringComparison.Ordinal);
        Assert.Contains($"{Esc}[38;2;255;85;85mSuspicious activity", described, StringComparison.Ordinal);
        Assert.Contains($"{Esc}[38;2;255;85;85mBlocked for 5 minutes", described, StringComparison.Ordinal);
        Assert.DoesNotContain("§", described, StringComparison.Ordinal);
    }

    [Fact]
    public void NoRenderer_StillFlattensToPlainText()
    {
        // The default is unchanged, so an embedding host that prints raw strings is not handed escape codes.
        Assert.Equal(
            CommandStrings.DisconnectKickedWith("§cNope"),
            Kick(Component.Text("§cNope")).Describe());
    }

    [Fact]
    public void ReasonOfNothingButCodes_FallsBackToThePlainText()
    {
        // A message that renders to nothing at all would otherwise turn "kicked, and here is why" into "kicked, and here is a blank", so the plain flattening stands in.
        AnsiComponentRenderer renderer = Renderer();

        Assert.Equal(
            CommandStrings.DisconnectKickedWith("§c"),
            Kick(Component.Text("§c")).Describe(renderMessage: renderer.Render));
    }

    [Fact]
    public void EmptinessIsStillDecidedOnThePlainText()
    {
        // A kick with no reason is a kick, not a kick with an empty colon.
        // The renderer must not change that decision by wrapping whitespace in escapes.
        AnsiComponentRenderer renderer = Renderer();

        Assert.Equal(CommandStrings.DisconnectKicked, Kick(null).Describe(renderMessage: renderer.Render));
        Assert.Equal(
            CommandStrings.DisconnectKicked, Kick(Component.Text("   ")).Describe(renderMessage: renderer.Render));
    }

    [Fact]
    public void NonKickCloseWithServerText_IsRenderedToo()
    {
        // A transfer, or any other close that carries server text, takes the same path.
        AnsiComponentRenderer renderer = Renderer();
        var info = new DisconnectInfo { Reason = CloseReason.SocketEof, Message = Component.Text("§eMoving you") };

        Assert.Equal($"{Esc}[38;2;255;255;85mMoving you{Esc}[0m", info.Describe(renderMessage: renderer.Render));
    }
}
