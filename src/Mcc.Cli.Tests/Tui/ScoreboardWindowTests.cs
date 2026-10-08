using Avalonia;
using Mcc.Cli.Tui.Scoreboard;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

public sealed class ScoreboardWindowTests
{
    [Theory]
    [InlineData(100, 50, 42, 24, 57, 13)]
    [InlineData(80, 30, 30, 10, 49, 10)]
    public void DefaultAnchor_IsRightAlignedAndVerticallyCentered(
        double surfaceWidth,
        double surfaceHeight,
        double windowWidth,
        double windowHeight,
        int expectedX,
        int expectedY)
        => Assert.Equal(
            new PixelPoint(expectedX, expectedY),
            ScoreboardWindow.ResolveRightCenter(
                new Size(surfaceWidth, surfaceHeight),
                new Size(windowWidth, windowHeight)));

    [Fact]
    public void DefaultAnchor_ClampsWhenWindowIsLargerThanSurface()
        => Assert.Equal(
            new PixelPoint(0, 0),
            ScoreboardWindow.ResolveRightCenter(new Size(20, 8), new Size(42, 24)));
}
