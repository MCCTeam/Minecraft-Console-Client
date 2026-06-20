using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class PaintingVariantHolderComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
        // Holder<PaintingVariant>: VarInt discriminator
        var holderId = DataTypes.ReadNextVarInt(data);
        if (holderId == 0)
        {
            // Inline PaintingVariant: VarInt width + VarInt height + ResourceLocation assetId
            DataTypes.ReadNextVarInt(data); // width
            DataTypes.ReadNextVarInt(data); // height
            DataTypes.ReadNextString(data); // assetId

            // Optional<Component> title
            if (DataTypes.ReadNextBool(data))
                DataTypes.ReadNextNbt(data);

            // Optional<Component> author
            if (DataTypes.ReadNextBool(data))
                DataTypes.ReadNextNbt(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
