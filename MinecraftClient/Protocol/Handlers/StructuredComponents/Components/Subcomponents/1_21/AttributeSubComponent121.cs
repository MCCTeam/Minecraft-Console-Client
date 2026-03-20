using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;

public class AttributeSubComponent121(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int TypeId { get; set; }
    public string? ResourceLocation { get; set; }
    public double Value { get; set; }
    public int Operation { get; set; }
    public int Slot { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        TypeId = dataTypes.ReadNextVarInt(data);
        ResourceLocation = dataTypes.ReadNextString(data);
        Value = dataTypes.ReadNextDouble(data);
        Operation = dataTypes.ReadNextVarInt(data);
        Slot = dataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(TypeId));
        
        if (string.IsNullOrEmpty(ResourceLocation?.Trim()))
            throw new ArgumentNullException($"Can not serialize AttributeSubComponent121 due to ResourceLocation being null or empty!");
        
        data.AddRange(DataTypes.GetString(ResourceLocation));
        data.AddRange(DataTypes.GetDouble(Value));
        data.AddRange(DataTypes.GetVarInt(Operation));
        data.AddRange(DataTypes.GetVarInt(Slot));
        return new Queue<byte>(data);
    }
}
