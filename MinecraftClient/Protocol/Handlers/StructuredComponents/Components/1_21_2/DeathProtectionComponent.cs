using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;

public class DeathProtectionComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public List<ConsumeEffectData> DeathEffects { get; set; } = new();

    public override void Parse(Queue<byte> data)
    {
        var effectCount = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < effectCount; i++)
        {
            var effectTypeId = dataTypes.ReadNextVarInt(data);
            var effectData = ReadConsumeEffectPayload(effectTypeId, data);
            DeathEffects.Add(new ConsumeEffectData(effectTypeId, effectData));
        }
    }

    private byte[] ReadConsumeEffectPayload(int effectTypeId, Queue<byte> data)
    {
        var payload = new List<byte>();
        switch (effectTypeId)
        {
            case 0: // apply_effects
                var effectCount = dataTypes.ReadNextVarInt(data);
                payload.AddRange(DataTypes.GetVarInt(effectCount));
                for (var i = 0; i < effectCount; i++)
                    payload.AddRange(ReadMobEffectInstance(data));
                payload.AddRange(DataTypes.GetFloat(dataTypes.ReadNextFloat(data)));
                break;
            case 1: // remove_effects
                payload.AddRange(ReadHolderSet(data));
                break;
            case 2: // clear_all_effects
                break;
            case 3: // teleport_randomly
                payload.AddRange(DataTypes.GetFloat(dataTypes.ReadNextFloat(data)));
                break;
            case 4: // play_sound
                var sound = (SoundEventSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
                payload.AddRange(sound.Serialize());
                break;
        }
        return payload.ToArray();
    }

    private byte[] ReadMobEffectInstance(Queue<byte> data)
    {
        var result = new List<byte>();
        result.AddRange(DataTypes.GetVarInt(dataTypes.ReadNextVarInt(data)));
        result.AddRange(ReadMobEffectDetails(data));
        return result.ToArray();
    }

    private byte[] ReadMobEffectDetails(Queue<byte> data)
    {
        var result = new List<byte>();
        result.AddRange(DataTypes.GetVarInt(dataTypes.ReadNextVarInt(data)));
        result.AddRange(DataTypes.GetVarInt(dataTypes.ReadNextVarInt(data)));
        result.AddRange(DataTypes.GetBool(dataTypes.ReadNextBool(data)));
        result.AddRange(DataTypes.GetBool(dataTypes.ReadNextBool(data)));
        result.AddRange(DataTypes.GetBool(dataTypes.ReadNextBool(data)));
        var hasHidden = dataTypes.ReadNextBool(data);
        result.AddRange(DataTypes.GetBool(hasHidden));
        if (hasHidden)
            result.AddRange(ReadMobEffectDetails(data));
        return result.ToArray();
    }

    private byte[] ReadHolderSet(Queue<byte> data)
    {
        var result = new List<byte>();
        var type = dataTypes.ReadNextVarInt(data);
        result.AddRange(DataTypes.GetVarInt(type));
        if (type == 0)
        {
            result.AddRange(DataTypes.GetString(dataTypes.ReadNextString(data)));
        }
        else
        {
            for (var i = 0; i < type - 1; i++)
                result.AddRange(DataTypes.GetVarInt(dataTypes.ReadNextVarInt(data)));
        }
        return result.ToArray();
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(DeathEffects.Count));
        foreach (var effect in DeathEffects)
        {
            data.AddRange(DataTypes.GetVarInt(effect.EffectTypeId));
            data.AddRange(effect.Payload);
        }
        return new Queue<byte>(data);
    }

    public record ConsumeEffectData(int EffectTypeId, byte[] Payload);
}
