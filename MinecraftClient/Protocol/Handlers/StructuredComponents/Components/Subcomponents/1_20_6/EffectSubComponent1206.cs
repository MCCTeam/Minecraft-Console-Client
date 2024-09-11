using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class EffectSubComponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public PotionEffectSubComponent1206 TypeId { get; set; }
    public float Probability { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        TypeId = (PotionEffectSubComponent1206)subComponentRegistry.ParseSubComponent(SubComponents.PotionEffect, data);
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