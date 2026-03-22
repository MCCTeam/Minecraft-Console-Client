using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class KineticWeaponComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
        dataTypes.ReadNextVarInt(data); // contactCooldownTicks
        dataTypes.ReadNextVarInt(data); // delayTicks
        ReadOptionalCondition(data);    // dismountConditions
        ReadOptionalCondition(data);    // knockbackConditions
        ReadOptionalCondition(data);    // damageConditions
        dataTypes.ReadNextFloat(data);  // forwardMovement
        dataTypes.ReadNextFloat(data);  // damageMultiplier
        ReadOptionalSoundEventHolder(data); // sound
        ReadOptionalSoundEventHolder(data); // hitSound
    }

    private void ReadOptionalCondition(Queue<byte> data)
    {
        if (!dataTypes.ReadNextBool(data)) return;
        dataTypes.ReadNextVarInt(data); // maxDurationTicks
        dataTypes.ReadNextFloat(data);  // minSpeed
        dataTypes.ReadNextFloat(data);  // minRelativeSpeed
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
