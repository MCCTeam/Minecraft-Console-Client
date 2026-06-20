using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class KineticWeaponComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int ContactCooldownTicks { get; set; }
    public int DelayTicks { get; set; }
    public KineticWeaponConditionData? DismountConditions { get; set; }
    public KineticWeaponConditionData? KnockbackConditions { get; set; }
    public KineticWeaponConditionData? DamageConditions { get; set; }
    public float ForwardMovement { get; set; }
    public float DamageMultiplier { get; set; }
    public SoundEventHolderData? Sound { get; set; }
    public SoundEventHolderData? HitSound { get; set; }

    public override void Parse(Queue<byte> data)
    {
        ContactCooldownTicks = DataTypes.ReadNextVarInt(data);
        DelayTicks = DataTypes.ReadNextVarInt(data);
        DismountConditions = ReadOptionalCondition(data);
        KnockbackConditions = ReadOptionalCondition(data);
        DamageConditions = ReadOptionalCondition(data);
        ForwardMovement = DataTypes.ReadNextFloat(data);
        DamageMultiplier = DataTypes.ReadNextFloat(data);
        Sound = StructuredComponentCodecHelpers.ReadOptionalSoundEventHolder(DataTypes, data);
        HitSound = StructuredComponentCodecHelpers.ReadOptionalSoundEventHolder(DataTypes, data);
    }

    private KineticWeaponConditionData? ReadOptionalCondition(Queue<byte> data)
    {
        if (!DataTypes.ReadNextBool(data))
            return null;

        return new KineticWeaponConditionData(
            DataTypes.ReadNextVarInt(data),
            DataTypes.ReadNextFloat(data),
            DataTypes.ReadNextFloat(data));
    }

    private void WriteOptionalCondition(List<byte> data, KineticWeaponConditionData? condition)
    {
        data.AddRange(DataTypes.GetBool(condition is not null));
        if (condition is null)
            return;

        data.AddRange(DataTypes.GetVarInt(condition.MaxDurationTicks));
        data.AddRange(DataTypes.GetFloat(condition.MinSpeed));
        data.AddRange(DataTypes.GetFloat(condition.MinRelativeSpeed));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(ContactCooldownTicks));
        data.AddRange(DataTypes.GetVarInt(DelayTicks));
        WriteOptionalCondition(data, DismountConditions);
        WriteOptionalCondition(data, KnockbackConditions);
        WriteOptionalCondition(data, DamageConditions);
        data.AddRange(DataTypes.GetFloat(ForwardMovement));
        data.AddRange(DataTypes.GetFloat(DamageMultiplier));
        StructuredComponentCodecHelpers.WriteOptionalSoundEventHolder(DataTypes, data, Sound);
        StructuredComponentCodecHelpers.WriteOptionalSoundEventHolder(DataTypes, data, HitSound);
        return new Queue<byte>(data);
    }
}

public sealed record KineticWeaponConditionData(int MaxDurationTicks, float MinSpeed, float MinRelativeSpeed);
