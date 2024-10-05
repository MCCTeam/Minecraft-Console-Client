using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class WrittenBlookContentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public string RawTitle { get; set; } = null!;
    public bool HasFilteredTitle { get; set; }
    public string? FilteredTitle { get; set; }
    public string Author { get; set; } = null!;
    public int Generation { get; set; }
    public int NumberOfPages { get; set; }
    public List<BookPage> Pages { get; set; } = [];
    public bool Resolved { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        RawTitle = ChatParser.ParseText(dataTypes.ReadNextString(data));
        HasFilteredTitle = dataTypes.ReadNextBool(data);

        if (HasFilteredTitle)
            FilteredTitle = dataTypes.ReadNextString(data);
        
        Author = dataTypes.ReadNextString(data);
        Generation = dataTypes.ReadNextVarInt(data);
        NumberOfPages = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfPages; i++)
        {
            var rawContent = ChatParser.ParseText(dataTypes.ReadNextString(data));
            var hasFilteredContent = dataTypes.ReadNextBool(data);
            var filteredContent = null as string;
            
            if(hasFilteredContent)
                filteredContent = dataTypes.ReadNextString(data);
            
            Pages.Add(new BookPage(rawContent, hasFilteredContent, filteredContent));
        }

        Resolved = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        
        data.AddRange(DataTypes.GetString(RawTitle));
        data.AddRange(DataTypes.GetBool(HasFilteredTitle));

        if (HasFilteredTitle)
        {
            if(FilteredTitle is null)
                throw new InvalidOperationException("Can not setialize WrittenBlookContentComponent1206 because HasFilteredTitle is true but FilteredTitle is null!");
            
            data.AddRange(DataTypes.GetString(FilteredTitle));
        }
        
        data.AddRange(DataTypes.GetString(Author));
        data.AddRange(DataTypes.GetVarInt(Generation));
        data.AddRange(DataTypes.GetVarInt(NumberOfPages));

        if (NumberOfPages != Pages.Count)
            throw new InvalidOperationException("Can not setialize WrittenBlookContentComponent1206 because NumberOfPages != Pages.Count!");

        foreach (var page in Pages)
        {
            data.AddRange(DataTypes.GetString(page.RawContent));
            data.AddRange(DataTypes.GetBool(page.HasFilteredContent));

            if (page.HasFilteredContent)
            {
                if(page.FilteredContent is null)
                    throw new InvalidOperationException("Can not setialize WrittenBlookContentComponent1206 because page.HasFilteredContent = true, but FilteredContent is null!");
                
                data.AddRange(DataTypes.GetString(page.FilteredContent));
            }
        }
        data.AddRange(DataTypes.GetBool(Resolved));
        return new Queue<byte>(data);
    }
}