using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class VarIntComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Value { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Value = dataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetVarInt(Value));
        return new Queue<byte>(bytes);
    }
}
