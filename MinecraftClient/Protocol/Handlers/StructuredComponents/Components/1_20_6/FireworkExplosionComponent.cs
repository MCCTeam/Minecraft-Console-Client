using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class FireworkExplosionComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public FireworkExplosionSubComponent? FireworkExplosionSubComponent { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        FireworkExplosionSubComponent = (FireworkExplosionSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.FireworkExplosion, data);
    }

    public override Queue<byte> Serialize()
    {
        return FireworkExplosionSubComponent!.Serialize();
    }
}