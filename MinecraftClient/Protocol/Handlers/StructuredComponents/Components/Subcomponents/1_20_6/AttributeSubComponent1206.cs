using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class AttributeSubComponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int TypeId { get; set; }
    public Guid Uuid { get; set; }
    public string? Name { get; set; }
    public double Value { get; set; }
    public int Operation { get; set; }
    public int Slot { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        TypeId = dataTypes.ReadNextVarInt(data);
        Uuid = dataTypes.ReadNextUUID(data);
        Name = dataTypes.ReadNextString(data);
        Value = dataTypes.ReadNextDouble(data);
        Operation = dataTypes.ReadNextVarInt(data);
        Slot = dataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(TypeId));
        data.AddRange(DataTypes.GetUUID(Uuid));
        
        if (string.IsNullOrEmpty(Name?.Trim()))
            throw new ArgumentNullException($"Can not serialize AttributeSubComponent due to Name being null or empty!");
        
        data.AddRange(DataTypes.GetString(Name));
        data.AddRange(DataTypes.GetDouble(Value));
        data.AddRange(DataTypes.GetVarInt(Operation));
        data.AddRange(DataTypes.GetVarInt(Slot));
        return new Queue<byte>(data);
    }
}