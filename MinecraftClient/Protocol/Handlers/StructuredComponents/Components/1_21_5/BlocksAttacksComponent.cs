using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class BlocksAttacksComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public float BlockDelaySeconds { get; set; }
    public float DisableCooldownScale { get; set; }
    public List<byte[]> RawDamageReductions { get; set; } = [];
    public float ItemDamageThreshold { get; set; }
    public float ItemDamageBase { get; set; }
    public float ItemDamageFactor { get; set; }

    public override void Parse(Queue<byte> data)
    {
        BlockDelaySeconds = DataTypes.ReadNextFloat(data);
        DisableCooldownScale = DataTypes.ReadNextFloat(data);

        var reductionCount = DataTypes.ReadNextVarInt(data);
        for (var i = 0; i < reductionCount; i++)
        {
            var horizontalBlockingAngle = DataTypes.ReadNextFloat(data);

            var hasTypeFilter = DataTypes.ReadNextBool(data);
            if (hasTypeFilter)
                ReadHolderSet(data);

            var baseDmg = DataTypes.ReadNextFloat(data);
            var factor = DataTypes.ReadNextFloat(data);
        }

        ItemDamageThreshold = DataTypes.ReadNextFloat(data);
        ItemDamageBase = DataTypes.ReadNextFloat(data);
        ItemDamageFactor = DataTypes.ReadNextFloat(data);

        var hasBypassedBy = DataTypes.ReadNextBool(data);
        if (hasBypassedBy)
            DataTypes.ReadNextString(data); // TagKey<DamageType> as ResourceLocation

        var hasBlockSound = DataTypes.ReadNextBool(data);
        if (hasBlockSound)
            ReadSoundEventHolder(data);

        var hasDisableSound = DataTypes.ReadNextBool(data);
        if (hasDisableSound)
            ReadSoundEventHolder(data);
    }

    private void ReadHolderSet(Queue<byte> data)
    {
        var sizeOrTag = DataTypes.ReadNextVarInt(data);
        if (sizeOrTag == 0)
        {
            DataTypes.ReadNextString(data); // Tag ResourceLocation
        }
        else
        {
            var count = sizeOrTag - 1;
            for (var i = 0; i < count; i++)
                DataTypes.ReadNextVarInt(data); // Holder<DamageType> registry ids
        }
    }

    private void ReadSoundEventHolder(Queue<byte> data)
    {
        var holderId = DataTypes.ReadNextVarInt(data);
        if (holderId == 0)
        {
            DataTypes.ReadNextString(data); // ResourceLocation
            var hasFixedRange = DataTypes.ReadNextBool(data);
            if (hasFixedRange)
                DataTypes.ReadNextFloat(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
