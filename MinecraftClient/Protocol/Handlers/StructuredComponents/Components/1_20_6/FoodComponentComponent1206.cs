using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class FoodComponentComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Nutrition { get; set; }
    public bool Saturation { get; set; }
    public bool CanAlwaysEat { get; set; }
    public float SecondsToEat { get; set; }
    public int NumberOfEffects { get; set; }
    public List<EffectSubComponent1206> Effects { get; set; } = new();
    
    public override void Parse(Queue<byte> data)
    {
        Nutrition = dataTypes.ReadNextVarInt(data);
        Saturation = dataTypes.ReadNextBool(data);
        CanAlwaysEat = dataTypes.ReadNextBool(data);
        SecondsToEat = dataTypes.ReadNextFloat(data);
        NumberOfEffects = dataTypes.ReadNextVarInt(data);
        
        for(var i = 0; i < NumberOfEffects; i++)
            Effects.Add((EffectSubComponent1206)subComponentRegistry.ParseSubComponent(SubComponents.Effect, data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Nutrition));
        data.AddRange(DataTypes.GetBool(Saturation));
        data.AddRange(DataTypes.GetBool(CanAlwaysEat));
        data.AddRange(DataTypes.GetFloat(SecondsToEat));
        data.AddRange(DataTypes.GetFloat(NumberOfEffects));

        if (NumberOfEffects > 0)
        {
            if(Effects.Count != NumberOfEffects)
                throw new ArgumentNullException($"Can not serialize FoodComponent1206 due to NumberOfEffcets being different from the count of elements in the Effects list!");
            
            foreach(var effect in Effects)
                data.AddRange(effect.Serialize());
        }
        
        return new Queue<byte>(data);
    }
}