using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class PropertySubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public string? Name { get; set; }
    public bool IsExactMatch { get; set; }
    public string? ExactValue { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        Name = dataTypes.ReadNextString(data);
        IsExactMatch = dataTypes.ReadNextBool(data);

        if (IsExactMatch)
        {
            ExactValue = dataTypes.ReadNextString(data);
        }
        else
        {
            MinValue = dataTypes.ReadNextBool(data) ? dataTypes.ReadNextString(data) : null;
            MaxValue = dataTypes.ReadNextBool(data) ? dataTypes.ReadNextString(data) : null;
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();

        if (string.IsNullOrEmpty(Name?.Trim()))
            throw new ArgumentNullException($"Can not serialize a Property sub-component if the Name is null or empty!");
        
        data.AddRange(DataTypes.GetString(Name));
        data.AddRange(DataTypes.GetBool(IsExactMatch));

        if (IsExactMatch)
        {
            if (string.IsNullOrEmpty(ExactValue?.Trim()))
                throw new ArgumentNullException($"Can not serialize a Property sub-component if the ExactValue is null or empty when the type is Exact Match!");
            
            data.AddRange(DataTypes.GetString(ExactValue));
        }
        else
        {
            data.AddRange(DataTypes.GetBool(MinValue is not null));
            if (MinValue is not null)
                data.AddRange(DataTypes.GetString(MinValue));
            
            data.AddRange(DataTypes.GetBool(MaxValue is not null));
            if (MaxValue is not null)
                data.AddRange(DataTypes.GetString(MaxValue));
        }
        
        return new Queue<byte>(data);
    }
}