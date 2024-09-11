using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class ItemNameComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public string ItemName { get; set; } = string.Empty;
    
    public override void Parse(Queue<byte> data)
    {
        ItemName = ChatParser.ParseText(dataTypes.ReadNextString(data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetString(ItemName));
        return new Queue<byte>(data);
    }
}