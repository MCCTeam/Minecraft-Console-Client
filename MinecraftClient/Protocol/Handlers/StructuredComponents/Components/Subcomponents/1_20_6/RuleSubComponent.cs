using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class RuleSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public BlockSetSubcomponent Blocks { get; set; }
    public bool HasSpeed { get; set; }
    public float Speed { get; set; }
    public bool HasCorrectDropForBlocks { get; set; }
    public bool CorrectDropForBlocks { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        Blocks = (BlockSetSubcomponent)SubComponentRegistry.ParseSubComponent(SubComponents.BlockSet, data);
        HasSpeed = DataTypes.ReadNextBool(data);
        
        if(HasSpeed)
            Speed = DataTypes.ReadNextFloat(data);
        
        HasCorrectDropForBlocks = DataTypes.ReadNextBool(data);
        
        if(HasCorrectDropForBlocks)
            CorrectDropForBlocks = DataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(Blocks.Serialize());
        data.AddRange(DataTypes.GetBool(HasSpeed));
        if(HasSpeed)
            data.AddRange(DataTypes.GetFloat(Speed));

        data.AddRange(DataTypes.GetBool(HasCorrectDropForBlocks));
        if(HasCorrectDropForBlocks)
            data.AddRange(DataTypes.GetBool(CorrectDropForBlocks));
        
        return new Queue<byte>(data);
    }
}