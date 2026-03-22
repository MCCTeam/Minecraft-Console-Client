using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;

public class SoundEventSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int Type { get; set; }
    public string? SoundName { get; set; }
    public bool HasFixedRange { get; set; }
    public float FixedRange { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        Type = dataTypes.ReadNextVarInt(data);

        if (Type != 0) return;
        
        SoundName = dataTypes.ReadNextString(data);
        HasFixedRange = dataTypes.ReadNextBool(data);

        if (HasFixedRange)
            FixedRange = dataTypes.ReadNextFloat(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Type));

        if (Type != 0) return new Queue<byte>(data);
        
        if (string.IsNullOrEmpty(SoundName?.Trim()))
            throw new ArgumentNullException($"Can not serialize SoundEventSubComponent due to SoundName being null or empty!");
            
        data.AddRange(DataTypes.GetString(SoundName));
        data.AddRange(DataTypes.GetBool(HasFixedRange));
            
        if(HasFixedRange)
            data.AddRange(DataTypes.GetFloat(FixedRange));

        return new Queue<byte>(data);
    }
}