using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

/// <summary>
/// 1.21.5+ dyed_color only carries the RGB integer.
/// Tooltip visibility moved to minecraft:tooltip_display.
/// </summary>
public class DyeColorComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Color { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Color = DataTypes.ReadNextInt(data);
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>(DataTypes.GetInt(Color));
    }
}
