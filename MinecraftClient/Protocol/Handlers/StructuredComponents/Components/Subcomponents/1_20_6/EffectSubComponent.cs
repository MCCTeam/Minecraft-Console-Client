using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class EffectSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public PotionEffectSubComponent TypeId { get; set; }
    public float Probability { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        TypeId = (PotionEffectSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.PotionEffect, data);
        Probability = dataTypes.ReadNextFloat(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(TypeId.Serialize());
        data.AddRange(DataTypes.GetFloat(Probability));
        return new Queue<byte>(data);
    }
}