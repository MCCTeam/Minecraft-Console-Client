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
        var isHolder = dataTypes.ReadNextBool(data);
        if (isHolder)
        {
            var holderId = dataTypes.ReadNextVarInt(data);
            if (holderId == 0)
            {
                // Inline Instrument: SoundEvent holder + VarInt useDuration + Float range + Component description
                var soundHolderId = dataTypes.ReadNextVarInt(data);
                if (soundHolderId == 0)
                {
                    dataTypes.ReadNextString(data); // ResourceLocation
                    var hasFixedRange = dataTypes.ReadNextBool(data);
                    if (hasFixedRange)
                        dataTypes.ReadNextFloat(data);
                }
                dataTypes.ReadNextVarInt(data); // useDuration
                dataTypes.ReadNextFloat(data); // range
                dataTypes.ReadNextString(data); // description (Component as JSON string)
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
