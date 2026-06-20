using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class SoundEventHolderComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int HolderId { get; set; }
    public string? SoundLocation { get; set; }
    public bool HasFixedRange { get; set; }
    public float FixedRange { get; set; }

    public override void Parse(Queue<byte> data)
    {
        HolderId = DataTypes.ReadNextVarInt(data);
        if (HolderId == 0)
        {
            SoundLocation = DataTypes.ReadNextString(data);
            HasFixedRange = DataTypes.ReadNextBool(data);
            if (HasFixedRange)
                FixedRange = DataTypes.ReadNextFloat(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetVarInt(HolderId));
        if (HolderId == 0)
        {
            bytes.AddRange(DataTypes.GetString(SoundLocation ?? ""));
            bytes.AddRange(DataTypes.GetBool(HasFixedRange));
            if (HasFixedRange)
                bytes.AddRange(DataTypes.GetFloat(FixedRange));
        }
        return new Queue<byte>(bytes);
    }
}
