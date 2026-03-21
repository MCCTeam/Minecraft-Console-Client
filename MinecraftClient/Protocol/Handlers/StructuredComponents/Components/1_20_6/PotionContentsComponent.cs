using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class PotionContentsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool HasPotionId { get; set; }
    public int PotionId { get; set; }
    public bool HasCustomColor { get; set; }
    public int CustomColor { get; set; }
    public List<PotionEffectSubComponent> Effects { get; set; } = new();
    
    public override void Parse(Queue<byte> data)
    {
        HasPotionId = dataTypes.ReadNextBool(data);
        if (HasPotionId)
            PotionId = dataTypes.ReadNextVarInt(data);

        HasCustomColor = dataTypes.ReadNextBool(data);
        if (HasCustomColor)
            CustomColor = dataTypes.ReadNextInt(data);

        var numberOfEffects = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < numberOfEffects; i++)
            Effects.Add((PotionEffectSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.PotionEffect, data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetBool(HasPotionId));
        if (HasPotionId)
            data.AddRange(DataTypes.GetVarInt(PotionId));

        data.AddRange(DataTypes.GetBool(HasCustomColor));
        if (HasCustomColor)
            data.AddRange(DataTypes.GetInt(CustomColor));

        data.AddRange(DataTypes.GetVarInt(Effects.Count));
        foreach (var effect in Effects)
            data.AddRange(effect.Serialize());
        
        return new Queue<byte>(data);
    }
}
