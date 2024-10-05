using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class DyeColorComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Color { get; set; }
    public bool ShowInTooltip { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        Color = dataTypes.ReadNextInt(data);
        ShowInTooltip = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetInt(Color));
        data.AddRange(DataTypes.GetBool(ShowInTooltip));
        return new Queue<byte>(data);
    }
}