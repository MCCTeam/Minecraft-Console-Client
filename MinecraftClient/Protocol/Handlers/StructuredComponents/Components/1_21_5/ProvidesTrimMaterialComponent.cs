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
        var isHolder = DataTypes.ReadNextBool(data);
        if (isHolder)
        {
            var holderId = DataTypes.ReadNextVarInt(data);
            if (holderId == 0)
            {
                // Inline TrimMaterial: MaterialAssetGroup + Component description
                // MaterialAssetGroup: string + map<ResourceLocation, string>
                DataTypes.ReadNextString(data); // base asset suffix
                var overrideCount = DataTypes.ReadNextVarInt(data);
                for (var i = 0; i < overrideCount; i++)
                {
                    DataTypes.ReadNextString(data); // ResourceKey<EquipmentAsset>
                    DataTypes.ReadNextString(data); // override suffix
                }
                // description Component
                DataTypes.ReadNextString(data);
            }
        }
        else
        {
            DataTypes.ReadNextString(data); // ResourceLocation key
        }
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}
