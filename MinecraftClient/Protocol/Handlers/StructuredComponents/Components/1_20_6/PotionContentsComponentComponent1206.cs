using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class PotionContentsComponentComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int PotiononId { get; set; }
    public bool HasCustomColor { get; set; }
    public int CustomColor { get; set; }
    public int NumberOfCustomEffects { get; set; }
    public List<PotionEffectSubComponent1206> Effects { get; set; } = new();
    
    public override void Parse(Queue<byte> data)
    {
        PotiononId = dataTypes.ReadNextVarInt(data);
        HasCustomColor = dataTypes.ReadNextBool(data);
        CustomColor = dataTypes.ReadNextInt(data);
        NumberOfCustomEffects = dataTypes.ReadNextVarInt(data);
        
        for(var i = 0; i < NumberOfCustomEffects; i++)
            Effects.Add((PotionEffectSubComponent1206)subComponentRegistry.ParseSubComponent(SubComponents.PotionEffect, data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(PotiononId));
        data.AddRange(DataTypes.GetBool(HasCustomColor));
        data.AddRange(DataTypes.GetInt(CustomColor));

        if (NumberOfCustomEffects > 0)
        {
            if(Effects.Count != NumberOfCustomEffects)
                throw new ArgumentNullException($"Can not serialize PotionContentsComponentComponent1206 due to NumberOfCustomEffects being different from the count of elements in the Effects list!");
            
            foreach(var effect in Effects)
                data.AddRange(effect.Serialize());
        }
        
        return new Queue<byte>(data);
    }
}