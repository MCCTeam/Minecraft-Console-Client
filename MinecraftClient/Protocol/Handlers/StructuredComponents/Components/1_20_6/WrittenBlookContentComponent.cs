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
        RawTitle = dataTypes.ReadNextString(data);
        HasFilteredTitle = dataTypes.ReadNextBool(data);

        if (HasFilteredTitle)
            FilteredTitle = dataTypes.ReadNextString(data);
        
        Author = dataTypes.ReadNextString(data);
        Generation = dataTypes.ReadNextVarInt(data);
        NumberOfPages = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfPages; i++)
        {
            var rawContentNbt = dataTypes.ReadNextNbt(data);
            var rawContent = ChatParser.ParseText(rawContentNbt);
            var hasFilteredContent = dataTypes.ReadNextBool(data);
            Dictionary<string, object>? filteredContentNbt = null;
            string? filteredContent = null;
            
            if (hasFilteredContent)
            {
                filteredContentNbt = dataTypes.ReadNextNbt(data);
                filteredContent = ChatParser.ParseText(filteredContentNbt);
            }
            
            Pages.Add(new BookPage(rawContent, hasFilteredContent, filteredContent, rawContentNbt, filteredContentNbt));
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
                throw new InvalidOperationException("Can not serialize WrittenBookContentComponent because HasFilteredTitle is true but FilteredTitle is null!");
            
            data.AddRange(DataTypes.GetString(FilteredTitle));
        }
        
        data.AddRange(DataTypes.GetString(Author));
        data.AddRange(DataTypes.GetVarInt(Generation));
        data.AddRange(DataTypes.GetVarInt(Pages.Count));

        foreach (var page in Pages)
        {
            data.AddRange(DataTypes.GetNbt(page.RawContentNbt));
            data.AddRange(DataTypes.GetBool(page.HasFilteredContent));

            if (page.HasFilteredContent)
                data.AddRange(DataTypes.GetNbt(page.FilteredContentNbt));
        }
        data.AddRange(DataTypes.GetBool(Resolved));
        return new Queue<byte>(data);
    }
}