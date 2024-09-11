using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

public abstract class SubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
{
    protected DataTypes DataTypes { get; private set; } = dataTypes;
    protected SubComponentRegistry SubComponentRegistry { get; private set; } = subComponentRegistry;

    protected abstract void Parse(Queue<byte> data);
    public abstract Queue<byte> Serialize();
}