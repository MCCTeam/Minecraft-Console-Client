using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class UseRemainderComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public Item? ConvertInto { get; set; }

    public override void Parse(Queue<byte> data)
    {
        ConvertInto = dataTypes.ReadNextItemSlot(data, ItemPalette);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(dataTypes.GetItemSlot(ConvertInto, ItemPalette));
        return new Queue<byte>(data);
    }
}
