using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries;

public class StructuredComponentsRegistry1206 : StructuredComponentRegistry
{
    public StructuredComponentsRegistry1206(SubComponentRegistry subComponentRegistry, DataTypes dataTypes) : base(
        subComponentRegistry, dataTypes)
    {
        RegisterComponent<TestComponent>(0, "minecraft:test");
    }
}