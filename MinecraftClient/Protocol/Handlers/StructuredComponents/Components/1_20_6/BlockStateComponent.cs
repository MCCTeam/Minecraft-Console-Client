using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class BlockStateComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfProperties { get; set; }
    public List<(string, string)> Properties { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfProperties = dataTypes.ReadNextVarInt(data);
        for(var i = 0; i < NumberOfProperties; i++)
            Properties.Add((dataTypes.ReadNextString(data), dataTypes.ReadNextString(data)));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfProperties));
        for (var i = 0; i < NumberOfProperties; i++)
        {
            data.AddRange(DataTypes.GetString(Properties[i].Item1));
            data.AddRange(DataTypes.GetString(Properties[i].Item2));
        }
            
        return new Queue<byte>(data);
    }
}