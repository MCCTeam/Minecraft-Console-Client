using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_26_1;

public class InstrumentComponent261(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int HolderId { get; set; }

    public override void Parse(Queue<byte> data)
    {
        HolderId = DataTypes.ReadNextVarInt(data);
        if (HolderId != 0)
            return;

        var soundHolderId = DataTypes.ReadNextVarInt(data);
        if (soundHolderId == 0)
        {
            DataTypes.ReadNextString(data); // ResourceLocation
            var hasFixedRange = DataTypes.ReadNextBool(data);
            if (hasFixedRange)
                DataTypes.ReadNextFloat(data);
        }

        DataTypes.ReadNextFloat(data); // useDuration
        DataTypes.ReadNextFloat(data); // range
        DataTypes.ReadNextNbt(data); // ComponentSerialization.STREAM_CODEC
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetVarInt(HolderId));
        return new Queue<byte>(bytes);
    }
}
