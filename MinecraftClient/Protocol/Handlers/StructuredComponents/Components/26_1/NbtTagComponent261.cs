using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_1;

public class NbtTagComponent261(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public object? Tag { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Tag = DataTypes.ReadNextNbtTag(data);
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>(DataTypes.GetNbtTag(Tag));
    }
}
