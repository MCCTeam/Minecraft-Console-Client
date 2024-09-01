using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

public abstract class StructuredComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
{
    protected DataTypes DataTypes { get; private set; } = dataTypes;
    protected SubComponentRegistry SubComponentRegistry { get; private set; } = subComponentRegistry;
    
    public abstract void Parse(Queue<byte> data);
    public abstract Queue<byte> Serialize();
}