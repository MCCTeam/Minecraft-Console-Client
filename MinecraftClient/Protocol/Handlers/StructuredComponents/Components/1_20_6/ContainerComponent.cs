using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class ContainerComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfItems { get; set; }
    public List<Item?> Items { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfItems = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < NumberOfItems; i++)
            Items.Add(dataTypes.ReadNextItemSlot(data, ItemPalette));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Items.Count));
        foreach (var item in Items)
            data.AddRange(DataTypes.GetItemSlot(item, itemPalette));
            
        return new Queue<byte>(data);
    }
}
