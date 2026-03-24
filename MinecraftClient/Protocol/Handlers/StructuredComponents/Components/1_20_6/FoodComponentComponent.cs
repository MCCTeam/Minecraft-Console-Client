using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class FoodComponentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Nutrition { get; set; }
    public float Saturation { get; set; }
    public bool CanAlwaysEat { get; set; }
    public float SecondsToEat { get; set; }
    public List<EffectSubComponent> Effects { get; set; } = new();
    
    public override void Parse(Queue<byte> data)
    {
        Nutrition = DataTypes.ReadNextVarInt(data);
        Saturation = DataTypes.ReadNextFloat(data);
        CanAlwaysEat = DataTypes.ReadNextBool(data);
        SecondsToEat = DataTypes.ReadNextFloat(data);
        var numberOfEffects = DataTypes.ReadNextVarInt(data);
        
        for(var i = 0; i < numberOfEffects; i++)
            Effects.Add((EffectSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.Effect, data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Nutrition));
        data.AddRange(DataTypes.GetFloat(Saturation));
        data.AddRange(DataTypes.GetBool(CanAlwaysEat));
        data.AddRange(DataTypes.GetFloat(SecondsToEat));
        data.AddRange(DataTypes.GetVarInt(Effects.Count));

        foreach(var effect in Effects)
            data.AddRange(effect.Serialize());
        
        return new Queue<byte>(data);
    }
}