using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_8;

public class EquippableComponent1218(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Slot { get; set; }
    public SoundEventSubComponent? EquipSound { get; set; }
    public bool HasAssetId { get; set; }
    public string? AssetId { get; set; }
    public bool HasCameraOverlay { get; set; }
    public string? CameraOverlay { get; set; }
    public bool HasAllowedEntities { get; set; }
    public int AllowedEntitiesType { get; set; }
    public string? AllowedEntitiesTag { get; set; }
    public List<int>? AllowedEntitiesIds { get; set; }
    public bool Dispensable { get; set; }
    public bool Swappable { get; set; }
    public bool DamageOnHurt { get; set; }
    public bool EquipOnInteract { get; set; }
    public bool CanBeSheared { get; set; }
    public SoundEventSubComponent? ShearingSound { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Slot = DataTypes.ReadNextVarInt(data);
        EquipSound = (SoundEventSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);

        HasAssetId = DataTypes.ReadNextBool(data);
        if (HasAssetId)
            AssetId = DataTypes.ReadNextString(data);

        HasCameraOverlay = DataTypes.ReadNextBool(data);
        if (HasCameraOverlay)
            CameraOverlay = DataTypes.ReadNextString(data);

        HasAllowedEntities = DataTypes.ReadNextBool(data);
        if (HasAllowedEntities)
        {
            AllowedEntitiesType = DataTypes.ReadNextVarInt(data);
            if (AllowedEntitiesType == 0)
            {
                AllowedEntitiesTag = DataTypes.ReadNextString(data);
                AllowedEntitiesIds = null;
            }
            else
            {
                AllowedEntitiesTag = null;
                AllowedEntitiesIds = new List<int>(Math.Max(AllowedEntitiesType - 1, 0));
                for (var i = 0; i < AllowedEntitiesType - 1; i++)
                    AllowedEntitiesIds.Add(DataTypes.ReadNextVarInt(data));
            }
        }

        Dispensable = DataTypes.ReadNextBool(data);
        Swappable = DataTypes.ReadNextBool(data);
        DamageOnHurt = DataTypes.ReadNextBool(data);
        EquipOnInteract = DataTypes.ReadNextBool(data);
        CanBeSheared = DataTypes.ReadNextBool(data);
        ShearingSound = (SoundEventSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Slot));

        if (EquipSound is null)
            throw new ArgumentNullException(nameof(EquipSound), "EquipSound is required.");

        if (ShearingSound is null)
            throw new ArgumentNullException(nameof(ShearingSound), "ShearingSound is required.");

        data.AddRange(EquipSound.Serialize());

        data.AddRange(DataTypes.GetBool(HasAssetId));
        if (HasAssetId && AssetId is not null)
            data.AddRange(DataTypes.GetString(AssetId));

        data.AddRange(DataTypes.GetBool(HasCameraOverlay));
        if (HasCameraOverlay && CameraOverlay is not null)
            data.AddRange(DataTypes.GetString(CameraOverlay));

        data.AddRange(DataTypes.GetBool(HasAllowedEntities));
        if (HasAllowedEntities)
        {
            data.AddRange(DataTypes.GetVarInt(AllowedEntitiesType));
            if (AllowedEntitiesType == 0 && AllowedEntitiesTag is not null)
            {
                data.AddRange(DataTypes.GetString(AllowedEntitiesTag));
            }
            else if (AllowedEntitiesIds is not null)
            {
                foreach (var id in AllowedEntitiesIds)
                    data.AddRange(DataTypes.GetVarInt(id));
            }
        }

        data.AddRange(DataTypes.GetBool(Dispensable));
        data.AddRange(DataTypes.GetBool(Swappable));
        data.AddRange(DataTypes.GetBool(DamageOnHurt));
        data.AddRange(DataTypes.GetBool(EquipOnInteract));
        data.AddRange(DataTypes.GetBool(CanBeSheared));
        data.AddRange(ShearingSound.Serialize());
        return new Queue<byte>(data);
    }
}
