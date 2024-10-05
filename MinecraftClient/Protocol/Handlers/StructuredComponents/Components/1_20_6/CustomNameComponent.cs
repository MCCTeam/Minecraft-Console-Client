using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class CustomNameComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public string CustomName { get; set; } = string.Empty;
    
    public override void Parse(Queue<byte> data)
    {
        CustomName = ChatParser.ParseText(dataTypes.ReadNextString(data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetString(CustomName));
        return new Queue<byte>(data);
    }
}