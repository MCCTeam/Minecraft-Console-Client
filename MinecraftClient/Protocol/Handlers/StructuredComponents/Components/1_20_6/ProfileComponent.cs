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
    public bool IsFullProfile { get; set; }
    public string? BodyAssetId { get; set; }
    public string? CapeAssetId { get; set; }
    public string? ElytraAssetId { get; set; }
    public ProfileSkinModel? Model { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        ResetState();

        if (dataTypes.ProtocolVersion >= Protocol18Handler.MC_1_21_9_Version)
        {
            ParseResolvableProfile(data);
            return;
        }

        ParseLegacyProfile(data);
    }

    public override Queue<byte> Serialize()
    {
        return dataTypes.ProtocolVersion >= Protocol18Handler.MC_1_21_9_Version
            ? SerializeResolvableProfile()
            : SerializeLegacyProfile();
    }

    private void ResetState()
    {
        HasName = false;
        Name = null;
        HasUniqueId = false;
        Uuid = Guid.Empty;
        NumberOfProperties = 0;
        ProfileProperties = [];
        IsFullProfile = false;
        BodyAssetId = null;
        CapeAssetId = null;
        ElytraAssetId = null;
        Model = null;
    }

    private void ParseLegacyProfile(Queue<byte> data)
    {
        HasName = dataTypes.ReadNextBool(data);

        if (HasName)
            Name = dataTypes.ReadNextString(data);

        HasUniqueId = dataTypes.ReadNextBool(data);

        if (HasUniqueId)
            Uuid = dataTypes.ReadNextUUID(data);

        NumberOfProperties = dataTypes.ReadNextVarInt(data);
        ProfileProperties = ReadProfileProperties(data, NumberOfProperties);
    }

    private void ParseResolvableProfile(Queue<byte> data)
    {
        IsFullProfile = dataTypes.ReadNextBool(data);

        if (IsFullProfile)
        {
            HasUniqueId = true;
            Uuid = dataTypes.ReadNextUUID(data);
            HasName = true;
            Name = dataTypes.ReadNextString(data);
            NumberOfProperties = dataTypes.ReadNextVarInt(data);
            ProfileProperties = ReadProfileProperties(data, NumberOfProperties);
        }
        else
        {
            HasName = dataTypes.ReadNextBool(data);
            if (HasName)
                Name = dataTypes.ReadNextString(data);

            HasUniqueId = dataTypes.ReadNextBool(data);
            if (HasUniqueId)
                Uuid = dataTypes.ReadNextUUID(data);

            NumberOfProperties = dataTypes.ReadNextVarInt(data);
            ProfileProperties = ReadProfileProperties(data, NumberOfProperties);
        }

        BodyAssetId = ReadOptionalResourceLocation(data);
        CapeAssetId = ReadOptionalResourceLocation(data);
        ElytraAssetId = ReadOptionalResourceLocation(data);

        if (dataTypes.ReadNextBool(data))
            Model = dataTypes.ReadNextBool(data) ? ProfileSkinModel.Slim : ProfileSkinModel.Wide;
    }

    private Queue<byte> SerializeLegacyProfile()
    {
        var data = new List<byte>();
        NumberOfProperties = ProfileProperties.Count;

        data.AddRange(DataTypes.GetBool(HasName));
        if (HasName)
        {
            if (string.IsNullOrEmpty(Name))
                throw new NullReferenceException("Can't serialize the ProfileComponent because the Name is null/empty!");

            data.AddRange(DataTypes.GetString(Name));
        }

        data.AddRange(DataTypes.GetBool(HasUniqueId));
        if (HasUniqueId)
            data.AddRange(DataTypes.GetUUID(Uuid));

        data.AddRange(DataTypes.GetVarInt(NumberOfProperties));
        SerializeProfileProperties(data);

        return new Queue<byte>(data);
    }

    private Queue<byte> SerializeResolvableProfile()
    {
        var data = new List<byte>();
        NumberOfProperties = ProfileProperties.Count;

        data.AddRange(DataTypes.GetBool(IsFullProfile));
        if (IsFullProfile)
        {
            if (!HasUniqueId)
                throw new NullReferenceException("Can't serialize the ProfileComponent because a full profile requires a UUID!");

            if (!HasName || string.IsNullOrEmpty(Name))
                throw new NullReferenceException("Can't serialize the ProfileComponent because a full profile requires a name!");

            data.AddRange(DataTypes.GetUUID(Uuid));
            data.AddRange(DataTypes.GetString(Name));
        }
        else
        {
            data.AddRange(DataTypes.GetBool(HasName));
            if (HasName)
            {
                if (string.IsNullOrEmpty(Name))
                    throw new NullReferenceException("Can't serialize the ProfileComponent because HasName is true, but the Name is null/empty!");

                data.AddRange(DataTypes.GetString(Name));
            }

            data.AddRange(DataTypes.GetBool(HasUniqueId));
            if (HasUniqueId)
                data.AddRange(DataTypes.GetUUID(Uuid));
        }

        data.AddRange(DataTypes.GetVarInt(NumberOfProperties));
        SerializeProfileProperties(data);

        SerializeOptionalResourceLocation(data, BodyAssetId);
        SerializeOptionalResourceLocation(data, CapeAssetId);
        SerializeOptionalResourceLocation(data, ElytraAssetId);

        data.AddRange(DataTypes.GetBool(Model.HasValue));
        if (Model.HasValue)
            data.AddRange(dataTypes.GetBool(Model.Value == ProfileSkinModel.Slim));

        return new Queue<byte>(data);
    }

    private List<ProfileProperty> ReadProfileProperties(Queue<byte> data, int count)
    {
        var properties = new List<ProfileProperty>(count);
        for (var i = 0; i < count; i++)
        {
            var propertyName = dataTypes.ReadNextString(data);
            var propertyValue = dataTypes.ReadNextString(data);
            var hasSignature = dataTypes.ReadNextBool(data);
            var signature = hasSignature ? dataTypes.ReadNextString(data) : null;

            properties.Add(new ProfileProperty(propertyName, propertyValue, hasSignature, signature));
        }

        return properties;
    }

    private void SerializeProfileProperties(List<byte> data)
    {
        foreach (var profileProperty in ProfileProperties)
        {
            data.AddRange(DataTypes.GetString(profileProperty.Name));
            data.AddRange(DataTypes.GetString(profileProperty.Value));
            data.AddRange(DataTypes.GetBool(profileProperty.HasSignature));
            if (!profileProperty.HasSignature)
                continue;

            if (string.IsNullOrEmpty(profileProperty.Signature))
                throw new NullReferenceException("Can't serialize the ProfileComponent because HasSignature is true, but the Signature is null/empty!");

            data.AddRange(DataTypes.GetString(profileProperty.Signature));
        }
    }

    private string? ReadOptionalResourceLocation(Queue<byte> data)
    {
        return dataTypes.ReadNextBool(data) ? dataTypes.ReadNextString(data) : null;
    }

    private void SerializeOptionalResourceLocation(List<byte> data, string? resourceLocation)
    {
        data.AddRange(DataTypes.GetBool(!string.IsNullOrEmpty(resourceLocation)));
        if (!string.IsNullOrEmpty(resourceLocation))
            data.AddRange(DataTypes.GetString(resourceLocation));
    }
}

public record ProfileProperty(string Name, string Value, bool HasSignature, string? Signature);

public enum ProfileSkinModel
{
    Wide,
    Slim
}
