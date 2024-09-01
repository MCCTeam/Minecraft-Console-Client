using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;

public class TestSubComponentRegistry : SubComponentRegistry
{
    public TestSubComponentRegistry(DataTypes dataTypes) : base(dataTypes)
    {
        RegisterSubComponent<TestSubComonent>("TestSubcomponent");
    }
}