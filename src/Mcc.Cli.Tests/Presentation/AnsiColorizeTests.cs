using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// The SGR wrapper the classic host paints highlights with, including the Microsoft device code.
/// The expectations are literal escape sequences rather than anything derived from <see cref="AnsiColorMapping"/>, so a mapping change has to be looked at rather than absorbed.
/// </summary>
public sealed class AnsiColorizeTests
{
    [Fact]
    public void Colorize_AtTrueColor_WrapsInAYellowSgrRunAndResets()
    {
        // Vanilla yellow is #FFFF55.
        Assert.Equal(
            "[38;2;255;255;85mQ6PCGWQD[0m",
            Ansi.Colorize("Q6PCGWQD", TextColor.Yellow, ConsoleColorDepth.Vt10024Bit));
    }

    [Fact]
    public void Colorize_AtFourBit_FallsBackToTheNearestBasicColor()
    {
        // Bright yellow, SGR 93.
        Assert.Equal(
            "[93mQ6PCGWQD[0m",
            Ansi.Colorize("Q6PCGWQD", TextColor.Yellow, ConsoleColorDepth.Vt1004Bit));
    }

    [Fact]
    public void Colorize_WithColorOff_ReturnsThePlainText()
        => Assert.Equal("Q6PCGWQD", Ansi.Colorize("Q6PCGWQD", TextColor.Yellow, ConsoleColorDepth.Disable));

    [Fact]
    public void Colorize_RoundTripsThroughStrip()
    {
        string painted = Ansi.Colorize("Q6PCGWQD", TextColor.Yellow, ConsoleColorDepth.Vt10024Bit);
        Assert.Equal("Q6PCGWQD", Ansi.Strip(painted));
    }

    [Fact]
    public void DeviceCode_CarriesTheHighlightedCode()
    {
        string painted = Ansi.Colorize("Q6PCGWQD", TextColor.Yellow, ConsoleColorDepth.Vt10024Bit);
        string line = Strings.DeviceCode(painted, "https://www.microsoft.com/link");

        Assert.Contains(painted, line, StringComparison.Ordinal);

        // The URL stays unpainted: only the code is the thing to copy.
        Assert.Equal(
            "To sign in, open https://www.microsoft.com/link in a browser and enter the code: Q6PCGWQD",
            Ansi.Strip(line));
    }
}
