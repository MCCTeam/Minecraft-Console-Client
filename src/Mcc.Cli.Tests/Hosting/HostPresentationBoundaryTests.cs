using Mcc.Cli.Hosting.Classic;
using DMCBK.Core.Commands;
using Mcc.Cli;
using Mcc.Cli.Configuration;
using Mcc.Cli.Presentation;
using Umpk.Client.Snapshots;
using Umpk.Geometry;
using Xunit;

namespace Mcc.Cli.Tests.Hosting;

public sealed class HostPresentationBoundaryTests
{
    [Fact]
    public void ChunkMapRetainsPlayerMarkerAndDebugLoadingState()
    {
        var grid = new ChunkStatusGrid(10, 20, 3, 3, Enumerable.Repeat(true, 9).ToArray());
        var request = new ChunkMapPresentation(grid, new HashSet<ChunkPos> { new(11, 20) },
            "X:160 Y:71 Z:320", null, new ChunkPos(11, 20), 25, 17);
        string text = string.Join('\n', ChunkMapRenderer.Render(request, DMCBK.Core.Presentation.GlyphSet.Ascii));
        Assert.Contains("§§7■§§r", text);
        Assert.Contains("§§4▣§§r", text);
        Assert.Contains("X:160 Y:71 Z:320", text);
        Assert.Contains("8", text);
    }

    [Fact]
    public void ChunkMapReportsMarkerOutsideViewport()
    {
        var grid = new ChunkStatusGrid(0, 0, 3, 3, Enumerable.Repeat(true, 9).ToArray());
        var request = new ChunkMapPresentation(grid, new HashSet<ChunkPos>(), "0, 71, 0",
            null, new ChunkPos(1000, 1000), 25, 17);
        Assert.Contains(DMCBK.Core.Localization.McStrings.Get("cmd.chunk.outside"),
            ChunkMapRenderer.Render(request, MccGlyphs.Emoji));
    }

    [Fact]
    public void InventoryLayoutCanBeDisabledByTheHost()
    {
        var host = new ConsoleHostUi { ShowInventoryLayout = false };
        Assert.Null(host.GetInventoryLayout(null, playerInventory: true));
        host.ShowInventoryLayout = true;
        Assert.NotNull(host.GetInventoryLayout(null, playerInventory: true));
    }

    [Fact]
    public void PresentationPreferencesLoadFromConsoleConfiguration()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-presentation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "console.toml"), """
                [General]
                ShowInventoryLayout = false
                ShowEffectNamesInTui = true
                """);
            ConsoleHostConfig config = ConsoleHostConfig.Load(root);
            Assert.False(config.ShowInventoryLayout);
            Assert.True(config.ShowEffectNamesInTui);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
