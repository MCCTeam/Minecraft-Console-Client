using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class MaxDamageComponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, subComponentRegistry)
{
    public int MaxDamage { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        MaxDamage = dataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(MaxDamage));
        return new Queue<byte>(data);
    }
}