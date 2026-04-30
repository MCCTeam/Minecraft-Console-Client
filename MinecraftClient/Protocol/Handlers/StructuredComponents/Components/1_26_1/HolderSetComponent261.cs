using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_26_1;

public class HolderSetComponent261(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public string? TagKey { get; set; }
    public List<int> HolderIds { get; } = [];

    public override void Parse(Queue<byte> data)
    {
        var count = DataTypes.ReadNextVarInt(data) - 1;
        if (count == -1)
        {
            TagKey = DataTypes.ReadNextString(data);
            return;
        }

        for (var i = 0; i < count; i++)
            HolderIds.Add(DataTypes.ReadNextVarInt(data));
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        if (TagKey is not null)
        {
            bytes.AddRange(DataTypes.GetVarInt(0));
            bytes.AddRange(DataTypes.GetString(TagKey));
            return new Queue<byte>(bytes);
        }

        bytes.AddRange(DataTypes.GetVarInt(HolderIds.Count + 1));
        foreach (var holderId in HolderIds)
            bytes.AddRange(DataTypes.GetVarInt(holderId));

        return new Queue<byte>(bytes);
    }
}
