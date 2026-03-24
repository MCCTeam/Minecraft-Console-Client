using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class EquippableComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int Slot { get; set; }
    public SoundEventSubComponent? EquipSound { get; set; }
    public bool HasModel { get; set; }
    public string? Model { get; set; }
    public bool HasCameraOverlay { get; set; }
    public string? CameraOverlay { get; set; }
    public bool HasAllowedEntities { get; set; }
    public int AllowedEntitiesType { get; set; }
    public string? AllowedEntitiesTag { get; set; }
    public List<int>? AllowedEntitiesIds { get; set; }
    public bool Dispensable { get; set; }
    public bool Swappable { get; set; }
    public bool DamageOnHurt { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Slot = DataTypes.ReadNextVarInt(data);
        EquipSound = (SoundEventSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
        
        HasModel = DataTypes.ReadNextBool(data);
        if (HasModel)
            Model = DataTypes.ReadNextString(data);

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
            }
            else
            {
                AllowedEntitiesIds = new List<int>();
                for (var i = 0; i < AllowedEntitiesType - 1; i++)
                    AllowedEntitiesIds.Add(DataTypes.ReadNextVarInt(data));
            }
        }

        Dispensable = DataTypes.ReadNextBool(data);
        Swappable = DataTypes.ReadNextBool(data);
        DamageOnHurt = DataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Slot));
        if (EquipSound is not null) data.AddRange(EquipSound.Serialize());

        data.AddRange(DataTypes.GetBool(HasModel));
        if (HasModel && Model is not null)
            data.AddRange(DataTypes.GetString(Model));

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
        return new Queue<byte>(data);
    }
}
