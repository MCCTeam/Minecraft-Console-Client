using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;

public class SubComponentRegistry121 : SubComponentRegistry1206
{
    public SubComponentRegistry121(DataTypes dataTypes) : base(dataTypes)
    {
        ReplaceSubComponent<AttributeSubComponent121>(SubComponents.Attribute);
        RegisterSubComponent<SoundEventSubComponent>(SubComponents.SoundEvent);
    }
}