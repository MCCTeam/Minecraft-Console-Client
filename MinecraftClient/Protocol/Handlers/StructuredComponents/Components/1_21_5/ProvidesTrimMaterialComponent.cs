using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class ProvidesTrimMaterialComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
        // EitherHolder<TrimMaterial>: Bool + (Holder<TrimMaterial> OR ResourceLocation)
        var isHolder = dataTypes.ReadNextBool(data);
        if (isHolder)
        {
            var holderId = dataTypes.ReadNextVarInt(data);
            if (holderId == 0)
            {
                // Inline TrimMaterial: MaterialAssetGroup + Component description
                // MaterialAssetGroup: string + map<ResourceLocation, string>
                dataTypes.ReadNextString(data); // base asset suffix
                var overrideCount = dataTypes.ReadNextVarInt(data);
                for (var i = 0; i < overrideCount; i++)
                {
                    dataTypes.ReadNextString(data); // ResourceKey<EquipmentAsset>
                    dataTypes.ReadNextString(data); // override suffix
                }
                // description Component
                dataTypes.ReadNextString(data);
            }
        }
        else
        {
            dataTypes.ReadNextString(data); // ResourceLocation key
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
