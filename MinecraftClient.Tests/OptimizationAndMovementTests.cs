using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.PacketPalettes;
using Xunit;

namespace MinecraftClient.Tests;

public class OptimizationAndMovementTests
{
    [Fact]
    public void TestItemTypeExtensions_FoodAndStackCount()
    {
        Assert.True(ItemType.Apple.IsFood());
        Assert.True(ItemType.CookedBeef.IsFood());
        Assert.True(ItemType.Bread.IsFood());
        Assert.False(ItemType.DiamondSword.IsFood());
        Assert.False(ItemType.Stone.IsFood());

        Assert.True(ItemType.Stone.IsStackable());
        Assert.Equal(64, ItemType.Stone.StackCount());
        Assert.Equal(64, ItemType.Egg.StackCount());
        Assert.Equal(1, ItemType.DiamondSword.StackCount());
        Assert.False(ItemType.DiamondSword.IsStackable());
    }

    [Fact]
    public void TestChunk_FillWithBlock()
    {
        var chunk = new Chunk();
        var stone = new Block(1, 0);
        chunk.FillWithBlock(stone);

        for (int y = 0; y < 16; y += 7)
        {
            for (int z = 0; z < 16; z += 5)
            {
                for (int x = 0; x < 16; x += 3)
                {
                    var block = chunk.GetBlock(new Location(x, y, z));
                    Assert.Equal(stone.BlockId, block.BlockId);
                }
            }
        }
    }

    [Fact]
    public void TestMaterial2Tool_Sets()
    {
        var stoneTools = Material2Tool.GetCorrectToolForBlock(Material.Stone);
        Assert.NotEmpty(stoneTools);
        Assert.Contains(ItemType.DiamondPickaxe, stoneTools);

        var oakLogTools = Material2Tool.GetCorrectToolForBlock(Material.OakLog);
        Assert.NotEmpty(oakLogTools);
        Assert.Contains(ItemType.DiamondAxe, oakLogTools);

        Assert.True(Material2Tool.IsUnbreakable(Material.Bedrock));
        Assert.True(Material2Tool.IsUnbreakable(Material.Barrier));
        Assert.False(Material2Tool.IsUnbreakable(Material.Stone));
    }

    [Fact]
    public void TestPalette_StaticCachesWork()
    {
        var palette1 = new ItemPalette120();
        var palette2 = new ItemPalette120();

        int id1 = palette1.ToId(ItemType.Stone);
        int id2 = palette2.ToId(ItemType.Stone);
        Assert.Equal(id1, id2);
        Assert.Equal(ItemType.Stone, palette1.FromId(id1));

        var packetPal1 = new PacketPalette1202();
        var packetPal2 = new PacketPalette1202();
        Assert.NotNull(packetPal1);
        Assert.NotNull(packetPal2);
    }
}
