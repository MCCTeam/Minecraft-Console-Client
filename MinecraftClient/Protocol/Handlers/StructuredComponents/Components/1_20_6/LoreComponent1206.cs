using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class LoreNameComponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, subComponentRegistry)
{
    public int NumberOfLines { get; set; }
    public List<string> Lines { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfLines = dataTypes.ReadNextVarInt(data);
        
        if (NumberOfLines <= 0) return;
        
        for (var i = 0; i < NumberOfLines; i++)
            Lines.Add(ChatParser.ParseText(dataTypes.ReadNextString(data)));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Lines.Count));

        if (Lines.Count <= 0) return new Queue<byte>(data);
        
        foreach (var line in Lines)
            data.AddRange(DataTypes.GetString(line));

        return new Queue<byte>(data);
    }
}