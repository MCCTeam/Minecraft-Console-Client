using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class PiercingWeaponComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool DealsKnockback { get; set; }
    public bool Dismounts { get; set; }
    public SoundEventHolderData? Sound { get; set; }
    public SoundEventHolderData? HitSound { get; set; }

    public override void Parse(Queue<byte> data)
    {
        DealsKnockback = DataTypes.ReadNextBool(data);
        Dismounts = DataTypes.ReadNextBool(data);
        Sound = StructuredComponentCodecHelpers.ReadOptionalSoundEventHolder(DataTypes, data);
        HitSound = StructuredComponentCodecHelpers.ReadOptionalSoundEventHolder(DataTypes, data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetBool(DealsKnockback));
        data.AddRange(DataTypes.GetBool(Dismounts));
        StructuredComponentCodecHelpers.WriteOptionalSoundEventHolder(DataTypes, data, Sound);
        StructuredComponentCodecHelpers.WriteOptionalSoundEventHolder(DataTypes, data, HitSound);
        return new Queue<byte>(data);
    }
}
