using MinecraftClient.Mapping;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class FlatWorldTestBuilderTests
{
    [Fact]
    public void SetSolid_CreatesMissingChunkColumns()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 118, max: 126);

        FlatWorldTestBuilder.SetSolid(world, 123, 79, 110);

        Assert.Equal(Material.Stone, world.GetBlock(new Location(123, 79, 110)).Type);
    }
}
