using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

public abstract class SubComponent(DataTypes dataTypes)
{
    protected DataTypes DataTypes { get; private set; } = dataTypes;

    public abstract void Parse(Queue<byte> data);
    public abstract Queue<byte> Serialize();
}