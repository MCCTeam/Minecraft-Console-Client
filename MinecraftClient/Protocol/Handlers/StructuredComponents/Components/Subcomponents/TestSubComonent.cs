using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;

public class TestSubComonent(DataTypes dataTypes) : SubComponent(dataTypes)
{
    public int Test { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        Test = DataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        throw new System.NotImplementedException();
    }
}