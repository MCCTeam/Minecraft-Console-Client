using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class AttackRangeComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
    public float MinCreativeRange { get; set; }
    public float MaxCreativeRange { get; set; }
    public float HitboxMargin { get; set; }
    public float MobFactor { get; set; }

    public override void Parse(Queue<byte> data)
    {
        MinRange = DataTypes.ReadNextFloat(data);
        MaxRange = DataTypes.ReadNextFloat(data);
        MinCreativeRange = DataTypes.ReadNextFloat(data);
        MaxCreativeRange = DataTypes.ReadNextFloat(data);
        HitboxMargin = DataTypes.ReadNextFloat(data);
        MobFactor = DataTypes.ReadNextFloat(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetFloat(MinRange));
        bytes.AddRange(DataTypes.GetFloat(MaxRange));
        bytes.AddRange(DataTypes.GetFloat(MinCreativeRange));
        bytes.AddRange(DataTypes.GetFloat(MaxCreativeRange));
        bytes.AddRange(DataTypes.GetFloat(HitboxMargin));
        bytes.AddRange(DataTypes.GetFloat(MobFactor));
        return new Queue<byte>(bytes);
    }
}
