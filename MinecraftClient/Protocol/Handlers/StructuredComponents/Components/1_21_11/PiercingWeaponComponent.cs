using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class PiercingWeaponComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool DealsKnockback { get; set; }
    public bool Dismounts { get; set; }

    public override void Parse(Queue<byte> data)
    {
        DealsKnockback = dataTypes.ReadNextBool(data);
        Dismounts = dataTypes.ReadNextBool(data);
        ReadOptionalSoundEventHolder(data);
        ReadOptionalSoundEventHolder(data);
    }

    private void ReadOptionalSoundEventHolder(Queue<byte> data)
    {
        if (!dataTypes.ReadNextBool(data)) return;
        var holderId = dataTypes.ReadNextVarInt(data);
        if (holderId == 0)
        {
            dataTypes.ReadNextString(data);
            if (dataTypes.ReadNextBool(data))
                dataTypes.ReadNextFloat(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
