using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class LoreNameComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfLines { get; set; }
    public List<string> Lines { get; set; } = [];
    public List<Dictionary<string, object>> LinesNbt { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfLines = dataTypes.ReadNextVarInt(data);
        
        if (NumberOfLines <= 0) return;
        
        for (var i = 0; i < NumberOfLines; i++)
        {
            var lineNbt = dataTypes.ReadNextNbt(data);
            LinesNbt.Add(lineNbt);
            Lines.Add(ChatParser.ParseText(lineNbt));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(LinesNbt.Count));

        foreach (var lineNbt in LinesNbt)
            data.AddRange(DataTypes.GetNbt(lineNbt));

        return new Queue<byte>(data);
    }
}