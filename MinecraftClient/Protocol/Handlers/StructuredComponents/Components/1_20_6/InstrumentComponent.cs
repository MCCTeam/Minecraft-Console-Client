using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class InstrumentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    // holder ID: 0 = inline instrument data, N>0 = registry reference (id = N-1)
    public int InstrumentHolderId { get; set; }

    // Inline instrument fields (only when InstrumentHolderId == 0):
    // holder ID for SoundEvent: 0 = inline sound, N>0 = registry reference (id = N-1)
    public int SoundEventHolderId { get; set; }
    // Inline SoundEvent fields (only when SoundEventHolderId == 0):
    public string? SoundLocation { get; set; }
    public bool HasFixedRange { get; set; }
    public float FixedRange { get; set; }

    public int UseDuration { get; set; }
    public float Range { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        InstrumentHolderId = dataTypes.ReadNextVarInt(data);

        if (InstrumentHolderId == 0)
        {
            SoundEventHolderId = dataTypes.ReadNextVarInt(data);

            if (SoundEventHolderId == 0)
            {
                SoundLocation = dataTypes.ReadNextString(data);
                HasFixedRange = dataTypes.ReadNextBool(data);
                if (HasFixedRange)
                    FixedRange = dataTypes.ReadNextFloat(data);
            }

            UseDuration = dataTypes.ReadNextVarInt(data);
            Range = dataTypes.ReadNextFloat(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(InstrumentHolderId));

        if (InstrumentHolderId == 0)
        {
            data.AddRange(DataTypes.GetVarInt(SoundEventHolderId));

            if (SoundEventHolderId == 0)
            {
                data.AddRange(DataTypes.GetString(SoundLocation ?? ""));
                data.AddRange(DataTypes.GetBool(HasFixedRange));
                if (HasFixedRange)
                    data.AddRange(DataTypes.GetFloat(FixedRange));
            }

            data.AddRange(DataTypes.GetVarInt(UseDuration));
            data.AddRange(DataTypes.GetFloat(Range));
        }

        return new Queue<byte>(data);
    }
}
