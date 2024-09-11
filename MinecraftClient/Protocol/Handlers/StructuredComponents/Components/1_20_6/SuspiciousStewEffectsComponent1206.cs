using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class SuspiciousStewEffectsComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfEffects { get; set; }
    public List<SuspiciousStewEffect> Effects { get; set; } = new();

    public override void Parse(Queue<byte> data)
    {
        NumberOfEffects = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfEffects; i++)
            Effects.Add(new SuspiciousStewEffect(dataTypes.ReadNextVarInt(data), dataTypes.ReadNextVarInt(data)));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfEffects));

        if (NumberOfEffects != Effects.Count)
            throw new InvalidOperationException("Can not serialize SuspiciousStewEffectsComponent1206 because umberOfEffects != Effects.Count!");
        
        foreach (var effect in Effects)
        {
            data.AddRange(DataTypes.GetVarInt(effect.TypeId));
            data.AddRange(DataTypes.GetVarInt(effect.Duration));
        }
        return new Queue<byte>(data);
    }
}