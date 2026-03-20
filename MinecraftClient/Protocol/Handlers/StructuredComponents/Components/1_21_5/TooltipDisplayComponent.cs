using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class TooltipDisplayComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool HideTooltip { get; set; }
    public List<int> HiddenComponentIds { get; set; } = [];

    public override void Parse(Queue<byte> data)
    {
        HideTooltip = dataTypes.ReadNextBool(data);
        var count = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < count; i++)
            HiddenComponentIds.Add(dataTypes.ReadNextVarInt(data));
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetBool(HideTooltip));
        bytes.AddRange(DataTypes.GetVarInt(HiddenComponentIds.Count));
        foreach (var id in HiddenComponentIds)
            bytes.AddRange(DataTypes.GetVarInt(id));
        return new Queue<byte>(bytes);
    }
}
