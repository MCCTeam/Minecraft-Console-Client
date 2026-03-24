using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class ContainerComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public List<Item?> Items { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        var count = DataTypes.ReadNextVarInt(data);
        for (var i = 0; i < count; i++)
            Items.Add(DataTypes.ReadNextItemSlot(data, ItemPalette));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Items.Count));
        foreach (var item in Items)
            data.AddRange(DataTypes.GetItemSlot(item, ItemPalette));
            
        return new Queue<byte>(data);
    }
}
