using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

public abstract class StructuredComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
{
    protected DataTypes DataTypes { get; private set; } = dataTypes;
    protected SubComponentRegistry SubComponentRegistry { get; private set; } = subComponentRegistry;
    protected ItemPalette ItemPalette { get; private set; } = itemPalette;

    /// <summary>
    /// The registry type ID assigned during parsing, used for round-trip serialization.
    /// </summary>
    public int TypeId { get; set; } = -1;

    public abstract void Parse(Queue<byte> data);
    public abstract Queue<byte> Serialize();
}