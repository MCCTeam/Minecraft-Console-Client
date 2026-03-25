using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class PotionContentsComponent1212(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool HasPotionId { get; set; }
    public int PotionId { get; set; }
    public bool HasCustomColor { get; set; }
    public int CustomColor { get; set; }
    public List<PotionEffectSubComponent> Effects { get; set; } = [];
    public bool HasCustomName { get; set; }
    public string? CustomName { get; set; }

    public override void Parse(Queue<byte> data)
    {
        HasPotionId = DataTypes.ReadNextBool(data);
        if (HasPotionId)
            PotionId = DataTypes.ReadNextVarInt(data);

        HasCustomColor = DataTypes.ReadNextBool(data);
        if (HasCustomColor)
            CustomColor = DataTypes.ReadNextInt(data);

        var numberOfEffects = DataTypes.ReadNextVarInt(data);
        Effects = new List<PotionEffectSubComponent>(numberOfEffects);
        for (var i = 0; i < numberOfEffects; i++)
            Effects.Add((PotionEffectSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.PotionEffect, data));

        HasCustomName = DataTypes.ReadNextBool(data);
        if (HasCustomName)
            CustomName = DataTypes.ReadNextString(data);
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

        data.AddRange(DataTypes.GetBool(HasCustomName));
        if (HasCustomName && CustomName is not null)
            data.AddRange(DataTypes.GetString(CustomName));

        return new Queue<byte>(data);
    }
}
