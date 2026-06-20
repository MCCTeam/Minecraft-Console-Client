using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class BlocksAttacksComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public float BlockDelaySeconds { get; set; }
    public float DisableCooldownScale { get; set; }
    public List<DamageReductionData> DamageReductions { get; set; } = [];
    public float ItemDamageThreshold { get; set; }
    public float ItemDamageBase { get; set; }
    public float ItemDamageFactor { get; set; }
    public string? BypassedBy { get; set; }
    public SoundEventHolderData? BlockSound { get; set; }
    public SoundEventHolderData? DisableSound { get; set; }

    public override void Parse(Queue<byte> data)
    {
        BlockDelaySeconds = DataTypes.ReadNextFloat(data);
        DisableCooldownScale = DataTypes.ReadNextFloat(data);

        var reductionCount = DataTypes.ReadNextVarInt(data);
        for (var i = 0; i < reductionCount; i++)
        {
            var horizontalBlockingAngle = DataTypes.ReadNextFloat(data);

            var hasTypeFilter = DataTypes.ReadNextBool(data);
            var typeFilter = hasTypeFilter
                ? StructuredComponentCodecHelpers.ReadHolderSet(DataTypes, data)
                : null;

            var baseDmg = DataTypes.ReadNextFloat(data);
            var factor = DataTypes.ReadNextFloat(data);
            DamageReductions.Add(new DamageReductionData(horizontalBlockingAngle, typeFilter, baseDmg, factor));
        }

        ItemDamageThreshold = DataTypes.ReadNextFloat(data);
        ItemDamageBase = DataTypes.ReadNextFloat(data);
        ItemDamageFactor = DataTypes.ReadNextFloat(data);

        var hasBypassedBy = DataTypes.ReadNextBool(data);
        if (hasBypassedBy)
            BypassedBy = DataTypes.ReadNextString(data); // TagKey<DamageType> as ResourceLocation

        BlockSound = StructuredComponentCodecHelpers.ReadOptionalSoundEventHolder(DataTypes, data);
        DisableSound = StructuredComponentCodecHelpers.ReadOptionalSoundEventHolder(DataTypes, data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetFloat(BlockDelaySeconds));
        data.AddRange(DataTypes.GetFloat(DisableCooldownScale));
        data.AddRange(DataTypes.GetVarInt(DamageReductions.Count));
        foreach (var reduction in DamageReductions)
        {
            data.AddRange(DataTypes.GetFloat(reduction.HorizontalBlockingAngle));
            data.AddRange(DataTypes.GetBool(reduction.Type is not null));
            if (reduction.Type is not null)
                StructuredComponentCodecHelpers.WriteHolderSet(DataTypes, data, reduction.Type);
            data.AddRange(DataTypes.GetFloat(reduction.Base));
            data.AddRange(DataTypes.GetFloat(reduction.Factor));
        }

        data.AddRange(DataTypes.GetFloat(ItemDamageThreshold));
        data.AddRange(DataTypes.GetFloat(ItemDamageBase));
        data.AddRange(DataTypes.GetFloat(ItemDamageFactor));
        data.AddRange(DataTypes.GetBool(BypassedBy is not null));
        if (BypassedBy is not null)
            data.AddRange(DataTypes.GetString(BypassedBy));
        StructuredComponentCodecHelpers.WriteOptionalSoundEventHolder(DataTypes, data, BlockSound);
        StructuredComponentCodecHelpers.WriteOptionalSoundEventHolder(DataTypes, data, DisableSound);
        return new Queue<byte>(data);
    }
}

public sealed record DamageReductionData(float HorizontalBlockingAngle, HolderSetData? Type, float Base, float Factor);
