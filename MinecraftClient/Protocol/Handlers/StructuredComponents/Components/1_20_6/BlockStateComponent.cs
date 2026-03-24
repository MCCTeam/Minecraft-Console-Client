using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class BlockStateComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public List<(string, string)> Properties { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        var count = DataTypes.ReadNextVarInt(data);
        for(var i = 0; i < count; i++)
            Properties.Add((DataTypes.ReadNextString(data), DataTypes.ReadNextString(data)));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Properties.Count));
        foreach (var (key, value) in Properties)
        {
            data.AddRange(DataTypes.GetString(key));
            data.AddRange(DataTypes.GetString(value));
        }
            
        return new Queue<byte>(data);
    }
}