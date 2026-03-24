using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class KineticWeaponComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
        DataTypes.ReadNextVarInt(data); // contactCooldownTicks
        DataTypes.ReadNextVarInt(data); // delayTicks
        ReadOptionalCondition(data);    // dismountConditions
        ReadOptionalCondition(data);    // knockbackConditions
        ReadOptionalCondition(data);    // damageConditions
        DataTypes.ReadNextFloat(data);  // forwardMovement
        DataTypes.ReadNextFloat(data);  // damageMultiplier
        ReadOptionalSoundEventHolder(data); // sound
        ReadOptionalSoundEventHolder(data); // hitSound
    }

    private void ReadOptionalCondition(Queue<byte> data)
    {
        if (!DataTypes.ReadNextBool(data)) return;
        DataTypes.ReadNextVarInt(data); // maxDurationTicks
        DataTypes.ReadNextFloat(data);  // minSpeed
        DataTypes.ReadNextFloat(data);  // minRelativeSpeed
    }

    private void ReadOptionalSoundEventHolder(Queue<byte> data)
    {
        if (!DataTypes.ReadNextBool(data)) return;
        var holderId = DataTypes.ReadNextVarInt(data);
        if (holderId == 0)
        {
            DataTypes.ReadNextString(data);
            if (DataTypes.ReadNextBool(data))
                DataTypes.ReadNextFloat(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
