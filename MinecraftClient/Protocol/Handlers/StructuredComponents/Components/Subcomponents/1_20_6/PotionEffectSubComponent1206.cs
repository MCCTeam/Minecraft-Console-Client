using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class PotionEffectSubComponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int TypeId { get; set; }
    public DetailsSubComponent1206 Details { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        TypeId = dataTypes.ReadNextVarInt(data);
        Details = (DetailsSubComponent1206)subComponentRegistry.ParseSubComponent(SubComponents.Details, data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(TypeId));
        data.AddRange(Details.Serialize());
        return new Queue<byte>(data);
    }
}