using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class CustomNameComponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, subComponentRegistry)
{
    public string CustomName { get; set; } = string.Empty;
    
    public override void Parse(Queue<byte> data)
    {
        CustomName = ChatParser.ParseText(dataTypes.ReadNextString(data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetString(CustomName));
        return new Queue<byte>(data);
    }
}