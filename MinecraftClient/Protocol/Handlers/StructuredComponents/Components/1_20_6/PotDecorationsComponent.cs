using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class PotDecorationsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfItems { get; set; }
    public List<int> Items { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfItems = dataTypes.ReadNextVarInt(data);
        for(var i = 0; i < NumberOfItems; i++)
            Items.Add(dataTypes.ReadNextVarInt(data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfItems));
        for(var i = 0; i < NumberOfItems; i++)
            data.AddRange(DataTypes.GetVarInt(Items[i]));
        return new Queue<byte>(data);
    }
}