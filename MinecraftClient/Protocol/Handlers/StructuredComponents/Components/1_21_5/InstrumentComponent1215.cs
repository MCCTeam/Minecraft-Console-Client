using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class InstrumentComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
        // EitherHolder<Instrument>: Bool + (Holder<Instrument> OR ResourceLocation)
        var isHolder = DataTypes.ReadNextBool(data);
        if (isHolder)
        {
            var holderId = DataTypes.ReadNextVarInt(data);
            if (holderId == 0)
            {
                // Inline Instrument: SoundEvent holder + VarInt useDuration + Float range + Component description
                var soundHolderId = DataTypes.ReadNextVarInt(data);
                if (soundHolderId == 0)
                {
                    DataTypes.ReadNextString(data); // ResourceLocation
                    var hasFixedRange = DataTypes.ReadNextBool(data);
                    if (hasFixedRange)
                        DataTypes.ReadNextFloat(data);
                }
                DataTypes.ReadNextVarInt(data); // useDuration
                DataTypes.ReadNextFloat(data); // range
                DataTypes.ReadNextString(data); // description (Component as JSON string)
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
