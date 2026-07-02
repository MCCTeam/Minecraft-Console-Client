using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_2;

public class SulfurCubeContentComponent262(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public Item? AbsorbedBlockItemStack { get; set; }

    public override void Parse(Queue<byte> data)
    {
        AbsorbedBlockItemStack = DataTypes.ReadNextItemStackTemplate(data, ItemPalette);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        if (AbsorbedBlockItemStack is not null && !AbsorbedBlockItemStack.IsEmpty)
            data.AddRange(DataTypes.GetItemStackTemplate(AbsorbedBlockItemStack, ItemPalette));
        return new Queue<byte>(data);
    }
}
