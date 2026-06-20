using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class RepairableComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Type { get; set; }
    public string? TagName { get; set; }
    public List<int>? ItemIds { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Type = DataTypes.ReadNextVarInt(data);
        if (Type == 0)
        {
            TagName = DataTypes.ReadNextString(data);
        }
        else
        {
            ItemIds = new List<int>();
            for (var i = 0; i < Type - 1; i++)
                ItemIds.Add(DataTypes.ReadNextVarInt(data));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Type));
        if (Type == 0 && TagName is not null)
        {
            data.AddRange(DataTypes.GetString(TagName));
        }
        else if (ItemIds is not null)
        {
            foreach (var id in ItemIds)
                data.AddRange(DataTypes.GetVarInt(id));
        }
        return new Queue<byte>(data);
    }
}
