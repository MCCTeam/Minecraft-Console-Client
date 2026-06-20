using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_1;

public class TypedEntityDataComponent261(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : TypedEntityDataComponent(dataTypes, itemPalette, subComponentRegistry)
{ }

public class BlockEntityDataComponent261(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : TypedBlockEntityDataComponent(dataTypes, itemPalette, subComponentRegistry)
{ }
