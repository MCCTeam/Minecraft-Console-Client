using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class ItemNameComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public string ItemName { get; set; } = string.Empty;
    public Dictionary<string, object>? ItemNameNbt { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        ItemNameNbt = dataTypes.ReadNextNbt(data);
        ItemName = ChatParser.ParseText(ItemNameNbt);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetNbt(ItemNameNbt));
        return new Queue<byte>(data);
    }
}