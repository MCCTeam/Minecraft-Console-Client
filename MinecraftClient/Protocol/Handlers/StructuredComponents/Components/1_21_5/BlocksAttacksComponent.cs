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
        BlockDelaySeconds = dataTypes.ReadNextFloat(data);
        DisableCooldownScale = dataTypes.ReadNextFloat(data);

        var reductionCount = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < reductionCount; i++)
        {
            var horizontalBlockingAngle = dataTypes.ReadNextFloat(data);

            var hasTypeFilter = dataTypes.ReadNextBool(data);
            if (hasTypeFilter)
                ReadHolderSet(data);

            var baseDmg = dataTypes.ReadNextFloat(data);
            var factor = dataTypes.ReadNextFloat(data);
        }

        ItemDamageThreshold = dataTypes.ReadNextFloat(data);
        ItemDamageBase = dataTypes.ReadNextFloat(data);
        ItemDamageFactor = dataTypes.ReadNextFloat(data);

        var hasBypassedBy = dataTypes.ReadNextBool(data);
        if (hasBypassedBy)
            dataTypes.ReadNextString(data); // TagKey<DamageType> as ResourceLocation

        var hasBlockSound = dataTypes.ReadNextBool(data);
        if (hasBlockSound)
            ReadSoundEventHolder(data);

        var hasDisableSound = dataTypes.ReadNextBool(data);
        if (hasDisableSound)
            ReadSoundEventHolder(data);
    }

    private void ReadHolderSet(Queue<byte> data)
    {
        var sizeOrTag = dataTypes.ReadNextVarInt(data);
        if (sizeOrTag == 0)
        {
            dataTypes.ReadNextString(data); // Tag ResourceLocation
        }
        else
        {
            var count = sizeOrTag - 1;
            for (var i = 0; i < count; i++)
                dataTypes.ReadNextVarInt(data); // Holder<DamageType> registry ids
        }
    }

    private void ReadSoundEventHolder(Queue<byte> data)
    {
        var holderId = dataTypes.ReadNextVarInt(data);
        if (holderId == 0)
        {
            dataTypes.ReadNextString(data); // ResourceLocation
            var hasFixedRange = dataTypes.ReadNextBool(data);
            if (hasFixedRange)
                dataTypes.ReadNextFloat(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
