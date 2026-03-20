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
        var holderId = dataTypes.ReadNextVarInt(data);
        if (holderId == 0)
        {
            // Inline PaintingVariant: VarInt width + VarInt height + ResourceLocation assetId
            dataTypes.ReadNextVarInt(data); // width
            dataTypes.ReadNextVarInt(data); // height
            dataTypes.ReadNextString(data); // assetId

            // Optional<Component> title
            if (dataTypes.ReadNextBool(data))
                dataTypes.ReadNextString(data);

            // Optional<Component> author
            if (dataTypes.ReadNextBool(data))
                dataTypes.ReadNextString(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
