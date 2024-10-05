using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class ContainerComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfItems { get; set; }
    public List<Item> Items { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfItems = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < NumberOfItems; i++)
        {
            var item = dataTypes.ReadNextItemSlot(data, ItemPalette);

            if (item is null)
                continue;
            
            Items.Add(item);
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfItems));
        for (var i = 0; i < NumberOfItems; i++)
            data.AddRange(DataTypes.GetItemSlot(Items[i], itemPalette));
            
        return new Queue<byte>(data);
    }
}