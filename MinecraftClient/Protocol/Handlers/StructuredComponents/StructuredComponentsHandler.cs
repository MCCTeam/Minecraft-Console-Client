using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents;

public class StructuredComponentsHandler
{
    private StructuredComponentRegistry ComponentRegistry { get; }
    
    public StructuredComponentsHandler(
        int protocolVersion,
        DataTypes dataTypes,
        ItemPalette itemPalette)
    {
        // Get the appropriate subcomponent registry type based on the protocol version and then instantiate it
        var subcomponentRegistryType = protocolVersion switch
        {
            Protocol18Handler.MC_1_20_6_Version => typeof(SubComponentRegistry1206),
            _ => throw new NotSupportedException($"Protocol version {protocolVersion} is not supported for subcomponent registries!")
        };

        var subcomponentRegistry = Activator.CreateInstance(subcomponentRegistryType, dataTypes) as SubComponentRegistry 
                            ?? throw new InvalidOperationException($"Failed to instantiate a component registry for type {nameof(subcomponentRegistryType)}");
        
        // Get the appropriate component registry type based on the protocol version and then instantiate it
        var registryType = protocolVersion switch
        {
            Protocol18Handler.MC_1_20_6_Version => typeof(StructuredComponentsRegistry1206),
            _ => throw new NotSupportedException($"Protocol version {protocolVersion} is not supported for structured component registries!")
        };

        ComponentRegistry = Activator.CreateInstance(registryType, dataTypes, itemPalette, subcomponentRegistry) as StructuredComponentRegistry 
                            ?? throw new InvalidOperationException($"Failed to instantiate a component registry for type {nameof(registryType)}");
    }

    public StructuredComponent Parse(int componentId, Queue<byte> data)
    {
        return ComponentRegistry.ParseComponent(componentId, data);
    }
}