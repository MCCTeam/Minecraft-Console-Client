using System.Collections.Generic;
using System.Text.Json.Serialization;
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
    [JsonIgnore]
    public int TypeId { get; set; } = -1;

    /// <summary>
    /// The registry name assigned during parsing (e.g. "minecraft:custom_data"), used for NBT serialization.
    /// </summary>
    [JsonIgnore]
    public string ComponentName { get; set; } = string.Empty;

    public abstract void Parse(Queue<byte> data);
    public abstract Queue<byte> Serialize();
}