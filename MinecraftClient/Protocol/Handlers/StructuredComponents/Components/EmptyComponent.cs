using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components;

public class EmptyComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
    }

    public override Queue<byte> Serialize()
    {
        return new Queue<byte>();
    }
}