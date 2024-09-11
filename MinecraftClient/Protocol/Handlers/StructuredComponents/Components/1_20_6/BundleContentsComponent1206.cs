using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class BundleContentsComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfItems { get; set; }
    public List<Item?> Items { get; set; } = [];

    public override void Parse(Queue<byte> data)
    {
        NumberOfItems = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfItems; i++)
            Items.Add(dataTypes.ReadNextItemSlot(data, itemPalette));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfItems));

        if (NumberOfItems != Items.Count)
            throw new ArgumentNullException($"Cannot serialize BundleContentsComponent1206 because NumberOfItems != Items.Count!");
            
        foreach (var item in Items.OfType<Item>())
            data.AddRange(DataTypes.GetItemSlot(item, itemPalette));

        return new Queue<byte>(data);
    }
}