using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class CustomModelDataComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Value { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Value = DataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>(DataTypes.GetVarInt(Value));
    }
}
