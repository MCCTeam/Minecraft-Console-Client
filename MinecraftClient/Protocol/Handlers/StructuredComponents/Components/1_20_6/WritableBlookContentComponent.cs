using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class WritableBlookContentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public List<BookPage> Pages { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        var count = DataTypes.ReadNextVarInt(data);

        for (var i = 0; i < count; i++)
        {
            var rawContent = DataTypes.ReadNextString(data);
            var hasFilteredContent = DataTypes.ReadNextBool(data);
            var filteredContent = null as string;
            
            if(hasFilteredContent)
                filteredContent = DataTypes.ReadNextString(data);
            
            Pages.Add(new BookPage(rawContent, hasFilteredContent, filteredContent));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        
        data.AddRange(DataTypes.GetVarInt(Pages.Count));

        foreach (var page in Pages)
        {
            data.AddRange(DataTypes.GetString(page.RawContent));
            data.AddRange(DataTypes.GetBool(page.HasFilteredContent));

            if (page.HasFilteredContent)
            {
                if(page.FilteredContent is null)
                    throw new InvalidOperationException("Can not serialize WritableBlookContentComponent because page.HasFilteredContent = true, but FilteredContent is null!");
                
                data.AddRange(DataTypes.GetString(page.FilteredContent));
            }
        }

        return new Queue<byte>(data);
    }
}