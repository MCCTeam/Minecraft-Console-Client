using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class HideTooltipComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : EmptyComponent(dataTypes, itemPalette, subComponentRegistry);