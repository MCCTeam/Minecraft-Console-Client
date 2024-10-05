using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class WritableBlookContentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfPages { get; set; }
    public List<BookPage> Pages { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfPages = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfPages; i++)
        {
            var rawContent = dataTypes.ReadNextString(data);
            var hasFilteredContent = dataTypes.ReadNextBool(data);
            var filteredContent = null as string;
            
            if(hasFilteredContent)
                filteredContent = dataTypes.ReadNextString(data);
            
            Pages.Add(new BookPage(rawContent, hasFilteredContent, filteredContent));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        
        data.AddRange(DataTypes.GetVarInt(NumberOfPages));

        if (NumberOfPages != Pages.Count)
            throw new InvalidOperationException("Can not setialize WritableBlookContentComponent1206 because NumberOfPages != Pages.Count!");

        foreach (var page in Pages)
        {
            data.AddRange(DataTypes.GetString(page.RawContent));
            data.AddRange(DataTypes.GetBool(page.HasFilteredContent));

            if (page.HasFilteredContent)
            {
                if(page.FilteredContent is null)
                    throw new InvalidOperationException("Can not setialize WritableBlookContentComponent1206 because page.HasFilteredContent = true, but FilteredContent is null!");
                
                data.AddRange(DataTypes.GetString(page.FilteredContent));
            }
        }

        return new Queue<byte>(data);
    }
}