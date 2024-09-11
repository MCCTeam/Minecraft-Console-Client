using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class MapPostProcessingComponent1206(DataTypes dataTypes, ItemPalette itemPalette,  SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Type { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        Type = dataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Type));
        return new Queue<byte>(data);
    }
}