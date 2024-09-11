using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;

public class SubComponentRegistry1206 : SubComponentRegistry
{
    public SubComponentRegistry1206(DataTypes dataTypes) : base(dataTypes)
    {
        RegisterSubComponent<BlockPredicateSubcomponent1206>(SubComponents.BlockPredicate);
        RegisterSubComponent<BlockSetSubcomponent1206>(SubComponents.BlockSet);
        RegisterSubComponent<PropertySubComponent1206>(SubComponents.Property);
        RegisterSubComponent<AttributeSubComponent1206>(SubComponents.Attribute);
    }
}