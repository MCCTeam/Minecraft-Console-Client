using DMCBK.Core.Commands;
using Mcc.Cli.Presentation;
using Xunit;
namespace Mcc.Cli.Tests.Presentation;

public sealed class ConsoleMapRendererTests
{
    [Theory]
    // A 128 wide map in a 120 column terminal: 60 usable cells, so it downscales by 3.
    [InlineData(128, 128, 120, 50, 3)]
    // A terminal big enough shows it 1:1.
    [InlineData(128, 128, 400, 200, 1)]
    public void Map_ComputeScale_MatchesTheLegacyBoxFit(
        int width, int height, int consoleWidth, int consoleHeight, int expected)
        => Assert.Equal(expected, ConsoleMapRenderer.ComputeScale(width, height, consoleWidth, consoleHeight));

    [Fact]
    public void Map_ConsoleRenderer_EmitsOneLinePerTwoPixelRows()
    {
        var image = new RgbImageFrame(4, 4, new byte[48]);
        IReadOnlyList<string> lines = ConsoleMapRenderer.Render(image, scale: 2);

        // scale 2 collapses 4 rows into 2 lines.
        // Each baseX step emits TWO cells (the left and right half of the sampled block), so a 4-wide image at scale 2 is 2 steps = 4 glyphs per line.
        Assert.Equal(2, lines.Count);
        foreach (string line in lines)
        {
            Assert.Equal(4, line.Count(c => c == '\u2580'));
            Assert.EndsWith("\u001b[0m", line, StringComparison.Ordinal);
        }
    }

}
