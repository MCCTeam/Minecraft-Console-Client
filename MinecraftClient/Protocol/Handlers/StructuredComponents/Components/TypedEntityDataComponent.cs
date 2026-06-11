using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components;

public class TypedEntityDataComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int EntityTypeId { get; set; }
    public Dictionary<string, object>? Nbt { get; set; }

    public override void Parse(Queue<byte> data)
    {
        EntityTypeId = DataTypes.ReadNextVarInt(data);
        Nbt = DataTypes.ReadNextNbt(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(EntityTypeId));
        data.AddRange(DataTypes.GetNbt(Nbt));
        return new Queue<byte>(data);
    }
}

public class TypedBlockEntityDataComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : TypedEntityDataComponent(dataTypes, itemPalette, subComponentRegistry)
{ }
