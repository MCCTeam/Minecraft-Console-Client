using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components;

public class TestComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, subComponentRegistry)
{
    public int TestInt { get; set; }
    public string TestString { get; set; } = null!;

    public TestSubComonent TestSubComonent { get; set; } = null!;
    
    public override void Parse(Queue<byte> data)
    {
        TestInt = dataTypes.ReadNextVarInt(data);
        TestString = dataTypes.ReadNextString(data);
        TestSubComonent = (SubComponentRegistry.ParseSubComponent("TestSubComponent", data) as TestSubComonent)!;
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(TestInt));
        data.AddRange(DataTypes.GetString(TestString));
        data.AddRange(TestSubComonent.Serialize());
        return new Queue<byte>(data);
    }
}