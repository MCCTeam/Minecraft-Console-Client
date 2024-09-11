using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components;

public class EmptyComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}