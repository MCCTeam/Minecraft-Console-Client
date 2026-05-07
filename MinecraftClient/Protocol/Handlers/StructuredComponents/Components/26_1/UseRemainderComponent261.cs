using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_1;

public class UseRemainderComponent261(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public Item? ConvertInto { get; set; }

    public override void Parse(Queue<byte> data)
    {
        ConvertInto = DataTypes.ReadNextItemStackTemplate(data, ItemPalette);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        if (ConvertInto is not null && !ConvertInto.IsEmpty)
            data.AddRange(DataTypes.GetItemStackTemplate(ConvertInto, ItemPalette));
        return new Queue<byte>(data);
    }
}
