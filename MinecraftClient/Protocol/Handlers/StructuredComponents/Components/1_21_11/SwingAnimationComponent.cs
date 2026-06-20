using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

public class SwingAnimationComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int AnimationType { get; set; }
    public int Duration { get; set; }

    public override void Parse(Queue<byte> data)
    {
        AnimationType = DataTypes.ReadNextVarInt(data);
        Duration = DataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetVarInt(AnimationType));
        bytes.AddRange(DataTypes.GetVarInt(Duration));
        return new Queue<byte>(bytes);
    }
}
