using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class ProfileComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool HasName { get; set; }
    public string? Name { get; set; } = null!;
    public bool HasUniqueId { get; set; }
    public Guid Uuid { get; set; }
    public int NumberOfProperties { get; set; }
    public List<ProfileProperty> ProfileProperties { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        HasName = dataTypes.ReadNextBool(data);
        
        if(HasName)
            Name = dataTypes.ReadNextString(data);

        HasUniqueId = dataTypes.ReadNextBool(data);

        if (HasUniqueId)
            Uuid = dataTypes.ReadNextUUID(data);

        NumberOfProperties = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < NumberOfProperties; i++)
        {
            var propertyName = dataTypes.ReadNextString(data);
            var propertyValue = dataTypes.ReadNextString(data);
            var hasSignature = dataTypes.ReadNextBool(data);
            var signature = hasSignature ? dataTypes.ReadNextString(data) : null;
            
            ProfileProperties.Add(new ProfileProperty(propertyName, propertyValue, hasSignature, signature));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        
        data.AddRange(DataTypes.GetBool(HasName));
        if (HasName)
        {
            if (string.IsNullOrEmpty(Name))
                throw new NullReferenceException("Can't serialize the ProfileComponent because the Name is null/empty!");
                
            data.AddRange(DataTypes.GetString(Name));
        }
        
        if (HasUniqueId)
            data.AddRange(DataTypes.GetUUID(Uuid));

        if (NumberOfProperties > 0)
        {
            if(NumberOfProperties != ProfileProperties.Count)
                throw new Exception("Can't serialize the ProfileComponent because the NumberOfProperties and ProfileProperties.Count differ!");

            foreach (var profileProperty in ProfileProperties)
            {
                data.AddRange(DataTypes.GetString(profileProperty.Name));
                data.AddRange(DataTypes.GetString(profileProperty.Value));
                data.AddRange(DataTypes.GetBool(profileProperty.HasSignature));
                if (profileProperty.HasSignature)
                {
                    if(string.IsNullOrEmpty(profileProperty.Signature))
                        throw new NullReferenceException("Can't serialize the ProfileComponent because HasSignature is true, but the Signature is null/empty!");
                    
                    data.AddRange(DataTypes.GetString(profileProperty.Signature));
                }
            }
        }
        
        return new Queue<byte>(data);
    }
}

public record ProfileProperty(string Name, string Value, bool HasSignature, string? Signature);