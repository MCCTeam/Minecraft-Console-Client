using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class PotionEffectSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int TypeId { get; set; }
    public DetailsSubComponent Details { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        TypeId = dataTypes.ReadNextVarInt(data);
        Details = (DetailsSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.Details, data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(TypeId));
        data.AddRange(Details.Serialize());
        return new Queue<byte>(data);
    }
}