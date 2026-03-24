using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class AttributeSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int TypeId { get; set; }
    public Guid Uuid { get; set; }
    public string? Name { get; set; }
    public double Value { get; set; }
    public int Operation { get; set; }
    public int Slot { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        TypeId = DataTypes.ReadNextVarInt(data);
        Uuid = DataTypes.ReadNextUUID(data);
        Name = DataTypes.ReadNextString(data);
        Value = DataTypes.ReadNextDouble(data);
        Operation = DataTypes.ReadNextVarInt(data);
        Slot = DataTypes.ReadNextVarInt(data);
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