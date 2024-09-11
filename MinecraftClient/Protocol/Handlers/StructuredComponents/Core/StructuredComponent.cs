using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

public abstract class StructuredComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
{
    protected DataTypes DataTypes { get; private set; } = dataTypes;
    protected SubComponentRegistry SubComponentRegistry { get; private set; } = subComponentRegistry;
    protected ItemPalette ItemPalette { get; private set; } = itemPalette;
    
    public abstract void Parse(Queue<byte> data);
    public abstract Queue<byte> Serialize();
}